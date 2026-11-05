# Floci Cloud Learning

This repository is my hands-on space for learning cloud concepts with Floci, Terraform, the AWS CLI, and C#.

Floci runs AWS-compatible services locally, so I can practice cloud workflows without creating AWS resources. I use the AWS CLI to explore service operations, Terraform to manage infrastructure, and C# with the AWS SDK to build small applications.

## Labs

| Lab | Status | What it covers |
| --- | --- | --- |
| [01 — S3](./labs01-s3/README.md) | Complete | AWS CLI S3 operations, Terraform buckets and outputs, S3 versioning, and a C# upload/list/download/delete app. |
| [02 — SQS](./labs02-sqs/README.md) | Complete | Terraform-managed standard queue and dead-letter queue, AWS CLI message lifecycle and retry practice, and a C# send/receive/delete app. |
| [03 — SNS fan-out to SQS](./labs03-sns-fanout/README.md) | Complete | Terraform-managed SNS topic, two SQS subscribers with queue policies, AWS CLI fan-out, and a menu-driven C# publisher and consumer. |
| [04 — DynamoDB](./labs04-dynamodb/README.md) | Complete | A Terraform-managed NoSQL orders table, AWS CLI item operations, and a menu-driven C# app using the DynamoDB SDK. |

Each lab has its own folder so its notes, configuration, and Terraform state are kept separate.

## Learning path

1. Use the AWS CLI to understand basic service operations.
2. Manage local cloud resources with Terraform: providers, resources, variables, outputs, plans, and state.
3. Use the AWS SDK in C# to interact with those resources.
4. Continue with other cloud services and patterns such as databases, IAM, networking, monitoring, and event routing.

The completed labs move from object storage (S3), to message queues (SQS), to publishing an event once and delivering independent copies through SNS to SQS. Lab 04 adds a NoSQL database, composite keys, item operations, and a C# app that creates, queries, updates, and deletes orders.

Start Floci before using a lab:

```powershell
floci start
```
