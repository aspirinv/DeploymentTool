using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using incadea.WsCrm.DeploymentTool.Contracts;

namespace incadea.WsCrm.DeploymentTool.Utils
{
    /// <summary>
    /// class to change 
    /// </summary>
    public class WebConfigChangeHelper
    {

        /// <summary>
        /// changes config file with wizard data
        /// </summary>
        /// <param name="configPath">path to web config</param>
        /// <param name="data">data to be set</param>
        public void UpdateConfig(string configPath, WizardContext data)
        {
            var doc = new XmlDocument();
            doc.Load(configPath);
            UpdateConfigXml(doc, data);
            doc.Save(configPath);
        }

        /// <summary>
        /// changes config file with wizard data
        /// </summary>
        /// <param name="configsStream">stream of web config</param>
        /// <param name="data">data to be set</param>
        public void UpdateConfig(Stream configsStream, WizardContext data)
        {
            var doc = new XmlDocument();
            doc.Load(configsStream);
            UpdateConfigXml(doc, data);
            configsStream.SetLength(0);
            doc.Save(configsStream);
        }

        private void UpdateConfigXml(XmlDocument doc, WizardContext data)
        {
            var configuration = doc.SelectSingleNode("/configuration");
            if (configuration == null)
            {
                throw new Exception("Invalid web.config content");
            }

            var unityNamespaceManager = new XmlNamespaceManager(doc.NameTable);
            unityNamespaceManager.AddNamespace("x", "http://schemas.microsoft.com/practices/2010/unity");

            var unityKeys = doc.SelectNodes(@"//x:unity/x:container/x:register", unityNamespaceManager)?.Cast<XmlNode>()
                .Where(node => node.Attributes != null)
                .ToDictionary(item => item.Attributes["type"].Value, item => item) ?? new Dictionary<string, XmlNode>();

            var appSettings = doc.SelectNodes("//appSettings/add")?.Cast<XmlNode>()
                .Where(node => node.Attributes != null) ?? new List<XmlNode>();
            foreach (var setting in appSettings)
            {
                var key = setting.Attributes["key"].Value.ToLower();
                if (data.AppSettings.ContainsKey(key))
                {
                    setting.Attributes["value"].Value = data.AppSettings[key];
                }
                else
                {
                    if (key == "crmservicepath")
                    {
                        setting.Attributes["value"].Value = data.CrmFactory.Url;
                    }
                }
            }

            if (data.IsAzureHosting)
            {
                unityKeys["incadea.WsCrm.Contracts.ILoggingProvider"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.Utils.AzureTracingService";
                unityKeys["incadea.WsCrm.Adapter.BusinessLogic.IAuthenticationManager"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.BusinessLogic.Common.OnlineAuthenticationManager";
                unityKeys["Microsoft.Xrm.Sdk.IOrganizationServiceFactory"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.Utils.OrganizationServiceFactory";
            }
            else
            {
                unityKeys["incadea.WsCrm.Contracts.ILoggingProvider"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.Utils.TracingService";
                unityKeys["incadea.WsCrm.Adapter.BusinessLogic.IAuthenticationManager"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.BusinessLogic.Common.OnpremiseAuthenticationManager";
                unityKeys["Microsoft.Xrm.Sdk.IOrganizationServiceFactory"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.Utils.OnPremiseServiceFactory";
            }

            if (!string.IsNullOrWhiteSpace(data.AppSettings["twilioaccount"]) && !string.IsNullOrWhiteSpace(data.AppSettings["twiliotoken"]))
            {
                unityKeys["incadea.WsCrm.Adapter.BusinessLogic.ISmsSender"].Attributes["mapTo"].Value =
                    "incadea.WsCrm.Adapter.BusinessLogic.Common.TwilioSmsSender";
            }
            if (!string.IsNullOrWhiteSpace(data.AppSettings["ftpserverpath"]))
            {
                unityKeys["incadea.WsCrm.Adapter.BusinessLogic.IDataUploader"].Attributes["mapTo"].Value =
                    data.IsSftp
                        ? "incadea.WsCrm.Adapter.BusinessLogic.Common.SftpDataUploader"
                        : "incadea.WsCrm.Adapter.BusinessLogic.Common.DataUploader";
            }
        }

        /// <summary>
        /// Reads app settings keys from the config file
        /// </summary>
        /// <param name="configFile">path to config file</param>
        /// <returns>list of app keys</returns>
        public IEnumerable<string> GetAppSettings(string configFile)
        {
            var doc = new XmlDocument();
            doc.Load(configFile);
            return doc.SelectNodes("//appSettings/add")?.Cast<XmlNode>()
                .Where(node => node.Attributes != null).Select(node=>node.Attributes["key"].Value.ToLower());
        }
    }
}
