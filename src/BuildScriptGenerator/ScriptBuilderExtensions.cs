﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using Microsoft.Oryx.Common;

namespace Microsoft.Oryx.BuildScriptGenerator
{
    internal static class ScriptBuilderExtensions
    {
        public static StringBuilder AppendSourceDirectoryInfo(this StringBuilder stringBuilder, string sourceDir)
        {
            stringBuilder
                .AppendFormatWithLine("echo Source directory     : {0}", sourceDir)
                .AppendLine();
            return stringBuilder;
        }

        public static StringBuilder AppendDestinationDirectoryInfo(
            this StringBuilder stringBuilder,
            string destinationDir)
        {
            stringBuilder
                .AppendFormatWithLine("echo Destination directory : {0}", destinationDir)
                .AppendLine();
            return stringBuilder;
        }

        public static StringBuilder AppendBenvCommand(this StringBuilder stringBuilder, string benvArgs)
        {
            var benvPath = FilePaths.Benv;
            if (File.Exists(benvPath))
            {
                stringBuilder
                    .AppendFormatWithLine("source {0} {1}", benvPath, benvArgs)
                    .AppendLine();
            }

            return stringBuilder;
        }

        public static StringBuilder AppendFormatWithLine(
            this StringBuilder stringBuilder,
            string format,
            object arg)
        {
            return stringBuilder.AppendFormatWithLine(format, new[] { arg });
        }

        public static StringBuilder AppendFormatWithLine(
            this StringBuilder stringBuilder,
            string format,
            object arg0,
            object arg1)
        {
            return stringBuilder.AppendFormatWithLine(format, new[] { arg0, arg1 });
        }

        public static StringBuilder AppendFormatWithLine(
            this StringBuilder stringBuilder,
            string format,
            object arg0,
            object arg1,
            object arg2)
        {
            return stringBuilder.AppendFormatWithLine(format, new[] { arg0, arg1, arg2 });
        }

        public static StringBuilder AppendFormatWithLine(
            this StringBuilder stringBuilder,
            string format,
            params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
            return stringBuilder;
        }
    }
}
