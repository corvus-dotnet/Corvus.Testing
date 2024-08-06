// <copyright file="RootService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.ReqnRoll.Specs.Containers
{
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// A fake service used to test DI.
    /// </summary>
    public class RootService
    {
        public RootService(CultureInfo cultureInfo, IComparer<string> comparer)
        {
            this.CultureInfo = cultureInfo;
            this.Comparer = comparer;
        }

        public CultureInfo CultureInfo { get; }

        public IComparer<string> Comparer { get; }
    }
}