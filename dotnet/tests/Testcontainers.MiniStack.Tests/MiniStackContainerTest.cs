using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Xunit;

namespace Testcontainers.MiniStack.Tests;

public sealed class MiniStackContainerTest : IAsyncLifetime
{
    private readonly MiniStackContainer _ministack = new MiniStackBuilder().Build();

    public Task InitializeAsync() => _ministack.StartAsync();

    public Task DisposeAsync() => _ministack.DisposeAsync().AsTask();

    private T CreateClient<T>() where T : AmazonServiceClient
    {
        var credentials = new BasicAWSCredentials(_ministack.AccessKey, _ministack.SecretKey);
        if (typeof(T) == typeof(AmazonS3Client))
        {
            return (T)(object)new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = _ministack.GetConnectionString(),
                ForcePathStyle = true,
                AuthenticationRegion = _ministack.Region,
            });
        }

        return (T)(object)new AmazonSQSClient(credentials, new AmazonSQSConfig
        {
            ServiceURL = _ministack.GetConnectionString(),
            AuthenticationRegion = _ministack.Region,
        });
    }

    [Fact]
    public void EndpointIsReachableUri()
    {
        Assert.StartsWith("http://", _ministack.GetConnectionString());
        Assert.True(_ministack.Port > 0);
    }

    [Fact]
    public async Task S3CreateAndListBucket()
    {
        using var s3 = CreateClient<AmazonS3Client>();

        await s3.PutBucketAsync("my-bucket");
        var buckets = await s3.ListBucketsAsync();

        Assert.Contains(buckets.Buckets, b => b.BucketName == "my-bucket");
    }

    [Fact]
    public async Task SqsCreateQueue()
    {
        using var sqs = CreateClient<AmazonSQSClient>();

        var queue = await sqs.CreateQueueAsync("my-queue");

        Assert.Contains("my-queue", queue.QueueUrl);
    }
}
