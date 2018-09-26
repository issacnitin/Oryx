﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.BuildScriptGenerator.Exceptions;

namespace Microsoft.Oryx.BuildScriptGeneratorCli
{
    internal class ScriptGenerator
    {
        private readonly IConsole _console;
        private readonly IServiceProvider _serviceProvider;

        public ScriptGenerator(
            IConsole console,
            IServiceProvider serviceProvider)
        {
            _console = console;
            _serviceProvider = serviceProvider;
        }

        public bool TryGenerateScript(out string generatedScript)
        {
            generatedScript = null;

            var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var options = _serviceProvider.GetRequiredService<IOptions<BuildScriptGeneratorOptions>>().Value;
                var scriptGeneratorProvider = _serviceProvider.GetRequiredService<IScriptGeneratorProvider>();
                var sourceRepoProvider = _serviceProvider.GetRequiredService<ISourceRepoProvider>();

                // Create a root temp directory for this tool under which all temporary files
                // generated by different services can be placed under.
                EnsureTempDirectory(options);

                var sourceRepo = sourceRepoProvider.GetSourceRepo();
                var scriptGeneratorContext = new ScriptGeneratorContext
                {
                    SourceRepo = sourceRepo,
                    LanguageName = options.LanguageName,
                    LanguageVersion = options.LanguageVersion,
                    OutputFolder = options.OutputFolder,
                    TempDirectory = options.TempDirectory,
                };
                logger.LogInformation(
                    "Language name: " + options.LanguageName + "\nLanguage version: " + options.LanguageVersion);

                // Get script generator
                var scriptGenerator = scriptGeneratorProvider.GetScriptGenerator(scriptGeneratorContext);
                if (scriptGenerator == null)
                {
                    _console.WriteLine(
                        "Error: Could not find a script generator which can generate a script for " +
                        $"the code in '{options.SourceCodeFolder}'.");
                    return false;
                }

                generatedScript = scriptGenerator.GenerateBashScript(scriptGeneratorContext);

                return true;
            }
            catch (InvalidUsageException ex)
            {
                _console.WriteLine(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while running this tool:" + Environment.NewLine + ex.ToString());
                _console.WriteLine("Oops... An unexpected error has occurred.");
                return false;
            }
        }

        private static void EnsureTempDirectory(BuildScriptGeneratorOptions options)
        {
            if (string.IsNullOrEmpty(options.TempDirectory))
            {
                throw new InvalidOperationException(
                    $"'{nameof(BuildScriptGeneratorOptions.TempDirectory)}' cannot be null or empty.");
            }

            Directory.CreateDirectory(options.TempDirectory);
        }
    }
}