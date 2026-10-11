using System.Threading.Tasks;
using Xunit;

namespace Testcontainers.MiniStack.Tests;

public sealed class MiniStackBuilderTest
{
    [Fact]
    public void DefaultsWithoutStarting()
    {
        var container = new MiniStackBuilder().Build();

        Assert.Equal("test", container.AccessKey);
        Assert.Equal("test", container.SecretKey);
        Assert.Equal("us-east-1", container.Region);
        Assert.Equal("latest", container.MiniStackVersion);
    }

    [Fact]
    public void ConfigurationIsReflectedInAccessors()
    {
        var container = new MiniStackBuilder("ministackorg/ministack:1.3.42")
            .WithRegion("eu-west-1")
            .WithCredentials("AKIAEXAMPLE000000000", "secret")
            .WithPersistence()
            .WithImdsV2Required()
            .Build();

        Assert.Equal("eu-west-1", container.Region);
        Assert.Equal("AKIAEXAMPLE000000000", container.AccessKey);
        Assert.Equal("secret", container.SecretKey);
        Assert.Equal("1.3.42", container.MiniStackVersion);
    }
}
