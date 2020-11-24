// <copyright file="FunctionProject.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.IO;

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
        /// <returns>A string containing the full path to the Azure Functions project runtime
        /// directory.</returns>
        public static string ResolvePath(string pathFragment, string runtime)
        {
            string currentDirectory = Environment.CurrentDirectory.ToLowerInvariant();

            string directoryExtension = @$"bin\release\{runtime}";
            if (currentDirectory.Contains("debug"))
            {
                directoryExtension = @$"bin\debug\{runtime}";
            }

            Console.WriteLine($"\tCurrent directory: {currentDirectory}");

            var candidate = new DirectoryInfo(currentDirectory);
            bool candidateIsSuccessful = false;

            while (!candidateIsSuccessful && candidate.Parent != null)
            {
                // We can skip the current directory and go straight to its parent, as it will
                // never be the directory we want.
                candidate = candidate.Parent;

                string pathToTest = Path.Combine(candidate.FullName, pathFragment, directoryExtension);
                candidateIsSuccessful = Directory.Exists(pathToTest);
            }

            string root = candidate.FullName;

            Console.WriteLine($"\tRoot: {root}");
            return Path.Combine(root, pathFragment, directoryExtension);
        }
    }
}