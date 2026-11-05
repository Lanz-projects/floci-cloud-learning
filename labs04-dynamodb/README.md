# Lab 04 — DynamoDB with Floci, Terraform, and C#

## Goal

Learn how a NoSQL database stores and retrieves application data. This lab creates a DynamoDB orders table with Terraform, explores item operations with the AWS CLI, and uses a C# console app with the AWS SDK for .NET.

All AWS CLI, Terraform, and SDK requests in this lab go to Floci at `http://localhost:4566`; they do not create AWS resources.

## Prerequisites

- Docker and Floci CLI
- AWS CLI
- Terraform
- .NET 10 SDK
- PowerShell
- Completion of [Lab 03 — SNS fan-out to SQS](../labs03-sns-fanout/README.md)

## Start Floci and configure the AWS CLI

Start Floci:

```powershell
floci start
```

In the PowerShell window used for this lab, set the local endpoint and emulator credentials:

```powershell
$env:AWS_ENDPOINT_URL = "http://localhost:4566"
$env:AWS_DEFAULT_REGION = "us-east-1"
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
```

These credentials are for the local emulator. The Terraform provider and C# client also point to Floci and use local test credentials.

## Create the table with Terraform

The configuration is in `main.tf`. It creates `floci-lab04-orders` with:

- `CustomerId` as the partition key (Terraform calls it `hash_key`).
- `OrderId` as the sort key (Terraform calls it `range_key`).
- On-demand billing (`PAY_PER_REQUEST`).
- String key attributes. Other item attributes, such as `Status` and `Total`, do not need to be declared in this table resource.

The pair `CustomerId` + `OrderId` uniquely identifies each item. Using `CustomerId` as the partition key also lets the app query all orders belonging to one customer.

From `labs04-dynamodb`, initialize and review the configuration:

```powershell
terraform init
terraform fmt
terraform validate
terraform plan
terraform apply
```

Review the plan before applying. After configuration changes, run `terraform fmt`, `terraform validate`, and `terraform plan` before applying.

The root `.gitignore` excludes the downloaded `.terraform` directory, Terraform state, and .NET build output. Keep `terraform.tfstate` out of Git; commit `.terraform.lock.hcl` so the selected AWS provider version stays consistent.

Check the table and read its Terraform outputs:

```powershell
aws dynamodb describe-table --table-name "floci-lab04-orders" --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
terraform output
terraform output -raw orders_table_name
terraform output -raw orders_table_arn
```

## Explore item operations with the AWS CLI

The AWS CLI commands are the same DynamoDB commands used with AWS; `--endpoint-url` sends each request to Floci. Set a PowerShell variable for the table name:

```powershell
$tableName = terraform output -raw orders_table_name
```

The examples below use small JSON files. PowerShell's here-string keeps the JSON double quotes intact, and `Set-Content -Encoding ascii` writes files the AWS CLI can parse reliably.

### Add two orders

Create the first item file and add the item:

```powershell
@'
{
  "CustomerId": { "S": "customer-123" },
  "OrderId": { "S": "cli-order-1001" },
  "Status": { "S": "Pending" },
  "Total": { "N": "49.95" }
}
'@ | Set-Content -Encoding ascii .\order-item.json

aws dynamodb put-item --table-name $tableName --item file://order-item.json --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

Create another order for the same customer by changing `OrderId` to `cli-order-1002`, `Status` to `Processing`, and `Total` to `19.95`, then run `put-item` again.

### Get an item by its full key

`GetItem` needs both key attributes because the table has a partition key and a sort key:

```powershell
@'
{
  "CustomerId": { "S": "customer-123" },
  "OrderId": { "S": "cli-order-1001" }
}
'@ | Set-Content -Encoding ascii .\order-key.json

aws dynamodb get-item --table-name $tableName --key file://order-key.json --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

### Query all orders for a customer

Unlike a table scan, a query uses the partition key to select a customer's orders. Create the expression value file:

```powershell
@'
{
  ":customerId": { "S": "customer-123" }
}
'@ | Set-Content -Encoding ascii .\query-values.json

aws dynamodb query --table-name $tableName --key-condition-expression "CustomerId = :customerId" --expression-attribute-values file://query-values.json --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

### Update an order

Create the expression name and value files, then update the status of `cli-order-1001` to `Shipped`:

```powershell
@'
{
  "#status": "Status"
}
'@ | Set-Content -Encoding ascii .\status-names.json

@'
{
  ":status": { "S": "Shipped" }
}
'@ | Set-Content -Encoding ascii .\status-values.json

aws dynamodb update-item --table-name $tableName --key file://order-key.json --update-expression "SET #status = :status" --expression-attribute-names file://status-names.json --expression-attribute-values file://status-values.json --return-values ALL_NEW --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

`#status` is an expression name placeholder because `Status` can conflict with reserved words. `:status` is a placeholder for the new attribute value.

### Delete an order

Use the same full key file to delete the first order:

```powershell
aws dynamodb delete-item --table-name $tableName --key file://order-key.json --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

You can query the customer again to confirm that the deleted item is gone. `put-item`, `get-item`, `query`, `update-item`, and `delete-item` are the core item operations used in this lab.

## Run the C# app

The app is in `app` and uses the `AWSSDK.DynamoDBv2` NuGet package. It reads the table name from `DYNAMODB_TABLE_NAME`; set that variable in the same PowerShell window used to run the app:

```powershell
$env:DYNAMODB_TABLE_NAME = terraform output -raw orders_table_name
dotnet run --project .\app\FlociDynamoDbDemo.csproj
```

The menu options are:

1. **Create an order** — enter a customer ID, order ID, status, and total.
2. **Get an order by its keys** — look up one item using both key values.
3. **List orders for a customer** — query using the partition key.
4. **Update an order status** — change the status for an existing item.
5. **Delete an order** — remove an item using both key values.
6. **Run a sample workflow** — create three sample orders for `demo-customer-001`, display them, change `demo-order-1002` from `Processing` to `Shipped`, and display the updated list. The sample items remain in the table; choose option 3 and enter `demo-customer-001` to view them again.
0. **Exit** — close the app.

The C# source is organized by responsibility:

- `Program.cs` reads the table name, creates the client and service, then starts the menu.
- `FlociDynamoDbClientFactory.cs` configures the AWS SDK client to use Floci.
- `Menus/OrderMenu.cs` displays the menu, reads input, and formats results.
- `Services/OrderService.cs` contains the DynamoDB SDK calls for create, get, query, update, and delete.

## Cleanup

To remove the DynamoDB table and every item in it, review and run:

```powershell
terraform destroy
```

Destroying the table permanently removes its data. Reapply the Terraform configuration to recreate an empty table.

## What I learned

- DynamoDB is a NoSQL key-value/document database; each item can have regular attributes beyond its primary key.
- A composite primary key consists of a partition key and sort key. Here, `CustomerId` groups orders and `OrderId` identifies an order within that group.
- `GetItem` looks up an item by its complete primary key, while `Query` efficiently finds items by partition key.
- DynamoDB CLI values include a type descriptor such as `S` for string and `N` for number.
- Terraform can manage the table and expose its name as an output for an application to use.
- The AWS SDK for .NET lets a C# app perform the same create, read, query, update, and delete operations through Floci.
