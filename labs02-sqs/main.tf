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
    sqs = "http://localhost:4566"
  }
}

resource "aws_sqs_queue" "learning" {
  name                       = "floci-learning-queue"
  visibility_timeout_seconds = 30
}

resource "aws_sqs_queue" "dead_letter" {
  name = "floci-learning-dlq"
}

resource "aws_sqs_queue_redrive_policy" "learning" {
  queue_url = aws_sqs_queue.learning.url

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dead_letter.arn
    maxReceiveCount     = 3
  })
}

output "queue_url" {
  description = "URL used to send and receive messages"
  value       = aws_sqs_queue.learning.url
}

output "queue_arn" {
  description = "ARN that identifies the queue"
  value       = aws_sqs_queue.learning.arn
}

