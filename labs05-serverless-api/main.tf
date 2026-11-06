terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}

provider "aws" {
  region     = "us-east-1"
  access_key = "test"
  secret_key = "test"

  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    iam          = "http://localhost:4566"
    lambda       = "http://localhost:4566"
    dynamodb     = "http://localhost:4566"
    apigatewayv2 = "http://localhost:4566"
  }
}

resource "aws_iam_role" "orders_lambda" {
  name = "floci-lab05-orders-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = "sts:AssumeRole"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
    }]
  })
}

resource "aws_lambda_function" "orders" {
  function_name = "floci-lab05-orders"

  filename         = "${path.module}/app/FlociOrdersFunction.zip"
  source_code_hash = filebase64sha256("${path.module}/app/FlociOrdersFunction.zip")

  role    = aws_iam_role.orders_lambda.arn
  handler = "FlociOrdersFunction::FlociOrdersFunction.Class1::FunctionHandler"
  runtime = "dotnet10"

  timeout       = 10
  architectures = ["x86_64"]

  environment {
    variables = {
      ORDERS_TABLE_NAME         = aws_dynamodb_table.orders.name
      AWS_ENDPOINT_URL_DYNAMODB = "http://localhost.floci.io:4566"
    }
  }
}

resource "aws_dynamodb_table" "orders" {
  name         = "floci-lab05-orders"
  billing_mode = "PAY_PER_REQUEST"

  hash_key  = "CustomerId"
  range_key = "OrderId"

  attribute {
    name = "CustomerId"
    type = "S"
  }

  attribute {
    name = "OrderId"
    type = "S"
  }
}

resource "aws_iam_role_policy" "orders_table_access" {
  name = "floci-lab05-orders-table-access"
  role = aws_iam_role.orders_lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "dynamodb:PutItem",
        "dynamodb:GetItem"
      ]
      Resource = aws_dynamodb_table.orders.arn
    }]
  })
}

resource "aws_apigatewayv2_integration" "orders_lambda" {
  api_id                 = aws_apigatewayv2_api.orders.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.orders.invoke_arn
  integration_method     = "POST"
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_api" "orders" {
  name          = "floci-lab05-orders-api"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_route" "create_order" {
  api_id    = aws_apigatewayv2_api.orders.id
  route_key = "POST /orders"
  target    = "integrations/${aws_apigatewayv2_integration.orders_lambda.id}"
}

resource "aws_apigatewayv2_route" "get_order" {
  api_id    = aws_apigatewayv2_api.orders.id
  route_key = "GET /orders/{customerId}/{orderId}"
  target    = "integrations/${aws_apigatewayv2_integration.orders_lambda.id}"
}

resource "aws_lambda_permission" "allow_api_gateway" {
  statement_id  = "AllowOrdersApiInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.orders.function_name
  principal     = "apigateway.amazonaws.com"
  # The provider skips account lookup, so use Floci's local account ID here.
  source_arn = "arn:aws:execute-api:us-east-1:000000000000:${aws_apigatewayv2_api.orders.id}/*/*/*"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.orders.id
  name        = "$default"
  auto_deploy = true

  depends_on = [
    aws_apigatewayv2_route.create_order,
    aws_apigatewayv2_route.get_order,
    aws_lambda_permission.allow_api_gateway
  ]
}

output "orders_table_name" {
  description = "Name of the DynamoDB orders table"
  value       = aws_dynamodb_table.orders.name
}

output "orders_table_arn" {
  description = "ARN of the DynamoDB orders table"
  value       = aws_dynamodb_table.orders.arn
}

output "orders_api_local_url" {
  description = "Local Floci URL for the orders API"
  value       = "http://${aws_apigatewayv2_api.orders.id}.execute-api.localhost.floci.io:4566"
}
