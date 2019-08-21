// <copyright file="ProcessOutputHandler.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.SpecFlow.Extensions.Internal
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides simplified access to a process's text output.
    /// </summary>
    public class ProcessOutputHandler
    {
        /// <summary>
        ///     Provides the task for <see cref="ExitCode"/>, enabling users of this class to
        ///     discover when the process finishes.
        /// </summary>
        private readonly TaskCompletionSource<int> exitCodeCompletionSource = new TaskCompletionSource<int>();

        /// <summary>
        ///     The text the process has sent to standard output so far.
        /// </summary>
        private readonly StringBuilder standardOutput = new StringBuilder();

        /// <summary>
        ///     The text the process has sent to standard error so far.
        /// </summary>
        private readonly StringBuilder standardError = new StringBuilder();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProcessOutputHandler"/> class for the
        ///     specified process, starting the process with the specified start info.
        /// </summary>
        /// <param name="startInfo">
        ///     The start information with which to launch the process.
        /// </param>
        public ProcessOutputHandler(ProcessStartInfo startInfo)
        {
            this.Process = new Process { StartInfo = startInfo };

            this.Process.Exited += this.OnProcessExit;
            this.Process.OutputDataReceived += this.OnOutputDataReceived;
            this.Process.ErrorDataReceived += this.OnErrorDataReceived;
            this.Process.EnableRaisingEvents = true;

            this.Process.Start();

            this.Process.BeginOutputReadLine();
            this.Process.BeginErrorReadLine();
        }

        /// <summary>
        ///     Gets the process being monitored.
        /// </summary>
        public Process Process { get; }

        /// <summary>
        ///     Gets a task that produces the exit code of the process once it completes.
        /// </summary>
        public Task<int> ExitCode => this.exitCodeCompletionSource.Task;

        /// <summary>
        ///     Gets the standard output produced so far by this process.
        /// </summary>
        public string StandardOutputText
        {
            get
            {
                lock (this.standardOutput)
                {
                    return this.standardOutput.ToString();
                }
            }
        }

        /// <summary>
        ///     Gets the standard error output produced so far by this process.
        /// </summary>
        public string StandardErrorText
        {
            get
            {
                lock (this.standardError)
                {
                    return this.standardError.ToString();
                }
            }
        }

        /// <summary>
        /// Call after the process has exited to ensure that all asynchronous output processing
        /// is complete.
        /// </summary>
        public void EnsureComplete()
        {
            // This ensures that any asynchronous processing within Process completes. Without
            // this, we can end up losing the tail end of the process's output.
            this.Process.WaitForExit();

            this.Process.OutputDataReceived -= this.OnOutputDataReceived;
            this.Process.ErrorDataReceived -= this.OnErrorDataReceived;
        }

        /// <summary>
        ///     Invoked each time the process sends a line of text to its standard output.
        /// </summary>
        /// <param name="line">
        ///     The line of text the process sent to its standard output.
        /// </param>
        protected virtual void OnStandardOutputLine(string line)
        {
        }

        /// <summary>
        ///     Invoked each time the process sends a line of text to its standard error.
        /// </summary>
        /// <param name="line">
        ///     The line of text the process sent to its standard error.
        /// </param>
        protected virtual void OnStandardErrorLine(string line)
        {
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (this.standardOutput)
            {
                this.standardOutput.AppendLine(e.Data);
            }

            this.OnStandardOutputLine(e.Data);
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string line = e.Data;
            if (line != null)
            {
                lock (this.standardError)
                {
                    this.standardError.AppendLine(line);
                }
            }

            this.OnStandardErrorLine(line);
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            this.exitCodeCompletionSource.SetResult(this.Process.ExitCode);
            this.Process.OutputDataReceived -= this.OnOutputDataReceived;
        }
    }
}