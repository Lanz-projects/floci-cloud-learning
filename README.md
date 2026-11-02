# Floci Cloud Learning

This repository is my hands-on space for learning cloud concepts with Floci, Terraform, and C#.

Floci runs AWS-compatible services locally, so I can practice cloud workflows without creating AWS resources. I use the AWS CLI to explore service operations, Terraform to manage infrastructure, and C# with the AWS SDK to build a small application.

## Labs

| Lab | Status | What it covers |
| --- | --- | --- |
| [01 — S3](./labs01-s3/README.md) | Complete | AWS CLI S3 operations, Terraform buckets and outputs, S3 versioning, and a C# upload/list/download/delete app. |
| [02 — SQS](./labs02-sqs/README.md) | Complete | Terraform-managed standard queue and dead-letter queue, AWS CLI message lifecycle and retry practice, and a C# send/receive/delete app. |

Each lab has its own folder so its notes, configuration, and Terraform state are kept separate.

## Learning path

1. Use the AWS CLI to understand basic service operations.
2. Manage local cloud resources with Terraform: providers, resources, variables, outputs, plans, and state.
3. Use the AWS SDK in C# to interact with those resources.
4. Explore additional cloud services and patterns such as SNS fan-out, databases, IAM, networking, and monitoring.

The next planned topic is SNS fan-out to SQS: publish one event to an SNS topic and deliver it to multiple queues.

Start Floci before using a lab:

```powershell
floci start
```
