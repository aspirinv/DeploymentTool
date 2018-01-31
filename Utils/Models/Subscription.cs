using Newtonsoft.Json;

namespace incadea.WsCrm.DeploymentTool.Utils.Models
{
    /// <summary>
    /// azure subscription
    /// </summary>
    public class Subscription
    {
        /// <summary>
        /// display Name
        /// </summary>
        [JsonProperty("displayName")]
        public string Name { get; set; }

        /// <summary>
        /// id
        /// </summary>
        [JsonProperty("subscriptionId")]
        public string Id { get; set; }
    }
}
