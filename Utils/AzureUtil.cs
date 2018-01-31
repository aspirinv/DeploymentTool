using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using incadea.WsCrm.DeploymentTool.Utils.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace incadea.WsCrm.DeploymentTool.Utils
{
    /// <summary>
    /// class for azure communication automation
    /// </summary>
    public class AzureUtil
    {
        private const string AzureHost = "https://management.azure.com/";
        private const string TokenPrefix = "Bearer ";
        public AzureUtil()
        {
        }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="token">azure token</param>
        public AzureUtil(string token)
        {
            _token = token;
        }

        private string _token;

        /// <summary>
        /// Token
        /// </summary>
        public string Token {
            get
            {
                if (string.IsNullOrWhiteSpace(_token))
                {
                    throw new Exception("Token is not gathered, call login operation first");
                }
                return _token;
            }
        }

        /// <summary>
        /// Execute GET request
        /// </summary>
        /// <typeparam name="T">type of result</typeparam>
        /// <param name="uri">uri to GET</param>
        /// <returns>json deserialized response</returns>
        public T Get<T>(string uri) where T: class
        {
            var obj = JObject.Parse(Get(uri));
            return obj["value"]?.ToObject<T>() ?? obj.ToObject<T>();
        }

        private string Get(string uri)
        {
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, TokenPrefix + Token);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";

            try
            {
                var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException webException)
            {
                var stream = webException.Response?.GetResponseStream();
                if (stream == null)
                {
                    throw;
                }
                throw new Exception(new StreamReader(stream).ReadToEnd());
            }
        }

        private const string Version = "api-version=2017-08-01";

        /// <summary>
        /// Authenticates into azure
        /// </summary>
        /// <param name="tenant">current tenant e.g. alexanderspirinrelsys for url alexanderspirinrelsys.onmicrosoft.com</param>
        /// <param name="clientId">application id, by default used PowerShell id</param>
        /// <param name="clientRedirectUri">redirect Url, by default used PowerShell redirectUrl</param>
        /// <returns>authentication token</returns>
        public async Task GetToken(string tenant = "common", string clientId = "1950a258-227b-4e31-a9cf-717495945fc2",
            string clientRedirectUri = "urn:ietf:wg:oauth:2.0:oob")
        {
            tenant = tenant + ".onmicrosoft.com";
            var context = new AuthenticationContext($"https://login.windows.net/{tenant}",
                true, TokenCache.DefaultShared);

            var result = await context.AcquireTokenAsync("https://management.core.windows.net/", clientId,
                new Uri(clientRedirectUri), new PlatformParameters(PromptBehavior.SelectAccount));

            _token = result.AccessToken;
        }

        /// <summary>
        /// creates resource group in azure
        /// </summary>
        /// <param name="group">group data</param>
        /// <param name="subscription">currrent subscription</param>
        public void CreateResourceGroup(ResourceGroup group, string subscription)
        {
            var uri = $"{AzureHost}subscriptions/{subscription}/resourcegroups/{group.Name}/?api-version=2015-01-01";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, TokenPrefix + Token);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "PUT";

            try
            {
                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(group));
                using (var stream = httpWebRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    group.Id = JsonConvert.DeserializeObject<ResourceGroup>(streamReader.ReadToEnd()).Id;
                }
            }
            catch (WebException webException)
            {
                var stream = webException.Response?.GetResponseStream();
                if (stream == null)
                {
                    throw;
                }
                throw new Exception(new StreamReader(stream).ReadToEnd());
            }

        }

        /// <summary>
        /// returns list of user available subscriptions
        /// </summary>
        /// <returns>list of user available subscriptions</returns>
        public IEnumerable<Subscription> GetSubscriptions()
        {
            var uri = $"{AzureHost}subscriptions/?{Version}";
            return Get<Subscription[]>(uri);
        }

        /// <summary>
        /// lists resource groups available under subscription
        /// </summary>
        /// <param name="subscriptionId">current subscription id</param>
        /// <returns>resource groups available under subscription</returns>
        public IEnumerable<ResourceGroup> GetResourceGroups(string subscriptionId)
        {
            var uri = $"{AzureHost}subscriptions/{subscriptionId}/resourcegroups/?{Version}";
            return Get<ResourceGroup[]>(uri);
        }

        /// <summary>
        /// lists locations available under subscription
        /// </summary>
        /// <param name="subscriptionId">current subscription id</param>
        /// <returns>locations available under subscription</returns>
        public IEnumerable<Location> GetLocations(string subscriptionId)
        {
            var capabilities = Get<JObject[]>(
                $"{AzureHost}/subscriptions/{subscriptionId}/providers/Microsoft.ClassicCompute/capabilities?api-version=2016-11-01")
                .Select(item=>item.Value<string>("location")).ToList();
            var uri = $"{AzureHost}subscriptions/{subscriptionId}/locations/?{Version}";
            var locations = Get<Location[]>(uri);

            return locations.Where(location=>capabilities.Contains(location.Name));
        }

        /// <summary>
        /// lists storage accounts available under subscription
        /// </summary>
        /// <param name="subscriptionId">current subscription id</param>
        /// <returns>storage accounts available under subscription</returns>
        public IEnumerable<string> GetStorages(string subscriptionId)
        {
            return Get<JObject[]>($"{AzureHost}/subscriptions/{subscriptionId}/providers/Microsoft.Storage/storageAccounts?api-version=2017-06-01")
                .Select(item => item.Value<string>("name")).ToList();
        }
    }
}
