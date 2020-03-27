﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.BuildScriptGenerator.Exceptions;

namespace Microsoft.Oryx.BuildScriptGeneratorCli
{
    internal class BuildScriptGenerator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConsole _console;
        private readonly List<ICheckerMessage> _checkerMessageSink;
        private readonly ILogger<BuildScriptGenerator> _logger;
        private readonly string _operationId;

        public BuildScriptGenerator(
            IServiceProvider serviceProvider,
            IConsole console,
            List<ICheckerMessage> checkerMessageSink,
            string operationId)
        {
            _console = console;
            _serviceProvider = serviceProvider;
            _checkerMessageSink = checkerMessageSink;
            _logger = _serviceProvider.GetRequiredService<ILogger<BuildScriptGenerator>>();
            _operationId = operationId;
        }

        public static BuildScriptGeneratorContext CreateContext(IServiceProvider serviceProvider, string operationId)
        {
            var options = serviceProvider.GetRequiredService<IOptions<BuildScriptGeneratorOptions>>().Value;
            var sourceRepoProvider = serviceProvider.GetRequiredService<ISourceRepoProvider>();
            var envSettings = serviceProvider.GetRequiredService<CliEnvironmentSettings>();

            return new BuildScriptGeneratorContext
            {
                OperationId = operationId,
                SourceRepo = sourceRepoProvider.GetSourceRepo(),
                Properties = options.Properties,
                ManifestDir = options.ManifestDir,
            };
        }

        public bool TryGenerateScript(out string generatedScript, out Exception exception)
        {
            generatedScript = null;
            exception = null;

            try
            {
                var scriptGenCtx = CreateContext(_serviceProvider, _operationId);
                var scriptGen = _serviceProvider.GetRequiredService<IBuildScriptGenerator>();

                scriptGen.GenerateBashScript(scriptGenCtx, out generatedScript, _checkerMessageSink);
                return true;
            }
            catch (InvalidUsageException ex)
            {
                exception = ex;
                _logger.LogError(ex, "Invalid usage");
                _console.WriteErrorLine(ex.Message);
                return false;
            }
        }
    }
}