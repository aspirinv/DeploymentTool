using System;
using Microsoft.Xrm.Sdk;

namespace incadea.WsCrm.DeploymentTool.Utils.Models
{
    /// <summary>
    /// Solution representation class
    /// </summary>
    public class CrmSolutionModel
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="solution">entity with solution data</param>
        public CrmSolutionModel(Entity solution)
        {
            Name = solution.GetAttributeValue<string>("uniquename");
            Version = solution.GetAttributeValue<string>("version");
            Id = solution.Id;
        }

        /// <summary>
        /// solution id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// solution version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// solution unique name
        /// </summary>
        public string Name { get; set; }
    }
}
