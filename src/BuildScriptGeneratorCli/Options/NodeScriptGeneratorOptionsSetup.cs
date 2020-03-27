﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.Node;

namespace Microsoft.Oryx.BuildScriptGeneratorCli.Options
{
    /// <summary>
    /// Gets hierarchical configuration from IConfiguration api and binds the properties on NodeScriptGeneratorOptions.
    /// </summary>
    public class NodeScriptGeneratorOptionsSetup : OptionsSetupBase, IConfigureOptions<NodeScriptGeneratorOptions>
    {
        public NodeScriptGeneratorOptionsSetup(IConfiguration configuration)
            : base(configuration)
        {
        }

        public void Configure(NodeScriptGeneratorOptions options)
        {
            options.CustomNpmRunBuildCommand = GetStringValue(SettingsKeys.CustomNpmRunBuildCommand);
            options.PruneDevDependencies = GetBooleanValue(SettingsKeys.PruneDevDependencies);
            options.NpmRegistryUrl = GetStringValue(SettingsKeys.NpmRegistryUrl);
        }
    }
}
