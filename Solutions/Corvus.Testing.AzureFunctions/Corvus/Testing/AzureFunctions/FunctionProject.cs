// <copyright file="FunctionProject.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.IO;
    using System.Linq;
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
        ///         "Solutions/Corvus.Testing.AzureFunctions.DemoFunction.InProcess",
        ///         "net6.0");
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

            string currentDirectory = Environment.CurrentDirectory;

            string buildConfiguration = "release";
            if (currentDirectory.Contains("debug", StringComparison.InvariantCultureIgnoreCase))
            {
                buildConfiguration = "debug";
            }

            logger.LogDebug("Working directory is {WorkingDirectory}", currentDirectory);

            var candidate = new DirectoryInfo(currentDirectory);

            while (candidate.Parent != null)
            {
                logger.LogTrace("Current candidate root directory is {CandidateRootDirectory}", candidate);

                // We can skip the current directory and go straight to its parent, as it will
                // never be the directory we want.
                candidate = candidate.Parent;

                string? GetChildFolderCaseInsensitive(string parent, string child)
                {
                    return Directory.EnumerateDirectories(parent).FirstOrDefault(fullFolderPath => Path.GetFileName(fullFolderPath).Equals(child, StringComparison.InvariantCultureIgnoreCase));
                }

                // assume pathFragment is in the correct case
                string? projectFolder = Path.Combine(candidate.FullName, pathFragment);

                if (Directory.Exists(projectFolder))
                {
                    string? binFolder = GetChildFolderCaseInsensitive(projectFolder, "bin");
                    if (binFolder is not null)
                    {
                        string? configFolder = GetChildFolderCaseInsensitive(binFolder, buildConfiguration);
                        if (configFolder is not null)
                        {
                            string? targetFolder = GetChildFolderCaseInsensitive(configFolder, runtime);
                            if (targetFolder is not null)
                            {
                                logger.LogDebug("Function root directory is {RootDirectory}", targetFolder);
                                return targetFolder;
                            }
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Failed to find {pathFragment}");
        }
    }
}