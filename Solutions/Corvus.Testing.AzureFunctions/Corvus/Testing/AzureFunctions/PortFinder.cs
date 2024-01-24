// <copyright file="PortFinder.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions
{
    using System;
    using System.Linq;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Enables tests to discover available ports.
    /// </summary>
    public static class PortFinder
    {
        /// <summary>
        /// Randomly selects a port that appears to be available for use.
        /// </summary>
        /// <param name="lowerBoundInclusive">
        /// The lowest port number acceptable. Defaults to 50000.
        /// </param>
        /// <param name="upperBoundExclusive">
        /// The port number above which no port will be selected. Defaults to 60000.
        /// </param>
        /// <returns>A port number that seems to be available.</returns>
        public static int FindAvailableTcpPort(int? lowerBoundInclusive, int? upperBoundExclusive)
        {
            int lb = lowerBoundInclusive ?? 50000;
            int ub = upperBoundExclusive ?? 60000;

            var portsInRangeInUse = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(e => e.Port)
                .Where(p => p >= lb && p < ub)
                .ToHashSet();

            int availablePorts = ub - lb - portsInRangeInUse.Count;
            int availablePortOffset = Random.Shared.Next(availablePorts);
            int port = Enumerable.Range(lb, ub - lb).Where(p => !portsInRangeInUse.Contains(p)).ElementAt(availablePortOffset);
            return port;
        }

        /// <summary>
        /// Discovers whether something on the computer is already listening for incoming
        /// requests on a particular port.
        /// </summary>
        /// <param name="port">The port number to check.</param>
        /// <returns>True if the port is currently in use.</returns>
        public static bool IsSomethingAlreadyListeningOn(int port)
        {
            return IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(e => e.Port == port);
        }
    }
}
