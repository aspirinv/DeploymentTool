using System.Runtime.Serialization;
using Microsoft.Web.Administration;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// data for onpremise hosting
    /// </summary>
    public class OnPremiseData
    {
        /// <summary>
        /// path to the service
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// hosting port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Selected site
        /// </summary>
        [IgnoreDataMember]
        public Site Site { get; set; }

        /// <summary>
        /// name of new site
        /// </summary>
        [IgnoreDataMember]
        public string SiteName { get; set; }

        /// <summary>
        /// site name to be stored
        /// </summary>
        public string SiteSerializationValue
        {
            get { return Site == null ? SiteName : Site.Name; }
            set { SiteName = value; }
        }
    }
}