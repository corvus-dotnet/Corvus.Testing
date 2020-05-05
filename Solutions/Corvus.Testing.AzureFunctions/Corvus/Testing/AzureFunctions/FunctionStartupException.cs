// <copyright file="FunctionStartupException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;

    /// <summary>
    /// Represents a failure when starting the Azure Function. This can be thrown at any point in the
    /// function host bootstrap sequence, and so the detail of the exception may relate to supporting
    /// tools such as, e.g., NPM. The <see cref="Stdout"/> and <see cref="Stderr"/> properties will be
    /// populated from the failed process as and when possible.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Given that processes can fail to start before the IO streams have been redirected, or have
    /// accepted any data, neither property can be *guaranteed* to have a value.
    /// </para>
    /// </remarks>
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
        /// Gets the text logged by the process to standard output, if any.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Given that processes can fail to start before the IO streams have been redirected, or
        /// have accepted any data, this cannot be *guaranteed* to have a value. It is recommended
        /// to provide clear indication of the specific value of this property when printing it
        /// when debugging or for other troubleshooting, such as wrapping it in marker characters
        /// or strings.
        /// </para>
        /// </remarks>
        public string Stdout { get; }

        /// <summary>
        /// Gets the text logged by the process to standard error, if any.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Given that processes can fail to start before the IO streams have been redirected, or
        /// have accepted any data, this cannot be *guaranteed* to have a value. It is recommended
        /// to provide clear indication of the specific value of this property when printing it
        /// when debugging or for other troubleshooting, such as wrapping it in marker characters
        /// or strings.
        /// </para>
        /// </remarks>
        public string Stderr { get; }
    }
}