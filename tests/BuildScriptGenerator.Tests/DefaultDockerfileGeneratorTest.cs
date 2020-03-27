﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.DotNetCore;
using Microsoft.Oryx.BuildScriptGenerator.Exceptions;
using Microsoft.Oryx.BuildScriptGenerator.Node;
using Microsoft.Oryx.Tests.Common;
using Xunit;

namespace Microsoft.Oryx.BuildScriptGenerator.Tests
{
    public class DefaultDockerfileGeneratorTest : IClassFixture<TestTempDirTestFixture>
    {
        private const string _buildImageFormat = "mcr.microsoft.com/oryx/build:{0}";
        private const string _argRuntimeFormat = "ARG RUNTIME={0}:{1}";

        private readonly string _tempDirRoot;

        public DefaultDockerfileGeneratorTest(TestTempDirTestFixture testFixture)
        {
            _tempDirRoot = testFixture.RootDirPath;
        }

        [Fact]
        public void GenerateDockerfile_Throws_IfNoPlatformIsCompatible()
        {
            // Arrange
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultDockerfileGenerator(platforms: new IProgrammingPlatform[] { }, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateDockerfile(ctx));
        }

        [Theory]
        [InlineData("dotnet", "2.0", "latest")]
        [InlineData("dotnet", "2.1", "slim")]
        [InlineData("dotnet", "3.0", "latest")]
        [InlineData("nodejs", "6", "latest")]
        [InlineData("nodejs", "8", "slim")]
        [InlineData("nodejs", "10", "slim")]
        [InlineData("nodejs", "12", "slim")]
        [InlineData("php", "5.6", "latest")]
        [InlineData("php", "7.3", "latest")]
        [InlineData("python", "2.7", "latest")]
        [InlineData("python", "3.7", "slim")]
        [InlineData("python", "3.8", "slim")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForProvidedPlatformAndVersion(
            string platformName,
            string platformVersion,
            string expectedBuildTag)
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: platformName,
                detectedLanguageVersion: platformVersion);
            var platform = new TestProgrammingPlatform(
                platformName,
                new[] { platformVersion },
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = platformName,
                PlatformVersion = platformVersion
            };
            var generator = CreateDefaultDockerfileGenerator(platform, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, expectedBuildTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(platformName),
                platformVersion),
                dockerfile);
            Assert.False(detector.DetectInvoked);
        }

        [Theory]
        [InlineData("dotnet", "2.0", "latest")]
        [InlineData("dotnet", "2.1", "slim")]
        [InlineData("dotnet", "3.0", "latest")]
        [InlineData("nodejs", "6", "latest")]
        [InlineData("nodejs", "8", "slim")]
        [InlineData("nodejs", "10", "slim")]
        [InlineData("nodejs", "12", "slim")]
        [InlineData("php", "5.6", "latest")]
        [InlineData("php", "7.3", "latest")]
        [InlineData("python", "2.7", "latest")]
        [InlineData("python", "3.7", "slim")]
        [InlineData("python", "3.8", "slim")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForProvidedPlatform(
            string platformName,
            string detectedPlatformVersion,
            string expectedBuildTag)
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: platformName,
                detectedLanguageVersion: detectedPlatformVersion);
            var platform = new TestProgrammingPlatform(
                platformName,
                new[] { detectedPlatformVersion },
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = platformName,
            };
            var generator = CreateDefaultDockerfileGenerator(platform, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, expectedBuildTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(platformName),
                detectedPlatformVersion),
                dockerfile);
            Assert.True(detector.DetectInvoked);
        }

        [Theory]
        [InlineData("dotnet", "2.0", "latest")]
        [InlineData("dotnet", "2.1", "slim")]
        [InlineData("dotnet", "3.0", "latest")]
        [InlineData("nodejs", "6", "latest")]
        [InlineData("nodejs", "8", "slim")]
        [InlineData("nodejs", "10", "slim")]
        [InlineData("nodejs", "12", "slim")]
        [InlineData("php", "5.6", "latest")]
        [InlineData("php", "7.3", "latest")]
        [InlineData("python", "2.7", "latest")]
        [InlineData("python", "3.7", "slim")]
        [InlineData("python", "3.8", "slim")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForNoProvidedPlatform(
            string detectedPlatformName,
            string detectedPlatformVersion,
            string expectedBuildTag)
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: detectedPlatformName,
                detectedLanguageVersion: detectedPlatformVersion);
            var platform = new TestProgrammingPlatform(
                detectedPlatformName,
                new[] { detectedPlatformVersion },
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultDockerfileGenerator(platform, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, expectedBuildTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(detectedPlatformName),
                detectedPlatformVersion),
                dockerfile);
            Assert.True(detector.DetectInvoked);
        }

        [Theory]
        [InlineData("nodejs", "8", "dotnet", "2.1", "slim")]
        [InlineData("nodejs", "8", "dotnet", "3.0", "latest")]
        [InlineData("nodejs", "12", "dotnet", "2.1", "slim")]
        [InlineData("nodejs", "12", "dotnet", "3.0", "latest")]
        [InlineData("nodejs", "8", "python", "3.7", "slim")]
        [InlineData("nodejs", "8", "python", "2.7", "latest")]
        [InlineData("python", "3.7", "dotnet", "2.1", "slim")]
        [InlineData("python", "3.7", "dotnet", "3.0", "latest")]
        [InlineData("dotnet", "2.1", "php", "5.6", "latest")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForMultiPlatformBuild(
            string platformName,
            string platformVersion,
            string runtimePlatformName,
            string runtimePlatformVersion,
            string expectedBuildTag)
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: platformName,
                detectedLanguageVersion: platformVersion);
            var platform = new TestProgrammingPlatform(
                platformName,
                new[] { platformVersion },
                detector: detector);

            var runtimeDetector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: runtimePlatformName,
                detectedLanguageVersion: runtimePlatformVersion);
            var runtimePlatform = new TestProgrammingPlatform(
                runtimePlatformName,
                new[] { runtimePlatformVersion },
                detector: runtimeDetector);
            var commonOptions = new BuildScriptGeneratorOptions { EnableMultiPlatformBuild = true };
            var generator = CreateDefaultDockerfileGenerator(new[] { platform, runtimePlatform }, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, expectedBuildTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(runtimePlatformName),
                runtimePlatformVersion),
                dockerfile);
        }

        private DockerfileContext CreateDockerfileContext()
        {
            return new DockerfileContext();
        }

        private DefaultDockerfileGenerator CreateDefaultDockerfileGenerator(
            IProgrammingPlatform platform,
            BuildScriptGeneratorOptions commonOptions)
        {
            return CreateDefaultDockerfileGenerator(new[] { platform }, commonOptions);
        }

        private DefaultDockerfileGenerator CreateDefaultDockerfileGenerator(
            IProgrammingPlatform[] platforms,
            BuildScriptGeneratorOptions commonOptions)
        {
            commonOptions = commonOptions ?? new BuildScriptGeneratorOptions();
            var configuration = new TestConfiguration();
            var platformName = commonOptions.PlatformName == "nodejs" ? "node" : commonOptions.PlatformName;
            configuration[$"{platformName}_version"] = commonOptions.PlatformVersion;
            return new DefaultDockerfileGenerator(
                new DefaultCompatiblePlatformDetector(
                    platforms,
                    NullLogger<DefaultCompatiblePlatformDetector>.Instance,
                    Options.Create(commonOptions),
                    configuration),
                NullLogger<DefaultDockerfileGenerator>.Instance,
                Options.Create(commonOptions));
        }

        private string ConvertToRuntimeName(string platformName)
        {
            if (string.Equals(platformName, DotNetCoreConstants.LanguageName, StringComparison.OrdinalIgnoreCase))
            {
                platformName = "dotnetcore";
            }

            if (string.Equals(platformName, NodeConstants.NodeJsName, StringComparison.OrdinalIgnoreCase))
            {
                platformName = "node";
            }

            return platformName;
        }
    }
}
