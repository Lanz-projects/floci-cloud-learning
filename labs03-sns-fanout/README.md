# Lab 03 — SNS fan-out to SQS with Floci, Terraform, and C#

## Goal

Learn how one published event can be delivered to multiple independent consumers. This lab uses Terraform to create an SNS topic, two SQS queues, subscriptions connecting the topic to each queue, and queue policies that allow SNS to send messages. A C# console app publishes JSON order events and can receive a copy from either queue.

The flow is:

```text
C# publisher -> SNS topic -> Billing SQS queue -> Billing consumer
                         -> Shipping SQS queue -> Shipping consumer
```

Each subscribed queue receives its own copy. Receiving or deleting the Billing copy does not affect the Shipping copy.

All AWS CLI, Terraform, and SDK requests in this lab go to Floci at `http://localhost:4566`; they do not create AWS resources.

## Prerequisites

- Docker and Floci CLI
- AWS CLI
- Terraform
- .NET 10 SDK
- PowerShell
- Completion of [Lab 02 — SQS](../labs02-sqs/README.md)

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

These values are for the local emulator. Terraform and the C# clients also point to Floci and use local test credentials.

## Create the topic and queues with Terraform

The configuration is in `main.tf`. It defines:

- `aws_sns_topic.orders`, the SNS topic named `floci-lab03-orders`.
- `aws_sqs_queue.billing` and `aws_sqs_queue.shipping`, the independent standard queues.
- A queue policy for each queue that allows the SNS service to send messages from this topic.
- An SNS subscription for each queue.
- Outputs for the topic ARN and both queue URLs.

The queue policies grant `sqs:SendMessage` to the SNS service and restrict it to the orders topic using the `aws:SourceArn` condition. The subscription describes the delivery destination; the queue policy grants permission for that delivery.

From `labs03-sns-fanout`, initialize and review the configuration:

```powershell
terraform init
terraform fmt
terraform validate
terraform plan
terraform apply
```

Review the plan before applying. After configuration changes, run `terraform fmt`, `terraform validate`, and `terraform plan` before applying.

The root `.gitignore` excludes the downloaded `.terraform` directory and Terraform state files. Keep `terraform.tfstate` out of Git; commit `.terraform.lock.hcl` so the selected AWS provider version stays consistent.

Read the outputs:

```powershell
terraform output
terraform output -raw topic_arn
terraform output -raw billing_queue_url
terraform output -raw shipping_queue_url
```

Set the values for the C# app in the same PowerShell session:

```powershell
$env:SNS_TOPIC_ARN = terraform output -raw topic_arn
$env:BILLING_QUEUE_URL = terraform output -raw billing_queue_url
$env:SHIPPING_QUEUE_URL = terraform output -raw shipping_queue_url
```

Environment variables last only for that PowerShell session. Set them again if you open a new terminal.

## Explore fan-out with the AWS CLI

The AWS CLI commands are the same ones used with AWS; the endpoint option sends them to Floci. Load the Terraform outputs into PowerShell variables:

```powershell
$topicArn = terraform output -raw topic_arn
$billingQueueUrl = terraform output -raw billing_queue_url
$shippingQueueUrl = terraform output -raw shipping_queue_url
```

Check the topic and its subscriptions:

```powershell
aws sns list-topics --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
aws sns list-subscriptions-by-topic --topic-arn $topicArn --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

Publish one sample event:

```powershell
aws sns publish --topic-arn $topicArn --message "OrderPlaced: orderId=cli-1001" --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

Receive from each queue independently:

```powershell
aws sqs receive-message --queue-url $billingQueueUrl --max-number-of-messages 1 --wait-time-seconds 2 --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
aws sqs receive-message --queue-url $shippingQueueUrl --max-number-of-messages 1 --wait-time-seconds 2 --endpoint-url "http://localhost:4566" --region "us-east-1" --no-cli-pager
```

By default, SNS wraps the published content in a notification envelope. The original content is in the envelope's `Message` field. Receiving hides a queue message temporarily; it does not delete it. To remove a message, use the latest `ReceiptHandle` returned for that queue:

```powershell
aws sqs delete-message --queue-url $billingQueueUrl --receipt-handle "PASTE_THE_FULL_BILLING_RECEIPT_HANDLE_HERE" --endpoint-url "http://localhost:4566" --region "us-east-1"
aws sqs delete-message --queue-url $shippingQueueUrl --receipt-handle "PASTE_THE_FULL_SHIPPING_RECEIPT_HANDLE_HERE" --endpoint-url "http://localhost:4566" --region "us-east-1"
```

Use the handle from each queue's own receive response. If a message is not deleted before the visibility timeout expires, it becomes available again.

## Run the C# app

The app is in `app` and uses `AWSSDK.SimpleNotificationService` and `AWSSDK.SQS`. Its source is organized as follows:

- `Program.cs` validates configuration and displays the menu.
- `FlociClientFactory.cs` creates SNS and SQS clients configured for Floci.
- `Models/OrderPlacedEvent.cs` defines the JSON order event.
- `Services/SnsPublisher.cs` serializes and publishes the event.
- `Services/SqsMessageConsumer.cs` receives a message, unwraps the SNS envelope, displays its content, and deletes it.

Set the three environment variables shown above, then run the app from `labs03-sns-fanout`:

```powershell
dotnet run --project .\app\FlociSnsDemo.csproj
```

The menu options are:

1. **Publish an order event** — enter an order ID. The app publishes a JSON event with the event type, order ID, customer ID, total, and UTC timestamp.
2. **Receive one message from Billing** — receive, display, and delete one message from the Billing queue.
3. **Receive one message from Shipping** — receive, display, and delete one message from the Shipping queue.
0. **Exit** — close the app.

Publishing does not automatically consume either queue. Choose Billing and Shipping separately to see that each queue owns its copy. If no message is available, the app reports that and leaves the queue unchanged.

## Cleanup

If messages remain in the queues, process them with the app or purge the queues before destroying them. Purging permanently deletes all messages in that queue:

```powershell
aws sqs purge-queue --queue-url $billingQueueUrl --endpoint-url "http://localhost:4566" --region "us-east-1"
aws sqs purge-queue --queue-url $shippingQueueUrl --endpoint-url "http://localhost:4566" --region "us-east-1"
```

To remove the topic, subscriptions, queue policies, and queues managed by Terraform, review and run:

```powershell
terraform destroy
```

## What I learned

- SNS is a broadcaster: the publisher sends one event to a topic, and SNS delivers a copy to each subscriber.
- SQS holds a subscriber's copy until a consumer receives and deletes it.
- Each queue has its own copy, so processing one queue does not consume the message from another.
- An SNS subscription defines a delivery destination; an SQS queue policy grants the topic permission to send there.
- SNS wraps messages in a notification envelope by default; the original event is in the envelope's `Message` field.
- Terraform can manage the topic, queues, subscriptions, and queue permissions together.
- A C# app can use the AWS SDK to publish to SNS and consume from SQS through Floci.
