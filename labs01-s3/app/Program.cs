using Amazon.Runtime;
using Amazon.S3;

var credentials = new BasicAWSCredentials("test", "test");

var config = new AmazonS3Config
{
    ServiceURL = "http://localhost:4566",
    AuthenticationRegion = "us-east-1",
    ForcePathStyle = true
};

using var s3Client = new AmazonS3Client(credentials, config);
var s3Service = new S3FileService(s3Client);
var configuredBucketName = Environment.GetEnvironmentVariable("S3_BUCKET");

if (string.IsNullOrWhiteSpace(configuredBucketName))
{
    Console.WriteLine("S3_BUCKET is not set.");
    return;
}

var bucketName = configuredBucketName;

// The program runs from bin/Debug/<target framework>; go up to the project folder.
var appFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var uploadsFolder = Path.Combine(appFolder, "uploads");
var downloadedFolder = Path.Combine(appFolder, "downloaded");

Directory.CreateDirectory(uploadsFolder);
Directory.CreateDirectory(downloadedFolder);

bool isRunning = true;

while (isRunning)
{
    Console.WriteLine();
    Console.WriteLine("Floci S3 Demo");
    Console.WriteLine("1. Upload");
    Console.WriteLine("2. List objects");
    Console.WriteLine("3. Download");
    Console.WriteLine("4. Delete");
    Console.WriteLine("0. Exit");
    Console.Write("Choose an option: ");

    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1":
                await UploadFromFolderAsync();
                break;
            case "2":
                await ListBucketObjectsAsync();
                break;
            case "3":
                await DownloadObjectAsync();
                break;
            case "4":
                await DeleteObjectAsync();
                break;
            case "0":
                ExitProgram();
                break;
            default:
                ShowInvalidOption();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Operation failed: {ex.Message}");
    }
}

async Task UploadFromFolderAsync()
{
    var uploadFiles = Directory.GetFiles(uploadsFolder)
        .OrderBy(filePath => Path.GetFileName(filePath), StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (uploadFiles.Length == 0)
    {
        Console.WriteLine($"No files found. Add a file to: {uploadsFolder}");
        return;
    }

    Console.WriteLine("Files available to upload:");
    for (var index = 0; index < uploadFiles.Length; index++)
    {
        Console.WriteLine($"{index + 1}. {Path.GetFileName(uploadFiles[index])}");
    }

    Console.Write("Choose a file number: ");
    if (!int.TryParse(Console.ReadLine(), out var fileNumber)
        || fileNumber < 1
        || fileNumber > uploadFiles.Length)
    {
        Console.WriteLine("That is not a valid file number.");
        return;
    }

    var selectedFile = uploadFiles[fileNumber - 1];
    var key = Path.GetFileName(selectedFile);
    await s3Service.UploadFileAsync(bucketName, key, selectedFile);
    Console.WriteLine($"Uploaded {key} to {bucketName}.");
}

async Task ListBucketObjectsAsync()
{
    var objects = await s3Service.ListObjectsAsync(bucketName);

    if (objects.Count == 0)
    {
        Console.WriteLine("The bucket is empty.");
        return;
    }

    Console.WriteLine($"Objects in {bucketName}:");
    foreach (var item in objects)
    {
        Console.WriteLine($"-{item.Key} ({item.Size} bytes)");
    }
}

async Task DownloadObjectAsync()
{
    Console.Write("S3 object key to download: ");
    var key = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(key))
    {
        Console.WriteLine("An object key is required.");
        return;
    }

    var fileName = Path.GetFileName(key);
    if (string.IsNullOrWhiteSpace(fileName))
    {
        Console.WriteLine("That object key does not have a file name to save.");
        return;
    }

    var downloadPath = Path.Combine(downloadedFolder, fileName);
    if (File.Exists(downloadPath))
    {
        Console.Write($"{fileName} already exists in downloaded. Overwrite it? (y/N): ");
        var confirmation = Console.ReadLine()?.Trim();
        if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Download cancelled.");
            return;
        }
    }

    await s3Service.DownloadFileAsync(bucketName, key, downloadPath);
    Console.WriteLine($"Downloaded {key} to {downloadPath}");
}

async Task DeleteObjectAsync()
{
    Console.Write("S3 object key to delete: ");
    var key = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(key))
    {
        Console.WriteLine("An object key is required.");
        return;
    }

    Console.Write($"Delete '{key}' from '{bucketName}'? (y/N): ");
    var confirmation = Console.ReadLine()?.Trim();
    if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Delete cancelled.");
        return;
    }

    await s3Service.DeleteFileAsync(bucketName, key);
    Console.WriteLine($"Deleted {key}");
}

void ExitProgram()
{
    isRunning = false;
}

void ShowInvalidOption()
{
    Console.WriteLine("Please choose a listed option.");
}