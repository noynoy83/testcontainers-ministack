using System;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.MiniStack;

/// <inheritdoc cref="ContainerBuilder{TBuilderEntity, TContainerEntity, TConfigurationEntity}" />
public sealed class MiniStackBuilder : ContainerBuilder<MiniStackBuilder, MiniStackContainer, MiniStackConfiguration>
{
    /// <summary>The default MiniStack image.</summary>
    public const string MiniStackImage = "ministackorg/ministack:latest";

    /// <summary>The MiniStack gateway port.</summary>
    public const ushort MiniStackPort = 4566;

    internal const string DefaultAccessKey = "test";
    internal const string DefaultSecretKey = "test";
    internal const string DefaultRegion = "us-east-1";
    private const string DockerSocketPath = "/var/run/docker.sock";

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackBuilder" /> class.
    /// </summary>
    public MiniStackBuilder()
        : this(MiniStackImage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackBuilder" /> class.
    /// </summary>
    /// <param name="image">The full Docker image name, including the tag.</param>
    public MiniStackBuilder(string image)
        : this(new MiniStackConfiguration())
    {
        DockerResourceConfiguration = Init().WithImage(image).DockerResourceConfiguration;
    }

    private MiniStackBuilder(MiniStackConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }

    /// <inheritdoc />
    protected override MiniStackConfiguration DockerResourceConfiguration { get; }

    /// <summary>
    /// Overrides the AWS region the emulator reports (<c>MINISTACK_REGION</c>).
    /// </summary>
    /// <param name="region">The AWS region, e.g. <c>eu-west-1</c>.</param>
    /// <returns>A configured instance of <see cref="MiniStackBuilder" />.</returns>
    public MiniStackBuilder WithRegion(string region)
    {
        return WithEnvironment("MINISTACK_REGION", region);
    }

    /// <summary>
    /// Overrides the AWS credentials the emulator accepts.
    /// </summary>
    /// <param name="accessKey">The AWS access key id.</param>
    /// <param name="secretKey">The AWS secret access key.</param>
    /// <returns>A configured instance of <see cref="MiniStackBuilder" />.</returns>
    public MiniStackBuilder WithCredentials(string accessKey, string secretKey)
    {
        return WithEnvironment("AWS_ACCESS_KEY_ID", accessKey)
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", secretKey);
    }

    /// <summary>
    /// Enables disk persistence (<c>PERSIST_STATE=1</c> and <c>S3_PERSIST=1</c>).
    /// </summary>
    /// <returns>A configured instance of <see cref="MiniStackBuilder" />.</returns>
    public MiniStackBuilder WithPersistence()
    {
        return WithEnvironment("PERSIST_STATE", "1")
            .WithEnvironment("S3_PERSIST", "1");
    }

    /// <summary>
    /// Requires IMDSv2 token-based access for instance metadata calls.
    /// </summary>
    /// <returns>A configured instance of <see cref="MiniStackBuilder" />.</returns>
    public MiniStackBuilder WithImdsV2Required()
    {
        return WithEnvironment("MINISTACK_IMDS_V2_REQUIRED", "1");
    }

    /// <summary>
    /// Activates real infrastructure mode (RDS, ElastiCache, ECS, Lambda, ... run as real containers).
    /// </summary>
    /// <remarks>
    /// <b>Security warning:</b> this bind-mounts the host Docker socket into the MiniStack container.
    /// Anything running inside MiniStack gains root-equivalent control of the host's container engine.
    /// Use only on trusted developer machines or isolated CI runners.
    /// </remarks>
    /// <returns>A configured instance of <see cref="MiniStackBuilder" />.</returns>
    public MiniStackBuilder WithRealInfrastructure()
    {
        return WithBindMount(DockerSocketPath, DockerSocketPath);
    }

    /// <inheritdoc />
    public override MiniStackContainer Build()
    {
        Validate();
        return new MiniStackContainer(DockerResourceConfiguration);
    }

    /// <inheritdoc />
    protected override MiniStackBuilder Init()
    {
        var builder = base.Init()
            .WithPortBinding(MiniStackPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPath("/_ministack/health")
                    .ForPort(MiniStackPort)
                    .ForStatusCode(System.Net.HttpStatusCode.OK)));

        // Route nested real-infra images through the same private registry as the MiniStack image.
        var prefix = TestcontainersSettings.HubImageNamePrefix;
        return string.IsNullOrEmpty(prefix) ? builder : builder.WithEnvironment("MINISTACK_IMAGE_PREFIX", prefix);
    }

    /// <inheritdoc />
    protected override MiniStackBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new MiniStackConfiguration(resourceConfiguration));
    }

    /// <inheritdoc />
    protected override MiniStackBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new MiniStackConfiguration(resourceConfiguration));
    }

    /// <inheritdoc />
    protected override MiniStackBuilder Merge(MiniStackConfiguration oldValue, MiniStackConfiguration newValue)
    {
        return new MiniStackBuilder(new MiniStackConfiguration(oldValue, newValue));
    }
}
