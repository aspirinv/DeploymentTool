using System;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// event args for progress sending
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// current progress
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="progress">current progress</param>
        public ProgressEventArgs(int progress)
        {
            Progress = progress;
        }
    }
}
