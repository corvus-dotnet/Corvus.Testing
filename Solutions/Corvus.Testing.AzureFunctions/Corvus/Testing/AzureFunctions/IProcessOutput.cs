// <copyright file="IProcessOutput.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System.Diagnostics;

    /// <summary>
    /// Provides access to the output for a process.
    /// </summary>
    public interface IProcessOutput
    {
        /// <summary>
        /// Gets the <see cref="ProcessStartInfo"/> for the process being monitored.
        /// </summary>
        ProcessStartInfo ProcessStartInfo { get; }

        /// <summary>
        ///     Gets the standard output produced so far by this process.
        /// </summary>
        string StandardOutputText { get; }

        /// <summary>
        ///     Gets the standard error output produced so far by this process.
        /// </summary>
        string StandardErrorText { get; }

        /// <summary>
        ///     Clears the output and error buffers.
        /// </summary>
        void ClearAllOutput();
    }
}
