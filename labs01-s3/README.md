# Lab 01 — S3 with Floci, Terraform, and C#

## Goal

Learn how object storage works by creating S3 buckets, uploading and downloading objects, and managing infrastructure with Terraform. Then use a C# console app and the AWS SDK for .NET to perform the same object operations.

Everything in this lab connects to Floci at `http://localhost:4566`; these commands do not create resources in AWS.

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

These are local emulator values. The Terraform AWS provider is also configured to use Floci and test credentials in `main.tf`.

## Manage buckets with Terraform

The Terraform files are in this folder:

- `main.tf` configures the AWS provider to send S3 requests to Floci, defines the buckets, and declares outputs.
- `variables.tf` declares the bucket-name inputs.
- `terraform.tfvars` supplies this lab's bucket names.

Terraform treats all `.tf` files in this folder as one configuration. From `labs01-s3`, run:

```powershell
terraform init
terraform fmt
terraform validate
terraform plan
terraform apply
```

Review the plan before applying it. This lab manages:

- `floci-learning`, the basic bucket.
- `floci-learning-archive`, the archive bucket with versioning enabled.

Get the bucket names from Terraform's outputs:

```powershell
terraform output bucket_name
terraform output archive_bucket_name
```

The `.gitignore` excludes Terraform state, the downloaded provider directory, and `.tfvars` files. Do not commit real credentials or secrets in Terraform variable files.

## Explore S3 with the AWS CLI

After Terraform creates the buckets, use the AWS CLI to upload and download an object. The CLI uses the same S3 commands as it does for AWS; the endpoint setting directs requests to Floci.

```powershell
Set-Content .\hello.txt "My first local cloud object"
aws s3 cp .\hello.txt s3://floci-learning/hello.txt
aws s3 ls s3://floci-learning
aws s3 cp s3://floci-learning/hello.txt .\hello-downloaded.txt
Get-Content .\hello-downloaded.txt
```

Use Terraform rather than the CLI to create or remove buckets so Terraform's state stays accurate.

## Run the C# app

The console app is in `app`. It uses `AWSSDK.S3` and reads the target bucket name from the `S3_BUCKET` environment variable. In the same PowerShell session, from `labs01-s3`, choose which bucket the app should use:

```powershell
$env:S3_BUCKET = terraform output -raw archive_bucket_name
```

To use the basic bucket instead, use:

```powershell
$env:S3_BUCKET = terraform output -raw bucket_name
```

Run the app:

```powershell
dotnet run --project .\app
```

The app options are:

1. **Upload** — choose a numbered file from `app\uploads`. Its filename becomes the S3 object key.
2. **List objects** — list objects in the selected bucket.
3. **Download** — enter an object key; the file is saved in `app\downloaded`.
4. **Delete** — enter an object key and confirm.
0. **Exit** — close the app.

The upload and download folders are created by the app if they do not exist. The menu dispatches to local functions in `Program.cs`; `S3FileService.cs` contains the AWS SDK calls.

## Experiment with S3 versioning

Versioning is enabled on the archive bucket only. Upload a file, change its contents, and upload the same filename again. Because the filename is the object key, S3 stores another version instead of replacing the prior version.

List versions from PowerShell:

```powershell
aws s3api list-object-versions --bucket $env:S3_BUCKET --endpoint-url http://localhost:4566 --no-cli-pager
```

A version with `IsLatest: true` is the current version. Objects uploaded before versioning was enabled can have `VersionId: null`. Uploads made after versioning is enabled receive version IDs.

In a versioned bucket, choosing **Delete** in the app adds a delete marker. The marker hides the object from ordinary listings, while its older versions remain stored. To restore visibility, delete that specific marker by its version ID (replace the example value with the marker's ID):

```powershell
aws s3api delete-object --bucket $env:S3_BUCKET --key test.txt --version-id YOUR_DELETE_MARKER_VERSION_ID --endpoint-url http://localhost:4566
```

To download a specific older version, supply its version ID (replace the example value with the object's version ID). The output filename is the final argument:

```powershell
aws s3api get-object --bucket $env:S3_BUCKET --key test.txt --version-id YOUR_OBJECT_VERSION_ID --endpoint-url http://localhost:4566 .\app\downloaded\test-older-version.txt
```

## Terraform plan and state practice

Try changing a bucket name in `terraform.tfvars` and run `terraform plan`. Terraform should show that the bucket must be replaced because an S3 bucket's name cannot be changed in place. Restore the original value and run `terraform plan` again; do not apply the temporary rename.

Useful state and output commands:

```powershell
terraform state list
terraform state show aws_s3_bucket.archive
terraform output
```

## Cleanup

Before destroying the buckets, remove their contents. For the versioned archive bucket, ordinary object deletion creates delete markers; permanently deleting the bucket requires removing all object versions and delete markers too. Then run:

```powershell
terraform destroy
```

Review the destroy plan and confirm only when you intend to remove the lab buckets.

## What I learned

- S3 stores objects under keys inside buckets.
- AWS CLI and SDK operations work against Floci when configured with its local endpoint.
- Terraform variables provide inputs; outputs expose values such as bucket names.
- Terraform state tracks which resources its configuration manages.
- S3 versioning preserves older object versions; a normal delete in a versioned bucket creates a delete marker.
- A C# application can use the AWS SDK to perform S3 operations without knowing how Terraform created the bucket.