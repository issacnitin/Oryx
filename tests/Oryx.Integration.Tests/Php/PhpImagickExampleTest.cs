﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.Oryx.BuildScriptGenerator.Php;
using Microsoft.Oryx.Common;
using Microsoft.Oryx.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Oryx.Integration.Tests
{
    [Trait("category", "php")]
    public class PhpImagickExampleTest : PhpEndToEndTestsBase
    {
        public PhpImagickExampleTest(ITestOutputHelper output, TestTempDirTestFixture fixture)
            : base(output, fixture)
        {
        }

        [Theory]
        [InlineData("7.4")]
        [InlineData("7.3")]
        [InlineData("7.2")]
        [InlineData("7.0")]
        [InlineData("5.6")]
        public async Task ImagickExample(string phpVersion)
        {
            // Arrange
            var appName = "imagick-example";
            var hostDir = Path.Combine(_hostSamplesDir, "php", appName);
            var volume = DockerVolume.CreateMirror(hostDir);
            var appDir = volume.ContainerDir;
            var buildScript = new ShellScriptBuilder()
               .AddCommand($"oryx build {appDir} --platform {PhpConstants.PlatformName} --language-version {phpVersion}")
               .ToString();
            var runScript = new ShellScriptBuilder()
                .AddCommand($"oryx create-script -appPath {appDir} -output {RunScriptPath}")
                .AddCommand(RunScriptPath)
                .ToString();

            // Act & Assert
            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName, _output, volume,
                "/bin/sh", new[] { "-c", buildScript },
                _imageHelper.GetRuntimeImage("php", phpVersion),
                ContainerPort,
                "/bin/sh", new[] { "-c", runScript },
                async (hostPort) =>
                {
                    string imagickOutput = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Equal("64x64", imagickOutput);
                });
        }
    }
}
