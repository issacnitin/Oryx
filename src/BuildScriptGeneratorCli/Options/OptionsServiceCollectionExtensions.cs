﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.BuildScriptGenerator.DotNetCore;
using Microsoft.Oryx.BuildScriptGenerator.Node;
using Microsoft.Oryx.BuildScriptGenerator.Php;
using Microsoft.Oryx.BuildScriptGenerator.Python;

namespace Microsoft.Oryx.BuildScriptGeneratorCli.Options
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddOptionsServices(this IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<BuildScriptGeneratorOptions>, BuildScriptGeneratorOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<NodeScriptGeneratorOptions>, NodeScriptGeneratorOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<DotNetCoreScriptGeneratorOptions>, DotNetCoreScriptGeneratorOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<PhpScriptGeneratorOptions>, PhpScriptGeneratorOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<PythonScriptGeneratorOptions>, PythonScriptGeneratorOptionsSetup>());
            return services;
        }
    }
}
