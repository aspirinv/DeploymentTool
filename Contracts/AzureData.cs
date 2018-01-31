
using System.Runtime.Serialization;
using incadea.WsCrm.DeploymentTool.Utils.Models;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// stores azure data
    /// </summary>
    public class AzureData
    {
        /// <summary>
        /// Authentication token
        /// </summary>
        [IgnoreDataMember]
        public string Token { get; set; }

        /// <summary>
        /// User group name
        /// </summary>
        public ResourceGroup Group { get; set; }

        /// <summary>
        /// selected location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// used subscription
        /// </summary>
        public string Subscription { get; set; }

        /// <summary>
        /// tenant name
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// selected storage account
        /// </summary>
        public string StorageAccount { get; set; }
    }
}
