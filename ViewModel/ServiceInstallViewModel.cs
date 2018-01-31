using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Utils;
using incadea.WsCrm.DeploymentTool.Utils.Models;
using Microsoft.Azure;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    public class ServiceInstallViewModel : ProgressViewModel
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public ServiceInstallViewModel(WizardContext context) : base(context)
        {

        }

        #region Overrides of StepViewModel<Progress>

        /// <summary>
        /// step name
        /// </summary>
        public override string Name { get; } = "Service Deployment";

        #endregion

        #region Overrides of ProgressViewModel

        /// <summary>
        /// real work goes here
        /// </summary>
        protected override void RunInternal()
        {
            if (WizardContext.IsAzureHosting)
            {
                ProcessAzure();
            }
            else
            {
                ProcessLocal();
            }
            Directory.Delete(Constants.ContentPackTemp, true);
            Directory.Delete(Constants.RootPackTemp, true);
        }

        #endregion

        #region private stuff

        private const string DiagnosticsId = "DiagnosticsExtension";

        private void ProcessLocal()
        {
            var data = WizardContext.OnPremiseData;
            const string poolName = "incadeacrmadapterpool";
            var manager = new ServerManager();
            LogInfo("Copying files");
            var sourcePath = Path.Combine(Directory.GetCurrentDirectory(),
                Path.Combine(Constants.ContentPackTemp, "approot"));
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, data.Path));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, data.Path), true);
            }

            LogInfo("Processing site information");
            Progress = 70;
            var site = data.Site;
            if (data.Site == null)
            {
                var pool = manager.ApplicationPools.FirstOrDefault(item => item.Name == poolName);
                if (pool == null)
                {
                    LogInfo("Creating a new pool");
                    pool = manager.ApplicationPools.Add(poolName);
                    pool.ManagedRuntimeVersion = "v4.0";
                    pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                    Progress = 85;
                }
                LogInfo("Creating a new site");
                site = manager.Sites.Add(data.SiteName, data.Path, data.Port);
                site.ApplicationDefaults.ApplicationPoolName = pool.Name;
                manager.CommitChanges();
            }
            else
            {
                site.Stop();
                site.Start();
            }
            Progress = 90;
            LogInfo("Restarting site and pool");
            manager.ApplicationPools.First(item => item.Name == site.ApplicationDefaults.ApplicationPoolName).Recycle();

            var binding = site.Bindings.First();
            var portPostfix = binding.EndPoint.Port != 80 || binding.EndPoint.Port != 443
                ? $":{binding.EndPoint.Port}"
                : string.Empty;
            WizardContext.ServiceUrl = $"{binding.Protocol}://localhost{portPostfix}";
            Progress = 100;
        }

        private void ProcessAzure()
        {
            var data = WizardContext.AzureData;

            if (string.IsNullOrWhiteSpace(data.Group.Id))
            {
                LogInfo("Creating a new Resource Group");
                new AzureUtil(data.Token).CreateResourceGroup(data.Group, data.Subscription);
            }
            Progress = 15;
            LogInfo("Uploading package");
            var keys = UploadPackage(data);
            Progress = 50;
            var computeManagementClient = new ComputeManagementClient(new TokenCloudCredentials(data.Subscription, data.Token));
            
            LogInfo("Creating service");
            CreateHostedService(computeManagementClient, data.Group.Name, data.Location);
            Progress = 70;
            LogInfo("Package deployment started");
            CreateService(computeManagementClient, keys, data);

            WizardContext.ServiceUrl = $"http://{data.Group.Name}.cloudapp.net/";
            Progress = 100;
        }

        private AzureStorageData UploadPackage(AzureData data)
        {
            LogInfo("Defining storage account");
            var storage = new StorageManagementClient(new TokenCloudCredentials(data.Subscription, data.Token));
            if (storage.StorageAccounts.List().All(a => a.Name != data.StorageAccount))
            {
                CreateStorageAccount(data, storage);
            }
            Progress = 25;
            LogInfo("Storage account defined");
            var key = storage.StorageAccounts.ListKeys(data.Group.Name, data.StorageAccount).StorageAccountKeys.Key1;
            var blobClient = new CloudStorageAccount(new StorageCredentials(data.StorageAccount, key), true)
                    .CreateCloudBlobClient();
            Thread.Sleep(5000); // waiting azure to start services
            Progress = 30;
            var container = blobClient.GetContainerReference("crmadaptercontainer");
            try
            {
                container.Create(BlobContainerPublicAccessType.Blob, new BlobRequestOptions());
            }
            catch (StorageException exception)
            {
                if (exception.RequestInformation.HttpStatusCode != 409)
                {
                    throw;
                }
            }
            var blob = container.GetBlockBlobReference("service");
            blob.UploadFromFile(Constants.GetPackPath());
            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24)
            };
            return new AzureStorageData
            {
                SharedBlobUrl = blob.Uri + blob.GetSharedAccessSignature(policy),
                StorageKey = key,
                StorageName = data.StorageAccount
            };
        }

        private void CreateStorageAccount(AzureData data, StorageManagementClient storage, int attempt = 0)
        {
            var availability = storage.StorageAccounts.CheckNameAvailabilityAsync(data.StorageAccount).Result;
            if (availability.NameAvailable)
            {
                storage.StorageAccounts.Create(data.Group.Name, data.StorageAccount,
                    new StorageAccountCreateParameters
                    {
                        Location = data.Location,
                        AccountType = AccountType.StandardRAGRS
                    });
            }
            else
            {
                if (availability.Reason == Reason.AccountNameInvalid)
                {
                    throw new Exception($"Storage Account name is invalid: {availability.Message}");
                }
                data.StorageAccount = data.StorageAccount + DateTime.Now.Second + DateTime.Now.Millisecond;
                LogInfo($"Specified Storage account name is already used changed to {data.StorageAccount}");
                if (attempt > 4)
                {
                    throw new Exception("Cannot generate unique Storage Account name");
                }
                CreateStorageAccount(data, storage, attempt + 1);
            }
        }


        private void CreateHostedService(ComputeManagementClient computeClient, string serviceName, string location)
        {
            try
            {
                computeClient.HostedServices.Get(serviceName);
            }
            catch (Exception)
            {
                computeClient.HostedServices.Create(new HostedServiceCreateParameters
                {
                    ServiceName = serviceName,
                    Location = location
                });
                WizardContext.ServiceUrl = computeClient.HostedServices.Get(serviceName).Uri.ToString();
            }
        }

        private void CreateService(ComputeManagementClient computeClient, AzureStorageData storageData, AzureData data)
        {
            var serviceName = data.Group.Name;
            var configuration =
                $@"<ServiceConfiguration serviceName=""incadea.WsCrm.CloudAdapter"" xmlns=""http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration"" osFamily=""4"" osVersion=""*"" schemaVersion=""2015-04.2.6"">
  <Role name=""{ConfigurationManager.AppSettings["roleName"]}"">
    <Instances count=""1"" />
    <ConfigurationSettings>
      <Setting name=""Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"" 
	  value=""{storageData.ConnectionString}"" />
    </ConfigurationSettings>
  </Role>
</ServiceConfiguration>";
            var deploymentExists = false;
            try
            {
                computeClient.Deployments.GetBySlot(serviceName, DeploymentSlot.Production);
                deploymentExists = true;
            }
            catch
            {
            }

            if (deploymentExists)
            {
                computeClient.Deployments.UpgradeBySlot(serviceName, DeploymentSlot.Production,
                    new DeploymentUpgradeParameters
                    {
                        PackageUri = new Uri(storageData.SharedBlobUrl),
                        Configuration = configuration,
                        Label = serviceName,
                        ExtensionConfiguration =
                            GetExtensionConfiguration(GetDiagnosticsId(computeClient, serviceName, storageData))
                    });
            }
            else
            {
                computeClient.Deployments.Create(serviceName, DeploymentSlot.Production, new DeploymentCreateParameters
                {
                    PackageUri = new Uri(storageData.SharedBlobUrl),
                    Configuration = configuration,
                    Label = serviceName,
                    Name = serviceName,
                    ExtensionConfiguration =
                        GetExtensionConfiguration(GetDiagnosticsId(computeClient, serviceName, storageData))
                });
                computeClient.Deployments.UpdateStatusByDeploymentSlot(serviceName, DeploymentSlot.Production,
                    new DeploymentUpdateStatusParameters(UpdatedDeploymentStatus.Running));
            }
        }

        private ExtensionConfiguration GetExtensionConfiguration(string diagnosticsId)
        {
            return string.IsNullOrWhiteSpace(diagnosticsId)
                ? null
                : new ExtensionConfiguration
                {
                    AllRoles = new List<ExtensionConfiguration.Extension>
                    {
                        new ExtensionConfiguration.Extension(diagnosticsId)
                        {
                            State = "Enable"
                        }
                    }
                };
        }

        private string GetDiagnosticsId(ComputeManagementClient computeClient, string serviceName, AzureStorageData data)
        {
            var diagnosticsType = "PaaSDiagnostics";
            var id = DiagnosticsId;
            var installed = computeClient.HostedServices.ListExtensions(serviceName).Extensions.FirstOrDefault(e => e.Type == diagnosticsType);
            if (installed != null)
            {
                var groups = Regex.Match(installed.PublicConfiguration, @"<StorageAccount>(.*)</StorageAccount>").Groups;
                if (groups.Count > 1 && groups[1].Value.Equals(data.StorageName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return installed.Id;
                }
                LogInfo("Diagnostics data was changed. Extension recreation is not supported yet");
                return installed.Id;
            }
            LogInfo("Checking diagnostics extension availability");
            var available = computeClient.HostedServices.ListAvailableExtensions().FirstOrDefault(p => p.Type == diagnosticsType);
            if (available == null)
            {
                LogError("Diagnostics Extension is not available and cannot be installed. Service logs disabled");
                return null;
            }

            LogInfo("Creating diagnostics extension");
            computeClient.HostedServices.AddExtension(serviceName, new HostedServiceAddExtensionParameters
            {
                Id = id,
                Type = available.Type,
                ProviderNamespace = available.ProviderNameSpace,
                Version = "*",
                PublicConfiguration = GetDiagnosticsConfig(data.StorageName),
                PrivateConfiguration = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<PrivateConfig xmlns=""http://schemas.microsoft.com/ServiceHosting/2010/10/DiagnosticsConfiguration"">
<StorageAccount name=""{data.StorageName}"" key=""{data.StorageKey}""></StorageAccount></PrivateConfig>"
            });
            return id;
        }

        private string GetDiagnosticsConfig(string accountName)
        {
            return 
    $@"<PublicConfig
	xmlns=""http://schemas.microsoft.com/ServiceHosting/2010/10/DiagnosticsConfiguration"">
	<WadCfg>
		<DiagnosticMonitorConfiguration overallQuotaInMB=""4096"" sinks=""applicationInsights.errors"">
			<DiagnosticInfrastructureLogs scheduledTransferLogLevelFilter=""Verbose"" />
			<Directories scheduledTransferPeriod=""PT1M"">
				<IISLogs containerName=""wad-iis-logfiles"" />
				<FailedRequestLogs containerName=""wad-failedrequestlogs"" />
				<DataSources>
					<DirectoryConfiguration containerName=""wad-custom"">
						<Absolute path=""\Resources\Directory"" expandEnvironment=""false"" />
					</DirectoryConfiguration>
				</DataSources>
			</Directories>
			<PerformanceCounters scheduledTransferPeriod=""PT1M"">
				<PerformanceCounterConfiguration counterSpecifier=""\Memory\Available MBytes"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\Web Service(_Total)\ISAPI Extension Requests/sec"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\Web Service(_Total)\Bytes Total/Sec"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\ASP.NET Applications(__Total__)\Requests/Sec"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\ASP.NET Applications(__Total__)\Errors Total/Sec"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\ASP.NET\Requests Queued"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\ASP.NET\Requests Rejected"" sampleRate=""PT3M"" />
				<PerformanceCounterConfiguration counterSpecifier=""\Processor(_Total)\% Processor Time"" sampleRate=""PT3M"" />
			</PerformanceCounters>
			<WindowsEventLog scheduledTransferPeriod=""PT1M"">
				<DataSource name=""Application!*"" />
			</WindowsEventLog>
			<CrashDumps dumpType=""Full"">
				<CrashDumpConfiguration processName=""WaAppAgent.exe"" />
				<CrashDumpConfiguration processName=""WaIISHost.exe"" />
				<CrashDumpConfiguration processName=""WindowsAzureGuestAgent.exe"" />
				<CrashDumpConfiguration processName=""WaWorkerHost.exe"" />
				<CrashDumpConfiguration processName=""DiagnosticsAgent.exe"" />
				<CrashDumpConfiguration processName=""w3wp.exe"" />
			</CrashDumps>
			<Logs scheduledTransferPeriod=""PT1M"" scheduledTransferLogLevelFilter=""Verbose"" />
		</DiagnosticMonitorConfiguration>
		<SinksConfig>
			<Sink name=""applicationInsights"">
				<ApplicationInsights>00595fbe-962d-4243-9ed9-03c07c13d550</ApplicationInsights>
				<Channels>
					<Channel logLevel=""Error"" name=""errors"" />
				</Channels>
			</Sink>
		</SinksConfig>
	</WadCfg>
	<StorageAccount>{accountName}</StorageAccount>
</PublicConfig>";

        }

        #endregion
    }
}
