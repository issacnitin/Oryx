﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.Node;
using Microsoft.Oryx.Tests.Common;
using Xunit;

namespace Microsoft.Oryx.BuildScriptGenerator.Tests.Node
{
    public class NodePlatformTest
    {
        [Fact]
        public void GeneratedBuildSnippet_HasCustomNpmRunBuildCommand_EvenIfPackageJsonHasBuildNodes()
        {
            // Arrange
            const string packageJson = @"{
              ""main"": ""server.js"",
              ""scripts"": {
                ""build"": ""build-node"",
                ""build:azure"": ""azure-node"",
              },
            }";
            var expectedText = "custom-npm-run-build";
            var commonOptions = new BuildScriptGeneratorOptions();
            var nodePlatform = CreateNodePlatform(
                commonOptions,
                new NodeScriptGeneratorOptions { CustomNpmRunBuildCommand = expectedText },
                new NodePlatformInstaller(Options.Create(commonOptions)));
            var repo = new MemorySourceRepo();
            repo.AddFile(packageJson, NodeConstants.PackageJsonFileName);
            var context = CreateContext(repo);
            context.ResolvedNodeVersion = "10.10";

            // Act
            var buildScriptSnippet = nodePlatform.GenerateBashBuildScriptSnippet(context);

            // Assert
            Assert.NotNull(buildScriptSnippet);
            Assert.Contains(expectedText, buildScriptSnippet.BashBuildScriptSnippet);
            Assert.DoesNotContain("npm run build", buildScriptSnippet.BashBuildScriptSnippet);
            Assert.DoesNotContain("npm run build:azure", buildScriptSnippet.BashBuildScriptSnippet);
        }

        [Fact]
        public void GeneratedBuildSnippet_HasNpmRunBuildCommand()
        {
            // Arrange
            const string packageJson = @"{
              ""main"": ""server.js"",
              ""scripts"": {
                ""build"": ""build-node"",
              },
            }";
            var commonOptions = new BuildScriptGeneratorOptions();
            var nodePlatform = CreateNodePlatform(
                commonOptions,
                new NodeScriptGeneratorOptions { CustomNpmRunBuildCommand = null },
                new NodePlatformInstaller(Options.Create(commonOptions)));
            var repo = new MemorySourceRepo();
            repo.AddFile(packageJson, NodeConstants.PackageJsonFileName);
            var context = CreateContext(repo);
            context.ResolvedNodeVersion = "10.10";

            // Act
            var buildScriptSnippet = nodePlatform.GenerateBashBuildScriptSnippet(context);

            // Assert
            Assert.NotNull(buildScriptSnippet);
            Assert.DoesNotContain("npm run build:azure", buildScriptSnippet.BashBuildScriptSnippet);
            Assert.Contains("npm run build", buildScriptSnippet.BashBuildScriptSnippet);
        }

        [Fact]
        public void GeneratedBuildSnippet_HasNpmRunBuildAzureCommand()
        {
            // Arrange
            const string packageJson = @"{
              ""main"": ""server.js"",
              ""scripts"": {
                ""build:azure"": ""build-azure-node"",
              },
            }";
            var commonOptions = new BuildScriptGeneratorOptions();
            var nodePlatform = CreateNodePlatform(
                commonOptions,
                new NodeScriptGeneratorOptions { CustomNpmRunBuildCommand = null },
                new NodePlatformInstaller(Options.Create(commonOptions)));
            var repo = new MemorySourceRepo();
            repo.AddFile(packageJson, NodeConstants.PackageJsonFileName);
            var context = CreateContext(repo);
            context.ResolvedNodeVersion = "10.10";

            // Act
            var buildScriptSnippet = nodePlatform.GenerateBashBuildScriptSnippet(context);

            // Assert
            Assert.NotNull(buildScriptSnippet);
            Assert.Contains("npm run build:azure", buildScriptSnippet.BashBuildScriptSnippet);
        }

        [Fact]
        public void BuildScript_HasSdkInstallScript_IfDynamicInstallIsEnabled_AndSdkIsNotAlreadyInstalled()
        {
            // Arrange
            var nodePlatform = CreateNodePlatform(dynamicInstallIsEnabled: true, sdkAlreadyInstalled: false);
            var repo = new MemorySourceRepo();
            repo.AddFile(string.Empty, NodeConstants.PackageJsonFileName);
            var context = CreateContext(repo);

            // Act
            var buildScriptSnippet = nodePlatform.GenerateBashBuildScriptSnippet(context);

            // Assert
            Assert.NotNull(buildScriptSnippet);
            Assert.NotNull(buildScriptSnippet.PlatformInstallationScriptSnippet);
            Assert.Equal(
                TestNodePlatformInstaller.InstallerScript,
                buildScriptSnippet.PlatformInstallationScriptSnippet);
        }

        [Fact]
        public void BuildScript_HasNoSdkInstallScript_IfDynamicInstallIsEnabled_AndSdkIsAlreadyInstalled()
        {
            // Arrange
            var nodePlatform = CreateNodePlatform(dynamicInstallIsEnabled: true, sdkAlreadyInstalled: true);
            var repo = new MemorySourceRepo();
            repo.AddFile(string.Empty, NodeConstants.PackageJsonFileName);
            var context = CreateContext(repo);

            // Act
            var buildScriptSnippet = nodePlatform.GenerateBashBuildScriptSnippet(context);

            // Assert
            Assert.NotNull(buildScriptSnippet);
            Assert.Null(buildScriptSnippet.PlatformInstallationScriptSnippet);
        }

        [Fact]
        public void BuildScript_DoesNotHaveSdkInstallScript_IfDynamicInstallNotEnabled_AndSdkIsNotAlreadyInstalled()
        {
            // Arrange
            var nodePlatform = CreateNodePlatform(dynamicInstallIsEnabled: false, sdkAlreadyInstalled: false);
            var repo = new MemorySourceRepo();
            repo.AddFile(string.Empty, NodeConstants.PackageJsonFileName);
            var context = CreateContext(repo);

            // Act
            var buildScriptSnippet = nodePlatform.GenerateBashBuildScriptSnippet(context);

            // Assert
            Assert.NotNull(buildScriptSnippet);
            Assert.Null(buildScriptSnippet.PlatformInstallationScriptSnippet);
        }

        private TestNodePlatform CreateNodePlatform(
            BuildScriptGeneratorOptions commonOptions,
            NodeScriptGeneratorOptions nodeScriptGeneratorOptions,
            NodePlatformInstaller platformInstaller)
        {
            var environment = new TestEnvironment();

            var versionProvider = new TestNodeVersionProvider();
            var detector = new TestNodeLanguageDetector(
                versionProvider,
                Options.Create(nodeScriptGeneratorOptions),
                NullLogger<NodeLanguageDetector>.Instance,
                environment,
                new TestStandardOutputWriter());

            return new TestNodePlatform(
                Options.Create(commonOptions),
                Options.Create(nodeScriptGeneratorOptions),
                versionProvider,
                NullLogger<NodePlatform>.Instance,
                detector,
                environment,
                platformInstaller);
        }

        private TestNodePlatform CreateNodePlatform(
            bool dynamicInstallIsEnabled,
            bool sdkAlreadyInstalled)
        {
            var cliOptions = new BuildScriptGeneratorOptions();
            cliOptions.EnableDynamicInstall = dynamicInstallIsEnabled;
            var environment = new TestEnvironment();
            var installer = new TestNodePlatformInstaller(
                Options.Create(cliOptions),
                sdkAlreadyInstalled);

            var versionProvider = new TestNodeVersionProvider();
            var nodeScriptGeneratorOptions = new NodeScriptGeneratorOptions();
            var detector = new TestNodeLanguageDetector(
                versionProvider,
                Options.Create(nodeScriptGeneratorOptions),
                NullLogger<NodeLanguageDetector>.Instance,
                environment,
                new TestStandardOutputWriter());

            return new TestNodePlatform(
                Options.Create(cliOptions),
                Options.Create(nodeScriptGeneratorOptions),
                versionProvider,
                NullLogger<NodePlatform>.Instance,
                detector,
                environment,
                installer);
        }

        private BuildScriptGeneratorContext CreateContext(ISourceRepo sourceRepo)
        {
            return new BuildScriptGeneratorContext
            {
                SourceRepo = sourceRepo,
            };
        }

        private class TestNodePlatform : NodePlatform
        {
            public TestNodePlatform(
                IOptions<BuildScriptGeneratorOptions> cliOptions,
                IOptions<NodeScriptGeneratorOptions> nodeScriptGeneratorOptions,
                INodeVersionProvider nodeVersionProvider,
                ILogger<NodePlatform> logger,
                NodeLanguageDetector detector,
                IEnvironment environment,
                NodePlatformInstaller nodePlatformInstaller)
                : base(
                      cliOptions,
                      nodeScriptGeneratorOptions,
                      nodeVersionProvider,
                      logger,
                      detector,
                      environment,
                      nodePlatformInstaller)
            {
            }
        }

        private class TestNodePlatformInstaller : NodePlatformInstaller
        {
            private readonly bool _sdkIsAlreadyInstalled;
            public static string InstallerScript = "installer-script-snippet";

            public TestNodePlatformInstaller(
                IOptions<BuildScriptGeneratorOptions> cliOptions,
                bool sdkIsAlreadyInstalled)
                : base(cliOptions)
            {
                _sdkIsAlreadyInstalled = sdkIsAlreadyInstalled;
            }

            public override bool IsVersionAlreadyInstalled(string version)
            {
                return _sdkIsAlreadyInstalled;
            }

            public override string GetInstallerScriptSnippet(string version)
            {
                return InstallerScript;
            }
        }

        private class TestNodeLanguageDetector : NodeLanguageDetector
        {
            public TestNodeLanguageDetector(
                INodeVersionProvider nodeVersionProvider,
                IOptions<NodeScriptGeneratorOptions> nodeScriptGeneratorOptions,
                ILogger<NodeLanguageDetector> logger,
                IEnvironment environment,
                IStandardOutputWriter writer)
                : base(nodeVersionProvider, nodeScriptGeneratorOptions, logger, environment, writer)
            {
            }
        }

        private class TestNodeVersionProvider : INodeVersionProvider
        {
            public PlatformVersionInfo GetVersionInfo()
            {
                throw new System.NotImplementedException();
            }
        }

        private class TestStandardOutputWriter : IStandardOutputWriter
        {
            public void Write(string message)
            {
            }

            public void WriteLine(string message)
            {
            }
        }
    }
}
