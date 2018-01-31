namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// data for settings
    /// </summary>
    public class SettingsData
    {
        /// <summary>
        /// proxy server url
        /// </summary>
        public string ProxyServer { get; set; }

        /// <summary>
        /// login for proxy
        /// </summary>
        public string ProxyLogin { get; set; }

        /// <summary>
        /// login for proxy
        /// </summary>
        public string ProxyPassword { get; set; }

        /// <summary>
        /// domain of proxy server
        /// </summary>
        public string ProxyDomain { get; set; }
    }
}
