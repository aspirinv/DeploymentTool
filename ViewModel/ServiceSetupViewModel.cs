using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CsvLoader.Core;
using GalaSoft.MvvmLight.Command;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;
using incadea.WsCrm.DeploymentTool.Utils;
using incadea.WsCrm.DeploymentTool.Utils.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Web.Administration;
using MessageBox = System.Windows.MessageBox;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// view model for azure settings step
    /// </summary>
    public class ServiceSetupViewModel : StepViewModel<ServiceSetup>
    {
        private readonly AzureUtil _azureUtil = new AzureUtil();
        private bool _loginInProgress;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="context">wizard context</param>
        public ServiceSetupViewModel(WizardContext context) : base(context)
        {
            Subscriptions = new ObservableCollection<Subscription>();
            Locations = new ObservableCollection<Location>();
            _tenant = "alexanderspirinrelsys";
            _redirectUrl = "urn:ietf:wg:oauth:2.0:oob";
            _applicationId = "1950a258-227b-4e31-a9cf-717495945fc2";
            _resourceGroupName = "CrmServiceGroup";
            _manager = new ServerManager();

            WebSites = new ObservableCollection<Site>(_manager.Sites);

            ChooseFolderCommand = new RelayCommand(() =>
            {
                var dialog = new FolderBrowserDialog
                {
                    SelectedPath = ServiceFilesPath,
                    ShowNewFolderButton = true,
                    RootFolder = Environment.SpecialFolder.MyComputer
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ServiceFilesPath = dialog.SelectedPath;
                }
            });
            LoginCommand = new RelayCommand(() =>
            {
                LoggedIn = false;
                _loginInProgress = true;
                _azureUtil.GetToken(Tenant, ApplicationId, RedirectUrl)
                    .ContinueWith(task =>
                    {
                        if (task.Status == TaskStatus.Faulted && task.Exception != null)
                        {
                            var message = string.Empty;
                            task.Exception.Handle(ex =>
                            {
                                if (!(ex is AdalServiceException) ||
                                    ((AdalServiceException)ex).ErrorCode != "authentication_canceled")
                                {
                                    message += Environment.NewLine + ex.Message;
                                }
                                return true;
                            });
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                InvokeInUiThread(() => MessageBox.Show(message, "Error on login to azure", MessageBoxButton.OK, MessageBoxImage.Error));
                            }
                            InvokeInUiThread(() => LoggedIn = false);
                        }
                        else
                        {
                            InvokeInUiThread(() =>
                            {
                                LoggedIn = true;
                                RunProfile();
                            });
                        }
                        _loginInProgress = false;
                        InvokeInUiThread(() => LoginCommand.RaiseCanExecuteChanged());
                    });
            },
                () => !_loginInProgress && !LoggedIn &&
                    !string.IsNullOrWhiteSpace(Tenant) &&
                    !string.IsNullOrWhiteSpace(ApplicationId) &&
                    !string.IsNullOrWhiteSpace(RedirectUrl));
        }

        public RelayCommand ChooseFolderCommand { get; set; }

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext()
        {
            return (IsAzureHosting && LoggedIn && Subscription != null && SelectedLocation != null
                    && !string.IsNullOrWhiteSpace(ResourceGroupName)
                    && !string.IsNullOrWhiteSpace(StorageAccount))
                   ||
                   (Directory.Exists(ServiceFilesPath) && (!string.IsNullOrWhiteSpace(SiteName) || SelectedSite != null));
        }

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack() => true;

        /// <summary>
        /// step name
        /// </summary>
        public override string Name => "Service hosting";

        /// <summary>
        /// command to login to azure
        /// </summary>
        public RelayCommand LoginCommand { get; }

        #region Overrides of StepViewModel<ServiceSetup>

        public override void FillWizardContext()
        {
            WizardContext.IsAzureHosting = IsAzureHosting;
            if (IsAzureHosting)
            {
                WizardContext.AzureData.Token = _azureUtil.Token;
                WizardContext.AzureData.Subscription = Subscription.Id;
                WizardContext.AzureData.Location = SelectedLocation.Name;
                WizardContext.AzureData.Group = SelectedGroup ??
                                                new ResourceGroup
                                                {
                                                    Name = ResourceGroupName,
                                                    Location = SelectedLocation.Name
                                                };
                WizardContext.AzureData.Tenant = Tenant;
                WizardContext.AzureData.StorageAccount = StorageAccount;
            }
            else
            {
                WizardContext.OnPremiseData.Path = ServiceFilesPath;
                WizardContext.OnPremiseData.Port = Port;
                WizardContext.OnPremiseData.Site = SelectedSite;
                WizardContext.OnPremiseData.SiteName = SiteName;
            }
        }

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public override void Run()
        {
            if (WizardContext.IsProfileRun)
            {
                IsAzureHosting = WizardContext.IsAzureHosting;
                if (WizardContext.IsAzureHosting)
                {
                    Tenant = WizardContext.AzureData.Tenant;
                    LoginCommand.Execute(null);
                }
                else
                {
                    RunProfile();
                }
            }
        }

        /// <summary>
        /// executed after run in profile mode 
        /// </summary>
        protected override Task OnProfileRun()
        {
            if (IsAzureHosting)
            {
                Subscription = Subscriptions.FirstOrDefault(subscription=>subscription.Id == WizardContext.AzureData.Subscription);
                SelectedLocation = Locations.FirstOrDefault(location => location.Name == WizardContext.AzureData.Location);
                SelectedGroup = ResourceGroups.FirstOrDefault(group => group.Id == WizardContext.AzureData.Group?.Id);
                StorageAccount = WizardContext.AzureData.StorageAccount;
                if (SelectedGroup == null)
                {
                    ResourceGroupName = WizardContext.AzureData.Group?.Name;
                }
            }
            else
            {
                ServiceFilesPath = WizardContext.OnPremiseData.Path;
                Port = WizardContext.OnPremiseData.Port;
                var savedSite = WebSites.FirstOrDefault(
                    site =>
                        site.Name.Equals(WizardContext.OnPremiseData.SiteSerializationValue,
                            StringComparison.InvariantCultureIgnoreCase));
                if (savedSite == null)
                {
                    SiteName = WizardContext.OnPremiseData.SiteSerializationValue;
                }
                else
                {
                    SelectedSite = savedSite;
                }
            }
            return base.OnProfileRun();
        }

        #endregion

        private bool _isAzureHosting = true;
        /// <summary>
        /// Sets and gets the IsAzureHosting property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsAzureHosting
        {
            get { return _isAzureHosting; }
            set
            {
                Set(() => IsAzureHosting, ref _isAzureHosting, value);
            }
        }
        private bool _loggedIn;
        /// <summary>
        /// Sets and gets the LoggedIn property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool LoggedIn
        {
            get { return _loggedIn; }
            set
            {
                _loggedIn = value;
                RaisePropertyChanged(() => LoggedIn);
                LoginCommand.RaiseCanExecuteChanged();
                if (_loggedIn)
                {
                    _azureUtil.GetSubscriptions().ForEach(Subscriptions.Add);
                }
                else
                {
                    Subscriptions.Clear();
                    Subscription = null;
                    Locations.Clear();
                    SelectedLocation = null;
                    ResourceGroups.Clear();
                    SelectedGroup = null;
                }
            }
        }

        private string _tenant;
        /// <summary>
        /// Sets and gets the Tenant property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Tenant
        {
            get { return _tenant; }
            set
            {
                _tenant = value;
                RaisePropertyChanged(() => Tenant);
                LoggedIn = false;
            }
        }


        private string _applicationId;
        /// <summary>
        /// Sets and gets the ApplicationId property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ApplicationId
        {
            get { return _applicationId; }
            set
            {
                _applicationId = value;
                RaisePropertyChanged(() => ApplicationId);
                LoggedIn = false;
            }
        }

        private string _redirectUrl;
        /// <summary>
        /// Sets and gets the RedirectUrl property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string RedirectUrl
        {
            get { return _redirectUrl; }
            set
            {
                _redirectUrl = value;
                RaisePropertyChanged(() => RedirectUrl);
                LoggedIn = false;
            }
        }

        /// <summary>
        /// available subscriptions
        /// </summary>
        public ObservableCollection<Subscription> Subscriptions { get; }

        /// <summary>
        /// available resource groups
        /// </summary>
        public ObservableCollection<ResourceGroup> ResourceGroups { get; } = new ObservableCollection<ResourceGroup>();

        /// <summary>
        /// available storage accounts
        /// </summary>
        public ObservableCollection<string> StorageAccounts { get; } = new ObservableCollection<string>();


        private Subscription _subscription;
        /// <summary>
        /// Sets and gets the Subscription property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Subscription Subscription
        {
            get { return _subscription; }
            set
            {
                _subscription = value;
                RaisePropertyChanged(() => Subscription);
                if (_subscription != null)
                {
                    Locations.Clear();
                    _azureUtil.GetLocations(_subscription.Id).ForEach(Locations.Add);
                    ResourceGroups.Clear();
                    _azureUtil.GetResourceGroups(_subscription.Id).ForEach(ResourceGroups.Add);
                    StorageAccounts.Clear();
                    _azureUtil.GetStorages(_subscription.Id).ForEach(StorageAccounts.Add);
                }
            }
        }

        ///// <summary>
        ///// available locations
        ///// </summary>
        public ObservableCollection<Location> Locations { get; }


        private Location _selectedLocation;
        /// <summary>
        /// Sets and gets the Location property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Location SelectedLocation
        {
            get { return _selectedLocation; }
            set
            {
                _selectedLocation = value;
                RaisePropertyChanged(() => SelectedLocation);
            }
        }


        private string _storageAccount;
        /// <summary>
        /// Sets and gets the StorageAccount property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string StorageAccount
        {
            get { return _storageAccount; }
            set
            {
                Set(() => StorageAccount, ref _storageAccount, value);
            }
        }

        private string _resourceGroupName;
        /// <summary>
        /// Sets and gets the ResourceGroupName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ResourceGroupName
        {
            get { return _resourceGroupName; }
            set
            {
                _resourceGroupName = value;
                RaisePropertyChanged(() => ResourceGroupName);
            }
        }


        private ResourceGroup _selectedGroup;
        /// <summary>
        /// Sets and gets the SelectedGroup property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ResourceGroup SelectedGroup
        {
            get { return _selectedGroup; }
            set
            {
                Set(() => SelectedGroup, ref _selectedGroup, value);
            }
        }


        private string _serviceFilesPath = @"C:\inetpub\wwwroot\CRMAdapter\";
        /// <summary>
        /// Sets and gets the ServiceFilesPath property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ServiceFilesPath
        {
            get { return _serviceFilesPath; }
            set
            {
                Set(() => ServiceFilesPath, ref _serviceFilesPath, value);
            }
        }

        /// <summary>
        /// available IIS sites
        /// </summary>
        public ObservableCollection<Site> WebSites { get; set; }


        private int _port;
        /// <summary>
        /// Sets and gets the Port property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Port
        {
            get { return _port; }
            set
            {
                Set(() => Port, ref _port, value);
            }
        }

        private ServerManager _manager;


        private Site _selectedSite;
        /// <summary>
        /// Sets and gets the SelectedSite property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Site SelectedSite
        {
            get { return _selectedSite; }
            set
            {
                if (value != null)
                {
                    Port = value.Bindings.First(v => v.Protocol.Contains("http")).EndPoint.Port;
                }
                Set(() => SelectedSite, ref _selectedSite, value);
            }
        }
        private string _siteName;

        /// <summary>
        /// Sets and gets the SiteName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SiteName
        {
            get { return _siteName; }
            set
            {
                Set(() => SiteName, ref _siteName, value);
            }
        }
    }
}
