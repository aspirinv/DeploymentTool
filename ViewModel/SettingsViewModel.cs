using System;
using System.Net;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using incadea.WsCrm.DeploymentTool.Contracts;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// view model for settings
    /// </summary>
    public class SettingsViewModel: ViewModelBase
    {
        private const string ProxyTestUri = "http://azure.com/";
        /// <summary>
        /// .ctor initializes default settings
        /// </summary>
        public SettingsViewModel()
        {

            var proxyAddress = WebRequest.DefaultWebProxy.GetProxy(new Uri(ProxyTestUri));
            if (!string.IsNullOrWhiteSpace(proxyAddress?.AbsoluteUri) && ProxyTestUri != proxyAddress.AbsoluteUri)
            {
                ProxyServerName = proxyAddress.AbsoluteUri;
            }
            ProxyLogin = Environment.UserName;
            ProxyDomain = Environment.UserDomainName;
            if (!string.IsNullOrWhiteSpace(CredentialCache.DefaultNetworkCredentials?.UserName))
            {
                ProxyLogin = CredentialCache.DefaultNetworkCredentials.UserName;
                ProxyPassword = CredentialCache.DefaultNetworkCredentials.Password;
                ProxyDomain = CredentialCache.DefaultNetworkCredentials.Domain;
            }
            ApplySettingsCommand = new RelayCommand<IClosable>(par =>
            {
                if (!string.IsNullOrWhiteSpace(ProxyServerName))
                {
                    WebRequest.DefaultWebProxy = new WebProxy(ProxyServerName);
                }
                if (!string.IsNullOrWhiteSpace(ProxyLogin)
                    && !string.IsNullOrWhiteSpace(ProxyPassword))
                {
                    var credentials = new NetworkCredential(ProxyLogin, ProxyPassword);
                    if (!string.IsNullOrWhiteSpace(ProxyDomain))
                    {
                        credentials.Domain = ProxyDomain;
                    }
                    WebRequest.DefaultWebProxy.Credentials = credentials;
                }
                par.Close();
            });
        }

        /// <summary>
        /// command to apply system settings
        /// </summary>
        public RelayCommand<IClosable> ApplySettingsCommand { get; set; }

        /// <summary>
        /// returns data from settings
        /// </summary>
        /// <returns>settings data</returns>
        public SettingsData GetData()
        {
            return new SettingsData
            {
                ProxyLogin = ProxyLogin,
                ProxyPassword = ProxyPassword,
                ProxyServer = ProxyServerName,
                ProxyDomain = ProxyDomain
            };
        }

        private string _proxyServerName;
        /// <summary>
        /// Sets and gets the ProxyServer property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProxyServerName
        {
            get { return _proxyServerName; }
            set
            {
                _proxyServerName = value;
                RaisePropertyChanged(() => ProxyServerName);
            }
        }

        private string _proxyLogin;
        /// <summary>
        /// Sets and gets the ProxyLogin property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProxyLogin
        {
            get { return _proxyLogin; }
            set
            {
                _proxyLogin = value;
                RaisePropertyChanged(() => ProxyLogin);
            }
        }

        private string _proxyPassword;
        /// <summary>
        /// Sets and gets the ProxyPassword property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProxyPassword
        {
            get { return _proxyPassword; }
            set
            {
                _proxyPassword = value;
                RaisePropertyChanged(() => ProxyPassword);
            }
        }

        private string _proxyDomain;
        /// <summary>
        /// Sets and gets the ProxyDomain property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProxyDomain
        {
            get { return _proxyDomain; }
            set
            {
                _proxyDomain = value;
                RaisePropertyChanged(() => ProxyDomain);
            }
        }

    }
}
