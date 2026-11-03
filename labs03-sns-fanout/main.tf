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
    sns = "http://localhost:4566"
    sqs = "http://localhost:4566"
  }
}

resource "aws_sns_topic" "orders" {
  name = "floci-lab03-orders"
}

resource "aws_sqs_queue" "billing" {
  name = "floci-lab03-billing"
}

resource "aws_sqs_queue" "shipping" {
  name = "floci-lab03-shipping"
}

resource "aws_sqs_queue_policy" "billing" {
  queue_url = aws_sqs_queue.billing.url

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowOrdersTopicToSend"
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.billing.arn

      Condition = {
        ArnEquals = {
          "aws:SourceArn" = aws_sns_topic.orders.arn
        }
      }
    }]
  })
}

resource "aws_sqs_queue_policy" "shipping" {
  queue_url = aws_sqs_queue.shipping.url

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowOrdersTopicToSend"
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.shipping.arn

      Condition = {
        ArnEquals = {
          "aws:SourceArn" = aws_sns_topic.orders.arn
        }
      }
    }]
  })
}

resource "aws_sns_topic_subscription" "billing" {
  topic_arn = aws_sns_topic.orders.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.billing.arn

  depends_on = [aws_sqs_queue_policy.billing]
}

resource "aws_sns_topic_subscription" "shipping" {
  topic_arn = aws_sns_topic.orders.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.shipping.arn

  depends_on = [aws_sqs_queue_policy.shipping]
}


output "topic_arn" {
  description = "ARN of the SNS topic used to publish order events"
  value       = aws_sns_topic.orders.arn
}

output "billing_queue_url" {
  description = "URL for the billing queue"
  value       = aws_sqs_queue.billing.url
}

output "shipping_queue_url" {
  description = "URL for the shipping queue"
  value       = aws_sqs_queue.shipping.url
}