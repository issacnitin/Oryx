﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.Exceptions;
using Microsoft.Oryx.BuildScriptGenerator.Resources;
using Microsoft.Oryx.Tests.Common;
using Xunit;

namespace Microsoft.Oryx.BuildScriptGenerator.Tests
{
    public class DefaultBuildScriptGeneratorTest : IClassFixture<TestTempDirTestFixture>
    {
        private const string TestPlatformName = "test";

        private readonly string _tempDirRoot;

        public DefaultBuildScriptGeneratorTest(TestTempDirTestFixture testFixure)
        {
            _tempDirRoot = testFixure.RootDirPath;
        }

        [Fact]
        public void TryGenerateScript_ReturnsTrue_IfNoLanguageIsProvided_AndCanDetectLanguage()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            generator.GenerateBashScript(context, out var generatedScript);

            // Assert
            Assert.Contains("script-content", generatedScript);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_OnlyProcessProvidedPlatform_IfMultiPlatformIsDisabled()
        {
            // Arrange
            var detector1 = new TestLanguageDetectorSimpleMatch(shouldMatch: true);
            var platform1 = new TestProgrammingPlatform(
                "main",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector: detector1);
            var detector2 = new TestLanguageDetectorSimpleMatch(shouldMatch: true);
            var platform2 = new TestProgrammingPlatform(
                "anotherPlatform",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "some code",
                detector: detector2);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "main",
                PlatformVersion = "1.0.0",
                EnableMultiPlatformBuild = false,
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform1, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            generator.GenerateBashScript(context, out var generatedScript);

            // Assert
            Assert.Contains("script-content", generatedScript);
            Assert.DoesNotContain("some code", generatedScript);
        }

        [Fact]
        public void TryGenerateScript_ReturnsTrue_IfLanguageIsProvidedButNoVersion_AndCanDetectVersion()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
                PlatformVersion = null, // version not provided by user
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            generator.GenerateBashScript(context, out var generatedScript);

            // Assert
            Assert.Contains("script-content", generatedScript);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_Throws_IfNoLanguageIsProvided_AndCannotDetectLanguage()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: null,
                detectedLanguageVersion: null);
            var platform = new TestProgrammingPlatform("test", new[] { "1.0.0" }, detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                EnableMultiPlatformBuild = true,
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal(Labels.UnableToDetectPlatformMessage, exception.Message);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_Throws_IfLanguageIsProvidedButNoVersion_AndCannotDetectVersion()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: null);
            var platform = new TestProgrammingPlatform("test", new[] { "1.0.0" }, detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedVersionException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal("Couldn't detect a version for the platform 'test' in the repo.", exception.Message);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_Throws_IfLanguageIsProvided_AndCannotDetectLanguage()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: null,
                detectedLanguageVersion: null);
            var platform = new TestProgrammingPlatform("test1", new[] { "1.0.0" }, detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test2",
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal("'test2' platform is not supported. Supported platforms are: test1", exception.Message);
        }

        [Fact]
        public void TryGenerateScript_Throws_IfLanguageIsProvidedButDisabled()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform("test", new[] { "1.0.0" }, detector: detector, enabled: false);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
                PlatformVersion = "1.0.0",
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
        }

        [Fact]
        public void TryGenerateScript_Throws_IfCanDetectLanguageVersion_AndLanguageVersionIsUnsupported()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "2.0.0"); // Unsupported version
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedVersionException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal(
                "Platform 'test' version '2.0.0' is unsupported. Supported versions: 1.0.0",
                exception.Message);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_Throws_IfSuppliedLanguageIsUnsupported()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "unsupported",
                PlatformVersion = "1.0.0",
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal(
                "'unsupported' platform is not supported. Supported platforms are: test",
                exception.Message);
            Assert.False(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_Throws_IfSuppliedLanguageVersionIsUnsupported()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
                PlatformVersion = "2.0.0", //unsupported version
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedVersionException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal(
                "Platform 'test' version '2.0.0' is unsupported. Supported versions: 1.0.0",
                exception.Message);
            Assert.False(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_ReturnsFalse_IfGeneratorTryGenerateScript_IsFalse()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: false,
                scriptContent: null,
                detector);
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateBashScript(context, out var generatedScript));
            Assert.Equal(Labels.UnableToDetectPlatformMessage, exception.Message);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_CallsDetector_IfMultiPlatformIsOff_AndNoLangProvided()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                EnableMultiPlatformBuild = false,
            };
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            generator.GenerateBashScript(context, out var generatedScript);
            Assert.True(detector.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_DoesntCallDetector_IfMultiPlatformIsOff_AndLangProvided()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);

            var detector2 = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test2",
                detectedLanguageVersion: "1.0.0");
            var platform2 = new TestProgrammingPlatform(
                "test2",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector2);

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
                PlatformVersion = "1.0.0",
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            generator.GenerateBashScript(context, out var generatedScript);
            Assert.False(detector.DetectInvoked);
            Assert.False(detector2.DetectInvoked);
        }

        [Fact]
        public void TryGenerateScript_CallsDetector_IfMultiPlatformIsOn_AndLangProvided()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test",
                detectedLanguageVersion: "1.0.0");
            var platform = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);

            var detector2 = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: "test2",
                detectedLanguageVersion: "1.0.0");
            var platform2 = new TestProgrammingPlatform(
                "test2",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector2);

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
                PlatformVersion = "1.0.0",
                EnableMultiPlatformBuild = true,
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act & Assert
            generator.GenerateBashScript(context, out var generatedScript);
            Assert.False(detector.DetectInvoked);
            Assert.True(detector2.DetectInvoked);
        }

        [Fact]
        public void GeneratesScript_UsingTheFirstPlatform_WhichCanGenerateScript()
        {
            // Arrange
            var detector = new TestLanguageDetectorUsingLangName(
                detectedLanguageName: null,
                detectedLanguageVersion: null);
            var platform1 = new TestProgrammingPlatform(
                "lang1",
                new[] { "1.0.0" },
                canGenerateScript: false,
                scriptContent: null,
                detector);
            var platform2 = new TestProgrammingPlatform(
                "lang2",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector);

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "lang2",
                PlatformVersion = "1.0.0",
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform1, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            generator.GenerateBashScript(context, out var generatedScript);

            // Assert
            Assert.Contains("script-content", generatedScript);
            Assert.False(detector.DetectInvoked);
        }

        [Fact]
        public void GeneratesScript_AddsSnippetsForMultiplePlatforms()
        {
            // Arrange
            var platform1 = new TestProgrammingPlatform(
                languageName: "lang1",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "ABCDEFG",
                detector: new TestLanguageDetectorSimpleMatch(
                    shouldMatch: true,
                    language: "lang1",
                    languageVersion: "1.0.0"));
            var platform2 = new TestProgrammingPlatform(
                languageName: "lang2",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "123456",
                detector: new TestLanguageDetectorSimpleMatch(
                    shouldMatch: true,
                    language: "lang2",
                    languageVersion: "1.0.0"));

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "lang1",
                PlatformVersion = "1.0.0",
                EnableMultiPlatformBuild = true,
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform1, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            generator.GenerateBashScript(context, out var generatedScript);

            // Assert
            Assert.Contains("ABCDEFG", generatedScript);
            Assert.Contains("123456", generatedScript);
        }

        [Fact]
        public void GeneratesScript_AddsSnippetsForOnePlatform_OtherIsDisabled()
        {
            // Arrange
            var platform1 = new TestProgrammingPlatform(
                "test",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "ABCDEFG",
                detector: new TestLanguageDetectorSimpleMatch(shouldMatch: true));
            var platform2 = new TestProgrammingPlatform(
                "test2",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "123456",
                detector: new TestLanguageDetectorSimpleMatch(shouldMatch: true),
                enabled: false);

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "test",
                PlatformVersion = "1.0.0",
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform1, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            generator.GenerateBashScript(context, out var generatedScript);

            // Assert
            Assert.Contains("ABCDEFG", generatedScript);
            Assert.DoesNotContain("123456", generatedScript);
        }

        [Fact]
        public void GetCompatiblePlatforms_ReturnsOnlyPlatforms_ParticipatingIn_MultiPlatformBuilds()
        {
            // Arrange
            var platform1 = new TestProgrammingPlatform(
                languageName: "lang1",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "ABCDEFG",
                detector: new TestLanguageDetectorSimpleMatch(
                    shouldMatch: true,
                    language: "lang1",
                    languageVersion: "1.0.0"));
            var platform2 = new TestProgrammingPlatform(
                languageName: "lang2",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "123456",
                detector: new TestLanguageDetectorSimpleMatch(
                    shouldMatch: true,
                    language: "lang2",
                    languageVersion: "1.0.0"),
                platformIsEnabledForMultiPlatformBuild: false); // This platform explicitly opts out

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = "lang1",
                PlatformVersion = "1.0.0",
                EnableMultiPlatformBuild = true,
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform1, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            var compatiblePlatforms = generator.GetCompatiblePlatforms(context);

            // Assert
            Assert.NotNull(compatiblePlatforms);
            Assert.Equal(2, compatiblePlatforms.Count);
        }

        [Fact]
        public void Checkers_AreAppliedCorrectly_WhenCheckersAreEnabled()
        {
            // Arrange
            var repoWarning = new CheckerMessage("some repo warning");
            IChecker[] checkers = { new TestChecker(() => new[] { repoWarning }) };

            var platformVersion = "1.0.0";
            var detector = new TestLanguageDetectorSimpleMatch(true, TestPlatformName, platformVersion);
            var platform = new TestProgrammingPlatform(
                TestPlatformName, new[] { platformVersion }, true, "script-content", detector);

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = TestPlatformName,
                PlatformVersion = platformVersion,
                EnableCheckers = true,
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform }, commonOptions, checkers);
            var context = CreateScriptGeneratorContext();

            var messages = new List<ICheckerMessage>();

            // Act
            // Return value of TryGenerateBashScript is irrelevant - messages should be added even if build fails
            generator.GenerateBashScript(context, out var generatedScript, messages);

            // Assert
            Assert.Single(messages);
            Assert.Equal(repoWarning, messages.First());
        }

        [Fact]
        public void Checkers_DontFailTheBuild_WhenTheyThrow()
        {
            // Arrange
            bool checkerRan = false;
            IChecker[] checkers = { new TestChecker(() =>
            {
                checkerRan = true;
                throw new Exception("checker failed");
            }) };

            var platformVersion = "1.0.0";
            var detector = new TestLanguageDetectorSimpleMatch(true, TestPlatformName, platformVersion);
            var scriptContent = "script-content";
            var platform = new TestProgrammingPlatform(
                TestPlatformName, new[] { platformVersion }, true, scriptContent, detector);

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = TestPlatformName,
                PlatformVersion = platformVersion,
                EnableCheckers = true,
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform }, commonOptions, checkers);
            var context = CreateScriptGeneratorContext();

            var messages = new List<ICheckerMessage>();

            // Act
            generator.GenerateBashScript(context, out var generatedScript, messages);

            // Assert
            Assert.True(checkerRan);
        }

        [Fact]
        public void GetRequiredToolVersions_ReturnsPlatformTools()
        {
            // Arrange
            var platName = "test";
            var platVer = "1.0.0";
            var detector = new TestLanguageDetectorUsingLangName(platName, platVer);
            var platform = new TestProgrammingPlatform(
                platName,
                new[] { platVer },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultScriptGenerator(platform, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            var result = generator.GetRequiredToolVersions(context);

            // Assert
            Assert.Equal(platName, result.First().Key);
            Assert.Equal(platVer, result.First().Value);
        }

        [Fact]
        public void GetRequiredToolVersions_ReturnsOnlyFirstPlatformTools_IfMultiPlatformIsDisabled()
        {
            // Arrange
            var mainPlatformName = "main";
            var platform1 = new TestProgrammingPlatform(
                mainPlatformName,
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "script-content",
                detector: new TestLanguageDetectorSimpleMatch(shouldMatch: true));
            var platform2 = new TestProgrammingPlatform(
                "anotherPlatform",
                new[] { "1.0.0" },
                canGenerateScript: true,
                scriptContent: "some code",
                detector: new TestLanguageDetectorSimpleMatch(shouldMatch: true));

            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = mainPlatformName,
                PlatformVersion = "1.0.0",
            };
            var generator = CreateDefaultScriptGenerator(new[] { platform1, platform2 }, commonOptions);
            var context = CreateScriptGeneratorContext();

            // Act
            var result = generator.GetRequiredToolVersions(context);

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(mainPlatformName, result.First().Key);
        }

        private DefaultBuildScriptGenerator CreateDefaultScriptGenerator(
            IProgrammingPlatform platform,
            BuildScriptGeneratorOptions commonOptions)
        {
            return CreateDefaultScriptGenerator(new[] { platform }, commonOptions, checkers: null);
        }

        private DefaultBuildScriptGenerator CreateDefaultScriptGenerator(
            IProgrammingPlatform[] platforms,
            BuildScriptGeneratorOptions commonOptions,
            IEnumerable<IChecker> checkers = null)
        {
            commonOptions = commonOptions ?? new BuildScriptGeneratorOptions();
            commonOptions.SourceDir = "/app";
            commonOptions.DestinationDir = "/output";

            var configuration = new TestConfiguration();
            configuration[$"{commonOptions.PlatformName}_version"] = commonOptions.PlatformVersion;
            return new DefaultBuildScriptGenerator(
                Options.Create(commonOptions),
                new DefaultCompatiblePlatformDetector(
                    platforms,
                    NullLogger<DefaultCompatiblePlatformDetector>.Instance,
                    Options.Create(commonOptions),
                    configuration),
                checkers,
                NullLogger<DefaultBuildScriptGenerator>.Instance,
                new TestEnvironment(),
                new DefaultStandardOutputWriter());
        }

        private static BuildScriptGeneratorContext CreateScriptGeneratorContext()
        {
            return new BuildScriptGeneratorContext
            {
                SourceRepo = new TestSourceRepo(),
            };
        }

        [Checker(TestPlatformName)]
        private class TestChecker : IChecker
        {
            private readonly Func<IEnumerable<ICheckerMessage>> _sourceRepoMessageProvider;
            private readonly Func<IEnumerable<ICheckerMessage>> _toolVersionMessageProvider;

            public TestChecker(
                Func<IEnumerable<ICheckerMessage>> repoMessageProvider = null,
                Func<IEnumerable<ICheckerMessage>> toolMessageProvider = null)
            {
                _sourceRepoMessageProvider = repoMessageProvider ?? (() => Enumerable.Empty<ICheckerMessage>());
                _toolVersionMessageProvider = toolMessageProvider ?? (() => Enumerable.Empty<ICheckerMessage>());
            }

            public IEnumerable<ICheckerMessage> CheckSourceRepo(ISourceRepo repo) =>
                _sourceRepoMessageProvider();

            public IEnumerable<ICheckerMessage> CheckToolVersions(IDictionary<string, string> tools) =>
                _toolVersionMessageProvider();
        }

        private class TestSourceRepo : ISourceRepo
        {
            public string RootPath => string.Empty;

            public bool FileExists(params string[] paths)
            {
                throw new NotImplementedException();
            }

            public bool DirExists(params string[] paths)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFiles(string searchPattern, bool searchSubDirectories)
            {
                throw new NotImplementedException();
            }

            public string ReadFile(params string[] paths)
            {
                throw new NotImplementedException();
            }

            public string[] ReadAllLines(params string[] paths)
            {
                throw new NotImplementedException();
            }

            public string GetGitCommitId() => null;
        }
    }
}