using System.Configuration;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// Configuration root
    /// </summary>
    public class SolutionsConfigurationCollection : ConfigurationElementCollection
    {
        #region Overrides of ConfigurationElementCollection

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A newly created <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new SolutionConfigurationElement();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        /// <param name="element">The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for. </param>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return element;
        }

        #endregion
    }
}
