﻿// <copyright file="FunctionConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System.Collections.Generic;

    /// <summary>
    /// Function configuration to use when starting a new function process.
    /// </summary>
    public class FunctionConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionConfiguration"/> class.
        /// </summary>
        public FunctionConfiguration()
        {
            this.EnvironmentVariables = new();
        }

        /// <summary>
        /// Gets the environment variables to set on the function process.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; }
    }
}