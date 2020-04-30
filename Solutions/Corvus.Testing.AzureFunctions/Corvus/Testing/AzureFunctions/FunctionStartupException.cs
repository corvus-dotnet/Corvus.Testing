// <copyright file="FunctionStartupException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;

    /// <inheritdoc />
    public class FunctionStartupException : Exception
    {
        /// <inheritdoc cref="Exception"/>
        public FunctionStartupException()
        {
        }

        /// <inheritdoc cref="Exception"/>
        public FunctionStartupException(string message)
            : base(message)
        {
        }

        /// <inheritdoc cref="Exception"/>
        public FunctionStartupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}