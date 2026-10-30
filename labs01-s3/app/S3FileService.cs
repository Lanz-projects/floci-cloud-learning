using Amazon.S3;
using Amazon.S3.Model;


public class S3FileService
{
    private readonly IAmazonS3 _client;

    public S3FileService(IAmazonS3 client)
    {
      _client = client;
    }

    public async Task<List<S3Object>> ListObjectsAsync(string BucketName)
    {
      var request = new ListObjectsV2Request
      {
        BucketName = BucketName
      };

      var response = await _client.ListObjectsV2Async(request);
      return response.S3Objects;
    }

    public async Task UploadFileAsync(string bucketName, string key, string filePath)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            FilePath = filePath
        };

        await _client.PutObjectAsync(request);
    }

    public async Task DownloadFileAsync(string bucketName, string key, string downloadPath)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };

        using var response = await _client.GetObjectAsync(request);
        await response.WriteResponseStreamToFileAsync(
            downloadPath,
            false,
            CancellationToken.None);
    }

    public async Task DeleteFileAsync(string bucketName, string key)
    {
      var request = new DeleteObjectRequest
      {
          BucketName = bucketName,
          Key = key
      }; 

      await _client.DeleteObjectAsync(request);
    }
}