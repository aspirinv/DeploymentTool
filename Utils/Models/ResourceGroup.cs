using Newtonsoft.Json;

namespace incadea.WsCrm.DeploymentTool.Utils.Models
{
    /// <summary>
    /// azure resource group
    /// </summary>
    public class ResourceGroup
    {
        /// <summary>
        /// id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// location
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
