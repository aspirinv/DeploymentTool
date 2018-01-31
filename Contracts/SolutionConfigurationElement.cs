using System.Configuration;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// Solution section element
    /// </summary>
    public class SolutionConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// solution name without version
        /// </summary>
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string) this["name"]; }
            set { this["name"] = value; }
        }

        /// <summary>
        /// import sequense position
        /// </summary>
        [ConfigurationProperty("sequencePosition")]
        public int SequencePosition
        {
            get { return (int)this["sequencePosition"]; }
            set { this["sequencePosition"] = value; }
        }

        /// <summary>
        /// when to import
        /// </summary>
        [ConfigurationProperty("mode")]
        public SolutionImportMode Mode
        {
            get { return (SolutionImportMode)this["mode"]; }
            set { this["mode"] = value; }
        }

    }
}
