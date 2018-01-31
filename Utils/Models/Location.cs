using Newtonsoft.Json;

namespace incadea.WsCrm.DeploymentTool.Utils.Models
{
    /// <summary>
    /// Azure location
    /// </summary>
    public class Location
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
        /// display Name
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
