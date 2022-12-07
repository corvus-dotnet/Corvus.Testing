// <copyright file="ProcessOutputExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System.Collections.Generic;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Extension methods for the <see cref="IProcessOutput"/> interface.
    /// </summary>
    public static class ProcessOutputExtensions
    {
        /// <summary>
        /// Logs the process StdOut and StdErr to the destination, then clears both output buffers for each supplied
        /// <see cref="IProcessOutput"/>.
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="outputs">The list of <see cref="IProcessOutput"/> to write.</param>
        public static void LogAllAndClear(this ILogger logger, IEnumerable<IProcessOutput> outputs)
        {
            foreach (IProcessOutput output in outputs)
            {
                logger.LogAndClear(output);
            }
        }

        /// <summary>
        /// Logs the process StdOut and StdErr to the destination, then clears both output buffers.
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="output">The <see cref="IProcessOutput"/> to write.</param>
        public static void LogAndClear(this ILogger logger, IProcessOutput output)
        {
            string name =
                $"{output.ProcessStartInfo.FileName} {output.ProcessStartInfo.Arguments}, working directory {output.ProcessStartInfo.WorkingDirectory}";

            logger.LogInformation("StdOut for process {Name}: {StdOut}", name, output.StandardOutputText);

            string stdErr = output.StandardErrorText;
            if (!string.IsNullOrEmpty(stdErr))
            {
                logger.LogWarning("StdErr for process {Name}: {StdErr}", name, stdErr);
            }

            output.ClearAllOutput();
        }
    }
}
