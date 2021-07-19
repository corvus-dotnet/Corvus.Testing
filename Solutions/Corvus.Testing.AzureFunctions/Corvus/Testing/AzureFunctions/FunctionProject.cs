// <copyright file="FunctionProject.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.IO;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Utility methods for working with Azure Functions projects.
    /// </summary>
    public sealed class FunctionProject
    {
        /// <summary>
        /// Resolves an Azure Functions project runtime directory from the given path fragment
        /// and specified runtime.
        /// </summary>
        /// <example>
        /// <![CDATA[
        /// public async Task ReadFunctionConfiguration()
        /// {
        ///     string functionProjectPath = FunctionProject.ResolvePath(
        ///         "Solutions/Corvus.Testing.AzureFunctions.DemoFunction",
        ///         "netcoreapp3.1");
        ///
        ///     string projectSettingsFile = Path.Combine(functionProjectPath, "local.settings.json");
        /// }
        /// ]]>
        /// </example>
        /// <param name="pathFragment">The path to the Azure Functions project, relative to some
        /// common root folder.</param>
        /// <param name="runtime">The runtime targeted by the Azure Functions project.</param>
        /// <param name="logger">An optional <see cref="ILogger" /> instance to receive output.</param>
        /// <returns>A string containing the full path to the Azure Functions project runtime
        /// directory.</returns>
        public static string ResolvePath(string pathFragment, string runtime, ILogger? logger = null)
        {
            logger ??= NullLogger.Instance;

            // Even though the dotnet Directory.Exists docs say that the path parameter is not
            // case-sensitive Linux OS file systems are case sensitive, and Directory.Exists seems
            // to reflect that. So to handle cases like "debug" and "Debug" correctly, we need to
            // perform case-insensitive string comparisons to work out which configuration is in
            // use, and then ensure that we preserve whatever casing happens to be present.
            // (We used to just force the whole path to lowercase, which was a whole lot simpler,
            // but that only works on Windows.)
            string currentDirectory = Environment.CurrentDirectory;
            string buildConfigurationFolder = string.Empty;
            int pos;
            if ((pos = currentDirectory.IndexOf("Debug", StringComparison.InvariantCultureIgnoreCase)) >= 0)
            {
                buildConfigurationFolder = currentDirectory.Substring(pos, "Debug".Length);
            }
            else if ((pos = currentDirectory.IndexOf("Release", StringComparison.InvariantCultureIgnoreCase)) >= 0)
            {
                buildConfigurationFolder = currentDirectory.Substring(pos, "Release".Length);
            }

            string directoryExtension = Path.Combine("bin", buildConfigurationFolder, runtime);
            logger.LogDebug("Working directory is {WorkingDirectory}", currentDirectory);
            logger.LogDebug("Working directory is {directoryExtension}", currentDirectory);

            var candidate = new DirectoryInfo(currentDirectory);
            bool candidateIsSuccessful = false;

            while (!candidateIsSuccessful && candidate.Parent != null)
            {
                logger.LogTrace("Current candidate root directory is {CandidateRootDirectory}", candidate);

                // We can skip the current directory and go straight to its parent, as it will
                // never be the directory we want.
                candidate = candidate.Parent;

                string pathToTest = Path.Combine(candidate.FullName, pathFragment, directoryExtension);
                candidateIsSuccessful = Directory.Exists(pathToTest);
                logger.LogTrace(
                    "Tested path {CandidateRootPath} with result: {CandidateRootPathExists}",
                    pathToTest,
                    candidateIsSuccessful ? "exists" : "not found");
            }

            string root = candidate.FullName;

            logger.LogDebug("Function root directory is {RootDirectory}", root);
            return Path.Combine(root, pathFragment, directoryExtension);
        }
    }
}