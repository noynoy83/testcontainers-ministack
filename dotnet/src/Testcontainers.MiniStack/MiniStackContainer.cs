using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Testcontainers.MiniStack;

/// <inheritdoc cref="DockerContainer" />
public sealed class MiniStackContainer : DockerContainer
{
    private readonly MiniStackConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackContainer" /> class.
    /// </summary>
    /// <param name="configuration">The container configuration.</param>
    public MiniStackContainer(MiniStackConfiguration configuration)
        : base(configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the AWS access key id.
    /// </summary>
    public string AccessKey => GetEnvironment("AWS_ACCESS_KEY_ID", MiniStackBuilder.DefaultAccessKey);

    /// <summary>
    /// Gets the AWS secret access key.
    /// </summary>
    public string SecretKey => GetEnvironment("AWS_SECRET_ACCESS_KEY", MiniStackBuilder.DefaultSecretKey);

    /// <summary>
    /// Gets the AWS region.
    /// </summary>
    public string Region => GetEnvironment("MINISTACK_REGION", MiniStackBuilder.DefaultRegion);

    /// <summary>
    /// Gets the MiniStack version, i.e. the image tag verbatim (<c>latest</c> if absent).
    /// </summary>
    public string MiniStackVersion => _configuration.Image.Tag ?? "latest";

    /// <summary>
    /// Gets the host port mapped to the MiniStack gateway.
    /// </summary>
    public ushort Port => GetMappedPublicPort(MiniStackBuilder.MiniStackPort);

    /// <summary>
    /// Gets the endpoint URL to configure AWS SDK clients with, e.g. <c>http://localhost:32789</c>.
    /// </summary>
    /// <returns>The endpoint URL.</returns>
    public string GetConnectionString()
    {
        return new UriBuilder(Uri.UriSchemeHttp, Hostname, Port).ToString().TrimEnd('/');
    }

    private string GetEnvironment(string key, string fallback)
    {
        return _configuration.Environments != null && _configuration.Environments.TryGetValue(key, out var value)
            ? value
            : fallback;
    }

    /// <summary>
    /// Stops the container and reaps the nested containers and volumes MiniStack spawned for
    /// real-infrastructure services (RDS, ElastiCache, ECS, EKS, Lambda). They live on the host engine,
    /// so the Testcontainers resource reaper does not track them. Cleanup is best-effort and never throws.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the container is stopped.</returns>
    protected override async Task UnsafeStopAsync(CancellationToken ct = default)
    {
        try
        {
            await base.UnsafeStopAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            await ReapMiniStackResourcesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
        finally
        {
            await ReapMiniStackResourcesAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static async Task ReapMiniStackResourcesAsync(CancellationToken ct)
    {
        try
        {
            using var client = TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid.NewGuid()).Build();
            var labelFilter = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool> { ["ministack"] = true },
            };

            try
            {
                var orphans = await client.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true, Filters = labelFilter }, ct).ConfigureAwait(false);
                foreach (var orphan in orphans)
                {
                    try
                    {
                        await client.Containers.RemoveContainerAsync(
                            orphan.ID, new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Best-effort: a stuck remove must never mask a test failure.
                    }
                }
            }
            catch (Exception)
            {
                // Best-effort.
            }

            // Named RDS volumes outlive their containers; remove any carrying the ministack label.
            try
            {
                var volumes = await client.Volumes.ListAsync(new VolumesListParameters { Filters = labelFilter }, ct).ConfigureAwait(false);
                foreach (var volume in volumes.Volumes ?? new List<VolumeResponse>())
                {
                    try
                    {
                        await client.Volumes.RemoveAsync(volume.Name, true, ct).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Best-effort.
                    }
                }
            }
            catch (Exception)
            {
                // Best-effort.
            }
        }
        catch (Exception)
        {
            // Docker client unavailable: skip cleanup.
        }
    }
}
