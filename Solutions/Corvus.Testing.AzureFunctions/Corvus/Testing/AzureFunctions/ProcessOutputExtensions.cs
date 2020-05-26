// <copyright file="ProcessOutputExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Extension methods for the <see cref="IProcessOutput"/> interface.
    /// </summary>
    public static class ProcessOutputExtensions
    {
        /// <summary>
        /// Writes the process StdOut and StdErr to the destination, then clears both output buffers.
        /// </summary>
        /// <param name="output">The <see cref="IProcessOutput"/> to write.</param>
        public static void WriteToConsoleAndClear(this IProcessOutput output)
        {
            output.WriteAndClear(Console.Out);
        }

        /// <summary>
        /// Writes the process StdOut and StdErr to the destination, then clears both output buffers.
        /// </summary>
        /// <param name="output">The <see cref="IProcessOutput"/> to write.</param>
        /// <param name="destination">The destination <see cref="TextWriter"/>, e.g. <see cref="Console.Out"/>.</param>
        public static void WriteAndClear(this IProcessOutput output, TextWriter destination)
        {
            string name =
                $"{output.ProcessStartInfo.FileName} {output.ProcessStartInfo.Arguments}, working directory {output.ProcessStartInfo.WorkingDirectory}";

            destination.WriteLine($"\nStdOut for process {name}:");
            destination.WriteLine(output.StandardOutputText);
            destination.WriteLine();

            string stdErr = output.StandardErrorText;
            if (!string.IsNullOrEmpty(stdErr))
            {
                destination.WriteLine($"\nStdErr for process {name}:");
                destination.WriteLine(output.StandardErrorText);
                destination.WriteLine();
            }

            output.ClearAllOutput();
        }

        /// <summary>
        /// Writes the process StdOut and StdErr to the destination, then clears both output buffers for each supplied
        /// <see cref="IProcessOutput"/>.
        /// </summary>
        /// <param name="outputs">The list of <see cref="IProcessOutput"/> to write.</param>
        public static void WriteAllToConsoleAndClear(this IEnumerable<IProcessOutput> outputs)
        {
            outputs.WriteAllAndClear(Console.Out);
        }

        /// <summary>
        /// Writes the process StdOut and StdErr to the destination, then clears both output buffers for each supplied
        /// <see cref="IProcessOutput"/>.
        /// </summary>
        /// <param name="outputs">The list of <see cref="IProcessOutput"/> to write.</param>
        /// <param name="destination">The destination <see cref="TextWriter"/>, e.g. <see cref="Console.Out"/>.</param>
        public static void WriteAllAndClear(this IEnumerable<IProcessOutput> outputs, TextWriter destination)
        {
            foreach (IProcessOutput output in outputs)
            {
                output.WriteAndClear(destination);
            }
        }

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
