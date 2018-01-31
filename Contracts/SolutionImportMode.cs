using System;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// solution import mode
    /// </summary>
    [Flags]
    public enum SolutionImportMode
    {
        /// <summary>
        /// in every deployment
        /// </summary>
        Full = 1, 

        /// <summary>
        /// only in data import
        /// </summary>
        DataImport = 2
    }
}
