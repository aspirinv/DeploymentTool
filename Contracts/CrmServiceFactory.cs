using System;
using CsvLoader.Core.Contracts;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// basic factory to perform CRM operations
    /// </summary>
    public class CrmServiceFactory : CrmConnectionInfo
    {
        private const string ServicePath = "/XRMServices/2011/organization.svc";

        /// <summary>
        /// service url
        /// </summary>
        public string Url { get; set; }

        #region Overrides of CrmConnectionInfo

        /// <summary>
        /// uri to crm
        /// </summary>
        public override Uri OrgUri => new Uri(Url + ServicePath);

        #endregion
        
    }
}
