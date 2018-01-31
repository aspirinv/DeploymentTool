using System.IO;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// Used constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// path to data folder
        /// </summary>
        public const string Setups = "Setups";
        /// <summary>
        /// path to CRM configurations
        /// </summary>
        public const string ConfigurationsPath = "Setups\\Configurations";
        /// <summary>
        /// path to test data
        /// </summary>
        public const string TestDataPath = "Setups\\TestData";

        /// <summary>
        /// temp folder of setup
        /// </summary>
        public const string SetupsTemp = "Setups\\Temp";
        /// <summary>
        /// unpack temp folder
        /// </summary>
        public const string RootPackTemp = "Setups\\Temp\\Service";
        /// <summary>
        /// unpack content temp folder
        /// </summary>
        public const string ContentPackTemp = "Setups\\Temp\\Content";

        /// <summary>
        /// CRM base role
        /// </summary>
        public const string BaseRole = "f0d460e0-f191-e611-80d9-005056af5bce";
        /// <summary>
        /// CRM call center role
        /// </summary>
        public const string CallCenterRole = "656ce974-9197-e611-80d9-005056af5bce";

        /// <summary>
        /// CRM base avatar role
        /// </summary>
        public const string BaseAvatarRole = "4095dc47-830d-e711-80fd-5065f38a9a01";
        /// <summary>
        /// CRM call center avatar role
        /// </summary>
        public const string CallCenterAvatarRole = "3dcc8ac4-d412-e711-8100-5065f38b06f1";

        /// <summary>
        /// name of the service package file
        /// </summary>
        public const string ServicePackageName = "service.cspkg";

        /// <summary>
        /// filter for profile files
        /// </summary>
        public const string ProfileFilter = "Profile files (*.crmdp)|*.crmdp";

        /// <summary>
        /// returns pack path
        /// </summary>
        /// <returns></returns>
        public static string GetPackPath()
        {
            return Path.Combine(Setups, ServicePackageName);
        }
    }
}
