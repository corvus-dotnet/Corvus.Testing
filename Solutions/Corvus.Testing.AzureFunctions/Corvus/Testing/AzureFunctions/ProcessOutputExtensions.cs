// <copyright file="ProcessOutputExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods for the <see cref="IProcessOutput"/> interface.
    /// </summary>
    public static class ProcessOutputExtensions
    {
        /// <summary>
        /// Writes the process StdOut and StdErr to the console, then clears both output buffers.
        /// </summary>
        /// <param name="output">The <see cref="IProcessOutput"/> to write.</param>
        public static void WriteToConsoleAndClear(this IProcessOutput output)
        {
            string name =
                $"{output.ProcessStartInfo.FileName} {output.ProcessStartInfo.Arguments}, working directory {output.ProcessStartInfo.WorkingDirectory}";

            Console.WriteLine($"\nStdOut for process {name}:");
            Console.WriteLine(output.StandardOutputText);
            Console.WriteLine();

            string stdErr = output.StandardErrorText;

            if (!string.IsNullOrEmpty(stdErr))
            {
                Console.WriteLine($"\nStdErr for process {name}:");
                Console.WriteLine(stdErr);
                Console.WriteLine();
            }

            output.ClearAllOutput();
        }

        /// <summary>
        /// Writes the process StdOut and StdErr to the console, then clears both output buffers for each supplied
        /// <see cref="IProcessOutput"/>.
        /// </summary>
        /// <param name="outputs">The list of <see cref="IProcessOutput"/> to write.</param>
        public static void WriteAllToConsoleAndClear(this IEnumerable<IProcessOutput> outputs)
        {
            foreach (IProcessOutput output in outputs)
            {
                output.WriteToConsoleAndClear();
            }
        }
    }
}
