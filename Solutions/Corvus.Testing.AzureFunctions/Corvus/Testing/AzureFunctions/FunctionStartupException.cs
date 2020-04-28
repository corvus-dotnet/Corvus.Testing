// <copyright file="FunctionStartupException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;

    /// <inheritdoc />
    public class FunctionStartupException : Exception
    {
        /// <summary>
        /// Instantiates a new FunctionStartupException with the provided message.
        /// </summary>
        /// <param name="message">The exception message, describing what went wrong.</param>
        public FunctionStartupException(string message)
            : base(message)
        {
        }
    }
}