// <copyright file="FunctionOutputBufferHandler.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Testing.AzureFunctions.Internal
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Monitors a function app host process's standard output for the message indicating that
    /// it is ready.
    /// </summary>
    /// <remarks>
    /// In addition to reporting when the host is ready, this also reads from the output stream
    /// continuously, ensuring that the 2KB output buffer does not fill up. If we don't do this,
    /// the process will grind to a halt when it fills that buffer.
    /// </remarks>
    public class FunctionOutputBufferHandler : ProcessOutputHandler
    {
        /// <summary>
        /// Competion source for <see cref="JobHostStarted"/>.
        /// </summary>
        private readonly TaskCompletionSource<object> jobHostStartedCompletionSource = new TaskCompletionSource<object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionOutputBufferHandler"/> class, launching
        /// the process with the specified information.
        /// </summary>
        /// <param name="startInfo">
        /// Start information for launching the function process.
        /// </param>
        public FunctionOutputBufferHandler(ProcessStartInfo startInfo)
            : base(startInfo)
        {
            this.jobHostStartedCompletionSource = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Gets a Task that will complete if the process output indicates that the job host has
        /// successfully started.
        /// </summary>
        /// <remarks>
        /// If the job host does not start successfully, this task will never complete. Code using
        /// this class should also monitor the base <see cref="ProcessOutputHandler.ExitCode"/> task
        /// to detect an unexpected process exit.
        /// </remarks>
        public Task JobHostStarted => this.jobHostStartedCompletionSource.Task;

        /// <inheritdoc />
        protected override void OnStandardOutputLine(string line)
        {
            // The functions host emits this the line before listing the function endpoints.
            // It is a pretty safe bet that the service is ready once this appears.
            const string outputIndicatingHostIsReady = "Functions:";
            if (!this.jobHostStartedCompletionSource.Task.IsCompleted
                && (line?.Contains(outputIndicatingHostIsReady) == true))
            {
                this.jobHostStartedCompletionSource.SetResult(true);
            }
        }
    }
}