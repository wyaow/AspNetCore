// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for components.
    /// </summary>
    public static class ComponentServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>An <see cref="IServerSideBlazorBuilder"/> that can be used to further customize the configuration.</returns>
        public static IServerSideBlazorBuilder AddServerSideBlazor(this IServiceCollection services)
        {
            var builder = new DefaultServerSideBlazorBuilder(services);

            // This call INTENTIONALLY uses the AddHubOptions on the SignalR builder, because it will merge
            // the global HubOptions before running the configure callback. We want to ensure that happens
            // once. Our AddHubOptions method doesn't do this.
            //
            // We need to restrict the set of protocols used by default to our specialized one. Users have
            // the chance to modify options further via the builder.
            //
            // Other than the options, the things exposed by the SignalR builder aren't very meaningful in
            // the Server-Side Blazor context and thus aren't exposed.
            services.AddSignalR().AddHubOptions<ComponentHub>(options =>
            {
                options.SupportedProtocols.Clear();
                options.SupportedProtocols.Add(BlazorPackHubProtocol.ProtocolName);
            });

            // Register the Blazor specific hub protocol
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, BlazorPackHubProtocol>());

            // Here we add a bunch of services that don't vary in any way based on the
            // user's configuration. So even if the user has multiple independent server-side
            // Components entrypoints, this lot is the same and repeated registrations are a no-op.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<StaticFileOptions>, ConfigureStaticFilesOptions>());
            services.TryAddSingleton<CircuitFactory, DefaultCircuitFactory>();

            services.TryAddSingleton<CircuitIdFactory>();
            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IPostConfigureOptions<CircuitOptions>, PostConfigureCircuitOptionsCircuitIdDataprotector>());

            services.TryAddScoped(s => s.GetRequiredService<ICircuitAccessor>().Circuit);
            services.TryAddScoped<ICircuitAccessor, DefaultCircuitAccessor>();

            services.TryAddSingleton<CircuitRegistry>();

            // We explicitly take over the prerendering and components services here.
            // We can't have two separate component implementations coexisting at the
            // same time, so when you register components (Circuits) it takes over
            // all the abstractions.
            services.AddScoped<IComponentPrerenderer, CircuitPrerenderer>();

            // Standard razor component services implementations
            //
            // These intentionally replace the non-interactive versions included in MVC.
            services.AddScoped<IUriHelper, RemoteUriHelper>();
            services.AddScoped<IJSRuntime, RemoteJSRuntime>();
            services.AddScoped<INavigationInterception, RemoteNavigationInterception>();
            services.AddScoped<IComponentContext, RemoteComponentContext>();
            services.AddScoped<AuthenticationStateProvider, FixedAuthenticationStateProvider>();

            return builder;
        }

        private class DefaultServerSideBlazorBuilder : IServerSideBlazorBuilder
        {
            public DefaultServerSideBlazorBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
