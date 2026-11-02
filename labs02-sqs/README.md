# Lab 02 — SQS with Floci, Terraform, and C#

## Goal

Learn how message queues let one part of an application send work to another part for later processing. This lab creates an SQS standard queue and a dead-letter queue (DLQ) with Terraform, uses the AWS CLI to explore message behavior, and builds a C# console app with the AWS SDK for .NET.

All AWS CLI, Terraform, and SDK requests in this lab go to Floci at `http://localhost:4566`; they do not create AWS resources.

## Prerequisites

- Docker and Floci CLI
- AWS CLI
- Terraform
- .NET 10 SDK
- PowerShell

## Start Floci and configure the AWS CLI

Start Floci:

```powershell
floci start
```

In the PowerShell window used for the AWS CLI, set the local endpoint and emulator credentials:

```powershell
$env:AWS_ENDPOINT_URL = "http://localhost:4566"
$env:AWS_DEFAULT_REGION = "us-east-1"
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
```

These values are for the local emulator. The Terraform provider and C# app also specify the Floci endpoint and test credentials in their configuration.

## Create the queues with Terraform

The Terraform configuration is in `main.tf`. It defines:

- `aws_sqs_queue.learning`, a standard queue named `floci-learning-queue` with a 30-second visibility timeout.
- `aws_sqs_queue.dead_letter`, a standard dead-letter queue named `floci-learning-dlq`.
- `aws_sqs_queue_redrive_policy.learning`, which sends repeatedly received, unprocessed messages from the learning queue to the DLQ after the configured receive threshold of 3.
- Outputs for the learning queue URL and ARN.

From `labs02-sqs`, initialize and review the configuration:

```powershell
terraform init
terraform fmt
terraform validate
terraform plan
terraform apply
```

Review the plan before applying. `terraform init` is needed once when setting up the lab; after configuration changes, run `terraform fmt`, `terraform validate`, and `terraform plan` before applying.

The root `.gitignore` excludes the downloaded `.terraform` directory, Terraform state files, and .NET build output. Keep `terraform.tfstate` out of Git; commit `.terraform.lock.hcl` so the selected AWS provider version stays consistent.

Terraform outputs provide the learning queue details:

```powershell
terraform output
terraform output -raw queue_url
terraform output -raw queue_arn
```

Set the URL for the C# app in the same PowerShell session:

```powershell
$env:SQS_QUEUE_URL = terraform output -raw queue_url
```

This environment variable lasts for that PowerShell session. Set it again if you open a new terminal.

## Explore message operations with the AWS CLI

The AWS CLI uses the same SQS commands as it does for AWS. The endpoint option directs each request to Floci. Get the queue URL from Terraform:

```powershell
$queueUrl = terraform output -raw queue_url
```

Send a message:

```powershell
aws sqs send-message --queue-url $queueUrl --message-body "Hello from my SQS lab" --endpoint-url "http://localhost:4566" --region "us-east-1"
```

Receive up to one message:

```powershell
aws sqs receive-message --queue-url $queueUrl --max-number-of-messages 1 --wait-time-seconds 2 --endpoint-url "http://localhost:4566" --region "us-east-1"
```

Receiving does not delete the message. It temporarily hides the message for the queue's 30-second visibility timeout. Delete it after processing by using the `ReceiptHandle` from the latest receive response—not the `MessageId`:

```powershell
aws sqs delete-message --queue-url $queueUrl --receipt-handle "PASTE_THE_FULL_RECEIPT_HANDLE_HERE" --endpoint-url "http://localhost:4566" --region "us-east-1"
```

If the message is not deleted before the visibility timeout expires, it becomes available again. To test that without waiting, receive it with `--visibility-timeout 0`; the message stays visible immediately, but each receive still counts as a delivery attempt.

## Test the dead-letter queue

Send one identifiable message to the learning queue, then receive it repeatedly without deleting it. The configured redrive policy moves it to the DLQ after the receive threshold is exceeded.

Get the DLQ URL:

```powershell
$dlqUrl = (aws sqs get-queue-url --queue-name "floci-learning-dlq" --endpoint-url "http://localhost:4566" --region "us-east-1" | ConvertFrom-Json).QueueUrl
```

Receive from the DLQ to inspect a failed message:

```powershell
aws sqs receive-message --queue-url $dlqUrl --max-number-of-messages 10 --visibility-timeout 0 --endpoint-url "http://localhost:4566" --region "us-east-1"
```

The DLQ keeps messages that need investigation or manual handling away from the main queue. Receiving a DLQ message does not delete it; use its receipt handle with `delete-message` when you are ready to remove it.

## Run the C# app

The console app is in `app` and uses the `AWSSDK.SQS` NuGet package. It reads the queue URL from `SQS_QUEUE_URL`. From `labs02-sqs`, set the variable and run the app:

```powershell
$env:SQS_QUEUE_URL = terraform output -raw queue_url
dotnet run --project .\app\FlociSqsDemo.csproj
```

The menu options are:

1. **Send a message** — enter a message body and send it to the learning queue.
2. **Receive a message** — receive and display one available message. The app keeps its receipt handle in memory.
3. **Delete the received message** — confirm deletion using that receipt handle.
0. **Exit** — close the app.

The app allows only one outstanding received message at a time so it can keep track of the correct receipt handle. If you exit without deleting a received message, it can become visible again after the visibility timeout. `Program.cs` handles the menu and client configuration; `SqsMessageService.cs` contains the send, receive, and delete SDK calls.

## Purge messages and destroy the lab

To remove all messages but keep a queue, use `purge-queue`. Purging permanently deletes available and in-flight messages and can take up to 60 seconds.

```powershell
aws sqs purge-queue --queue-url $queueUrl --endpoint-url "http://localhost:4566" --region "us-east-1"
aws sqs purge-queue --queue-url $dlqUrl --endpoint-url "http://localhost:4566" --region "us-east-1"
```

To remove the queues and redrive policy managed by Terraform, review and run:

```powershell
terraform destroy
```

## What I learned

- An SQS message is a work item passed from a producer to a consumer; SQS stores it until it is processed or expires.
- Receiving temporarily hides a message. Deleting with the latest receipt handle marks it as processed.
- The visibility timeout allows a message to become available again if it is not deleted.
- A redrive policy moves repeatedly received messages to a dead-letter queue for investigation.
- Terraform manages the queue infrastructure and exposes values such as the queue URL through outputs.
- A C# app can use `AWSSDK.SQS` to send, receive, and delete messages through Floci.
