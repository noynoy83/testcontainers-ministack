using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.ElastiCache;
using Amazon.ElastiCache.Model;
using Amazon.Runtime;
using Xunit;

namespace Testcontainers.MiniStack.Tests;

public sealed class MiniStackRealInfrastructureTest
{
    private static string[] ListMiniStackLabeledContainers()
    {
        using var process = Process.Start(new ProcessStartInfo("docker", "ps -aq --filter label=ministack")
        {
            RedirectStandardOutput = true,
        })!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public async Task ElastiCacheClusterIsReapedOnStop()
    {
        var ministack = new MiniStackBuilder().WithRealInfrastructure().Build();
        await ministack.StartAsync();
        try
        {
            using var client = new AmazonElastiCacheClient(
                new BasicAWSCredentials(ministack.AccessKey, ministack.SecretKey),
                new AmazonElastiCacheConfig { ServiceURL = ministack.GetConnectionString(), AuthenticationRegion = ministack.Region });

            var response = await client.CreateCacheClusterAsync(new CreateCacheClusterRequest
            {
                CacheClusterId = "tc-redis",
                Engine = "redis",
                CacheNodeType = "cache.t3.micro",
                NumCacheNodes = 1,
            });

            Assert.Equal("redis", response.CacheCluster.Engine);

            // The nested Redis container is spawned asynchronously.
            for (var i = 0; i < 30 && ListMiniStackLabeledContainers().Length == 0; i++)
            {
                await Task.Delay(1000);
            }

            Assert.NotEmpty(ListMiniStackLabeledContainers());
        }
        finally
        {
            await ministack.DisposeAsync();
        }

        Assert.Empty(ListMiniStackLabeledContainers());
    }
}
