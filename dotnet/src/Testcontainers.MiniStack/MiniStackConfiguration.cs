using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.MiniStack;

/// <inheritdoc cref="ContainerConfiguration" />
public sealed class MiniStackConfiguration : ContainerConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackConfiguration" /> class.
    /// </summary>
    public MiniStackConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public MiniStackConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public MiniStackConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniStackConfiguration" /> class.
    /// </summary>
    /// <param name="oldValue">The old Docker resource configuration.</param>
    /// <param name="newValue">The new Docker resource configuration.</param>
    public MiniStackConfiguration(MiniStackConfiguration oldValue, MiniStackConfiguration newValue)
        : base(oldValue, newValue)
    {
    }
}
