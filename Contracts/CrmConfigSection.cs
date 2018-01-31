using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// configuration section
    /// </summary>
    public class CrmConfigSection : ConfigurationSection
    {
        /// <summary>
        /// solutions information
        /// </summary>
        [ConfigurationProperty("Solutions")]
        [ConfigurationCollection(typeof(SolutionsConfigurationCollection), AddItemName = "Solution", ClearItemsName = "clear", RemoveItemName = "remove")]
        public SolutionsConfigurationCollection SolutionCollection
        {
            get { return (SolutionsConfigurationCollection) this["Solutions"]; }
            set { this["Solutions"] = value; }
        }

        /// <summary>
        /// Solutions as elements
        /// </summary>
        public IEnumerable<SolutionConfigurationElement> Solutions
            => SolutionCollection.Cast<SolutionConfigurationElement>();
    }
}
