using Microsoft.Xrm.Sdk;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// view model for option set
    /// </summary>
    public class OptionSetViewModel
    {
        /// <summary>
        /// caption to be shown
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// optionset value
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// converts data to optionset
        /// </summary>
        /// <returns>new optionset value</returns>
        public OptionSetValue ToOptionSet() => new OptionSetValue(Value);

        #region Overrides of Object

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return Caption;
        }

        #endregion
    }
}
