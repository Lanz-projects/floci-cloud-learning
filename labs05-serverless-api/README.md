# Lab 05 — Serverless Orders API with Floci, Terraform, C#, and DynamoDB

## Goal

Build an HTTP API that creates and retrieves orders. API Gateway routes requests to a C# Lambda function, and the function reads and writes orders in DynamoDB.

```text
HTTP client -> API Gateway HTTP API -> Lambda -> DynamoDB
```

Terraform manages the local resources. AWS CLI and SDK requests use Floci at `http://localhost:4566`; this lab does not create AWS resources.

## Prerequisites

- Docker with Floci CLI
- AWS CLI
- Terraform
- .NET 10 SDK
- Amazon Lambda Tools for .NET (`dotnet lambda`)
- PowerShell
- Completion of [Lab 04 — DynamoDB](../labs04-dynamodb/README.md)

Floci runs Lambda code in Docker containers, so Docker must be running when you invoke the function. See the [Floci service overview](https://floci.io/floci/services/) and [Lambda guide](https://floci.io/floci/services/lambda/).

## Start Floci and configure the AWS CLI

Start Floci:

```powershell
floci start
```

In the PowerShell window used for AWS CLI commands, configure the local endpoint and emulator credentials:

```powershell
$env:AWS_ENDPOINT_URL = "http://localhost:4566"
$env:AWS_DEFAULT_REGION = "us-east-1"
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
```

These are local emulator credentials. Do not use them for AWS accounts. Terraform has its own local endpoint and test credentials in `main.tf`; the Lambda function receives its DynamoDB endpoint and table name from its Terraform environment variables.

## Build and package the Lambda function

The Lambda project is in `app`. `Class1.cs` handles HTTP routing and responses, `Services/OrderService.cs` validates and accesses DynamoDB, and `Models/OrderRequest.cs` defines the create request fields. The handler keeps the `Class1` name used by Terraform.

From `labs05-serverless-api`, build and package the function before applying Terraform. Repeat these steps after changing the C# code:

```powershell
dotnet build .\app\FlociOrdersFunction.csproj
dotnet lambda package .\app\FlociOrdersFunction.zip --project-location .\app --configuration Release --framework net10.0
```

The package command publishes the function for Lambda and creates the ZIP file that Terraform deploys. The root `.gitignore` excludes the generated ZIP and .NET build output.

## Create the infrastructure with Terraform

From `labs05-serverless-api`, initialize and review the configuration:

```powershell
terraform init
terraform fmt
terraform validate
terraform plan
terraform apply
```

Review the plan before applying. After changes to Terraform or the Lambda package, run `terraform fmt`, `terraform validate`, and `terraform plan` before applying.

Terraform manages:

- An IAM role that Lambda assumes, with permission to call DynamoDB `PutItem` and `GetItem` on this lab's table.
- A DynamoDB table named `floci-lab05-orders`, with `CustomerId` as its partition key and `OrderId` as its sort key.
- The `floci-lab05-orders` Lambda function and its deployment package.
- An API Gateway HTTP API with a Lambda proxy integration using payload format `2.0`.
- Routes for creating and retrieving orders, a `$default` stage with automatic deployment, and a Lambda permission allowing API Gateway to invoke the function.

The function reads `ORDERS_TABLE_NAME` from its environment. Terraform sets `AWS_ENDPOINT_URL_DYNAMODB` to `http://localhost.floci.io:4566` so the Lambda container can reach Floci.

Check the function and Terraform outputs:

```powershell
aws lambda list-functions --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
terraform output
terraform output -raw orders_table_name
terraform output -raw orders_table_arn
terraform output -raw orders_api_local_url
```

## API routes

| Method and path | Purpose | Successful response |
| --- | --- | --- |
| `POST /orders` | Create an order from the JSON request body. | `201 Created` |
| `GET /orders/{customerId}/{orderId}` | Retrieve an order by its complete DynamoDB key. | `200 OK` |

The POST body uses `customerId`, `orderId`, `status`, and `total`. The HTTP method and path select the operation, so the request body does not need an `operation` field. Invalid JSON or order fields return `400 Bad Request`. A missing order returns `404 Not Found`.

The three string fields are required and the total cannot be negative. If `total` is omitted, the current C# model defaults it to zero. Creating an order with an existing customer and order ID replaces that item because the function uses DynamoDB `PutItem`.

The routes have no authentication configured; this API is for local practice. The Lambda role grants DynamoDB access and does not include a CloudWatch Logs policy.

## Call the API from PowerShell

For a quick check of all four responses, start Floci, package and apply the latest code, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\verify-api.ps1
```

The script reads the API URL from Terraform, creates a uniquely named order, and checks create, retrieve, invalid input, and missing order responses. The created verification order remains in DynamoDB. To supply a URL explicitly, add `-ApiUrl "http://your-local-api-url"`.

Get the local API URL from Terraform and create an order:

```powershell
$apiUrl = terraform output -raw orders_api_local_url

$order = @{
  customerId = "customer-123"
  orderId    = "lab05-api-1002"
  status     = "Pending"
  total      = 49.95
} | ConvertTo-Json -Compress

$postResponse = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$apiUrl/orders" -ContentType "application/json" -Body $order
$postResponse.StatusCode
$postResponse.Content
```

Expected status: `201`. The response body contains a message confirming the order was saved.

Retrieve it using the GET route:

```powershell
$getResponse = Invoke-WebRequest -UseBasicParsing -Method Get -Uri "$apiUrl/orders/customer-123/lab05-api-1002"
$getResponse.StatusCode
$getResponse.Content
```

Expected status: `200`. The response includes the order ID, status, and total.

### Verify error responses

Send a request with a missing customer ID. The handler should return `400` with a validation message:

```powershell
$invalidOrder = @{
  customerId = ""
  orderId    = "lab05-api-invalid"
  status     = "Pending"
  total      = 49.95
} | ConvertTo-Json -Compress

try {
  Invoke-RestMethod -Method Post -Uri "$apiUrl/orders" -ContentType "application/json" -Body $invalidOrder
}
catch {
  [int]$_.Exception.Response.StatusCode
  $_.ErrorDetails.Message
}
```

Request an order that does not exist. The response should have status `404`:

```powershell
curl.exe -i "$apiUrl/orders/customer-123/does-not-exist"
```

## Inspect an order with the AWS CLI

The table can also be checked directly with DynamoDB's `get-item` operation:

```powershell
$tableName = terraform output -raw orders_table_name

@'
{
  "CustomerId": { "S": "customer-123" },
  "OrderId": { "S": "lab05-api-1002" }
}
'@ | Set-Content -Encoding ascii .\app\bin\order-key.json

aws dynamodb get-item --table-name $tableName --key file://app/bin/order-key.json --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

## What I learned

- API Gateway provides HTTP routes and forwards requests to Lambda using a proxy integration.
- The Lambda handler receives an HTTP API v2 event, reads the body or path parameters, and returns a response with an HTTP status code and JSON body.
- Lambda's execution role allows it to access DynamoDB; a separate Lambda permission allows API Gateway to invoke the function.
- A DynamoDB composite key uses both `CustomerId` and `OrderId` to identify an order.
- Terraform connects the API, Lambda, IAM permissions, and table, while the AWS CLI and C# SDK use the same AWS service interfaces against Floci.
- HTTP status codes distinguish successful create/read requests (`201`/`200`) from invalid input (`400`) and missing orders (`404`).

## Cleanup

To remove the API, Lambda function, IAM resources, DynamoDB table, and its order data, review and run:

```powershell
terraform destroy
```

Destroying the table removes its data. Reapply the Terraform configuration to recreate the lab resources.
The Lambda ZIP must exist when Terraform evaluates this configuration, including when planning cleanup; package it first if it has been removed.

## References

- [Floci service overview](https://floci.io/floci/services/)
- [Floci Lambda guide](https://floci.io/floci/services/lambda/)
- [AWS Lambda HTTP API integration](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-lambda.html)
- [AWS Lambda guide for C#](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html)
- [Terraform API Gateway v2 resources](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/apigatewayv2_api)

