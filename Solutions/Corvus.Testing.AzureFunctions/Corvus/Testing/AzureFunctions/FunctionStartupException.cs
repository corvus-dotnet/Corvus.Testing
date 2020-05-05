// <copyright file="FunctionStartupException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;

    /// <summary>
    /// Represents a failure in starting a process.
    /// </summary>
    public class FunctionStartupException : Exception
    {
        /// <summary>
        /// Instantiates a new FunctionStartupException with the provided message,
        /// text from stdout, and text from stderr.
        /// </summary>
        /// <param name="message">The exception message, describing what went wrong.</param>
        /// <param name="stdout">The text logged by the process to standard output.</param>
        /// <param name="stderr">The text logged by the process to standard error.</param>
        public FunctionStartupException(string message, string stdout = "", string stderr = "")
            : base(message)
        {
            this.Stdout = stdout;
            this.Stderr = stderr;
        }

        /// <summary>
        /// Gets the text logged by the process to standard output.
        /// </summary>
        public string Stdout { get; }

        /// <summary>
        /// Gets the text logged by the process to standard error.
        /// </summary>
        public string Stderr { get; }
    }
}