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
    dynamodb = "http://localhost:4566"
  }
}

resource "aws_dynamodb_table" "orders" {
  name         = "floci-lab04-orders"
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

output "orders_table_name" {
  description = "Name of the DynamoDB orders table"
  value       = aws_dynamodb_table.orders.name
}

output "orders_table_arn" {
  description = "ARN that identifies the DynamoDB orders table"
  value       = aws_dynamodb_table.orders.arn
}