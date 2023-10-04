// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

public static class RedisContainerBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<RedisContainerComponent> AddRedisContainer(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var redis = new RedisContainerComponent();

        var componentBuilder = builder.AddComponent(name, redis);
        componentBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 6379)); // Internal port is always 6379.
        componentBuilder.WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "latest" });
        return componentBuilder;
    }

    public static IDistributedApplicationComponentBuilder<T> WithRedis<T>(this IDistributedApplicationComponentBuilder<T> projectBuilder, IDistributedApplicationComponentBuilder<RedisContainerComponent> redisBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        if (string.IsNullOrEmpty(connectionName) && !redisBuilder.Component.TryGetName(out connectionName))
        {
            throw new DistributedApplicationException("Redis connection name could not be determined. Please provide one.");
        }

        return projectBuilder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, () =>
        {
            if (!redisBuilder.Component.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
            {
                throw new DistributedApplicationException("Redis component does not have endpoint annotation.");
            }

            // We should only have one endpoint for Redis for local scenarios.
            var endpoint = allocatedEndpoints.Single();
            return endpoint.EndPointString;
        });
    }

    public static IDistributedApplicationComponentBuilder<T> WithRedis<T>(this IDistributedApplicationComponentBuilder<T> projectBuilder, string connectionName, string connectionString)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return projectBuilder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, connectionString);
    }
}
