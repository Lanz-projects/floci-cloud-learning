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
  s3_use_path_style           = true

  endpoints {
    s3 = "http://localhost:4566"
  }
}

resource "aws_s3_bucket" "learning" {
  bucket = var.bucket_name
}

resource "aws_s3_bucket" "archive" {
  bucket = var.archive_bucket_name
}

resource "aws_s3_bucket_versioning" "archive" {
  bucket = aws_s3_bucket.archive.id

  versioning_configuration {
    status = "Enabled"
  }
}

output "bucket_name" {
  description = "Name of the S3 bucket used by this lab"
  value       = aws_s3_bucket.learning.bucket
}

output "archive_bucket_name" {
  description = "Name of the archive bucket"
  value       = aws_s3_bucket.archive.bucket
}

