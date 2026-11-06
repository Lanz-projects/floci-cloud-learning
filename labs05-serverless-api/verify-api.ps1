param(
  [string]$ApiUrl
)

$ErrorActionPreference = 'Stop'

function Invoke-ApiCheck {
  param(
    [string]$Name,
    [string]$Method,
    [string]$Path,
    [int]$ExpectedStatus,
    [string]$ExpectedText,
    [string]$Body
  )

  $parameters = @{
    Uri = "$ApiUrl$Path"
    Method = $Method
    UseBasicParsing = $true
    ErrorAction = 'Stop'
  }
  if ($Body) {
    $parameters.ContentType = 'application/json'
    $parameters.Body = $Body
  }

  try {
    $response = Invoke-WebRequest @parameters
    $status = [int]$response.StatusCode
    $content = $response.Content
  }
  catch {
    if (-not $_.Exception.Response) { throw }
    $status = [int]$_.Exception.Response.StatusCode
    $content = $_.ErrorDetails.Message
  }

  if ($status -ne $ExpectedStatus) {
    throw "$Name failed: expected HTTP $ExpectedStatus, got $status. Response: $content"
  }
  if ($content -notlike "*$ExpectedText*") {
    throw "$Name failed: response did not contain '$ExpectedText'. Response: $content"
  }
  Write-Host "PASS $Name (HTTP $status)"
}

if (-not $ApiUrl) {
  Push-Location $PSScriptRoot
  try {
    $ApiUrl = terraform output -raw orders_api_local_url
    if ($LASTEXITCODE -ne 0 -or -not $ApiUrl) {
      throw 'Could not read orders_api_local_url. Apply the lab Terraform configuration first.'
    }
  }
  finally {
    Pop-Location
  }
}
$ApiUrl = $ApiUrl.TrimEnd('/')

# A unique key avoids overwriting orders created during other lab exercises.
$orderId = "verification-$([guid]::NewGuid().ToString('N'))"
$customerId = 'verification-customer'
$order = @{
  customerId = $customerId
  orderId = $orderId
  status = 'Pending'
  total = 49.95
} | ConvertTo-Json -Compress
$invalidOrder = @{
  customerId = ''
  orderId = $orderId
  status = 'Pending'
  total = 49.95
} | ConvertTo-Json -Compress

Invoke-ApiCheck -Name 'Create order' -Method POST -Path '/orders' -ExpectedStatus 201 -ExpectedText $orderId -Body $order
Invoke-ApiCheck -Name 'Get order' -Method GET -Path "/orders/$customerId/$orderId" -ExpectedStatus 200 -ExpectedText 'Pending, total 49.95'
Invoke-ApiCheck -Name 'Invalid order' -Method POST -Path '/orders' -ExpectedStatus 400 -ExpectedText 'required' -Body $invalidOrder
Invoke-ApiCheck -Name 'Missing order' -Method GET -Path "/orders/$customerId/missing-$orderId" -ExpectedStatus 404 -ExpectedText 'not found'

Write-Host "All checks passed. The created order remains in DynamoDB: $customerId / $orderId"
