using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using CsvLoader.Core;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;
using incadea.WsCrm.DeploymentTool.Utils;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// Service options
    /// </summary>
    public class ServiceOptionsViewModel : StepViewModel<ServiceOptions>
    {

        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public ServiceOptionsViewModel(WizardContext context) : base(context)
        {
        }

        #region Overrides of StepViewModel<ServiceOptions>

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext()
        {
            return AppSettings.All(setting => !setting.Required || !string.IsNullOrWhiteSpace(setting.Value));
        }

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack() => false;

        /// <summary>
        /// step name
        /// </summary>
        public override string Name { get; } = "Service options";

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public override void Run()
        {
            if (AppSettings.Any())
            {
                return;
            }
            _cssxFileName = Unpack();
            var configFile =
                Directory.GetFiles(Constants.ContentPackTemp, "web.config", SearchOption.AllDirectories).First();
            var settingKeys = _config.GetAppSettings(configFile);
            var requiredFields = new List<string>
            {
                "appclientid",
                "appkey"
            };
            if (WizardContext.CrmFactory.Url.Contains("dynamics.com"))
            {
                requiredFields.Add("crmtenant");
                requiredFields.Add("adfshost");
            }
            var ignoreFields = new List<string>
            {
                "aspnet:usetaskfriendlysynchronizationcontext",
                "crmservicepath", "clientversion"

            };
            foreach (var settingKey in settingKeys.Except(ignoreFields))
            {
                var option = new ServiceOptionViewModel
                {
                    Name = settingKey,
                    Required = requiredFields.Contains(settingKey)
                };
                AppSettings.Add(option);
                option.PropertyChanged += (sender, args) => RaisePropertyChanged();
            }
            RunProfile();
        }

        /// <summary>
        /// fills gathered parameters into withard context
        /// </summary>
        public override void FillWizardContext()
        {
            foreach (var setting in AppSettings)
            {
                WizardContext.AppSettings[setting.Name] = setting.Value;
            }
            WizardContext.IsSftp = IsSftp;

            FixConfig();
            _zip.ZipFolder(Constants.ContentPackTemp, _cssxFileName);

            var hashes = Directory.GetFiles(Constants.RootPackTemp, "*.csman").First();
            var hash = string.Empty;
            using (var file = File.OpenRead(_cssxFileName))
            {
                var hashBytes = new SHA256Managed().ComputeHash(file);
                hash = BitConverter.ToString(hashBytes).Replace("-", "");
            }

            var doc = new XmlDocument();
            doc.Load(hashes);
            var node = doc.SelectSingleNode($"//Item[@uri='/{Path.GetFileName(_cssxFileName)}']");
            node.Attributes["hash"].Value = hash;
            doc.Save(hashes);

            _zip.ZipFolder(Constants.RootPackTemp, Constants.GetPackPath());
        }

        /// <summary>
        /// executed after run in profile mode 
        /// </summary>
        protected override Task OnProfileRun()
        {
            foreach (var appsetting in WizardContext.AppSettings)
            {
                var item = AppSettings.FirstOrDefault(setting=>setting.Name == appsetting.Key);
                if (item != null)
                {
                    item.Value = appsetting.Value;
                }
            }
            IsSftp = WizardContext.IsSftp;

            return base.OnProfileRun();
        }

        #endregion

        #region props and fields

        private readonly ZipUtil _zip = new ZipUtil();
        private string _cssxFileName;
        private readonly WebConfigChangeHelper _config = new WebConfigChangeHelper();

        /// <summary>
        /// App settings collection
        /// </summary>
        public ObservableCollection<ServiceOptionViewModel> AppSettings { get; set; } = new ObservableCollection<ServiceOptionViewModel>();


        private bool _isSftp;
        /// <summary>
        /// Sets and gets the IsSftp property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSftp
        {
            get { return _isSftp; }
            set
            {
                Set(() => IsSftp, ref _isSftp, value);
            }
        }

        #endregion

        #region private stuff

        private string Unpack()
        {
            if (Directory.Exists(Constants.RootPackTemp))
            {
                Directory.Delete(Constants.RootPackTemp, true);
            }
            _zip.UnzipFolder(Constants.GetPackPath(), Constants.RootPackTemp);
            var packFile = Directory.GetFiles(Constants.RootPackTemp, "*.cssx").First();
            _zip.UnzipFolder(packFile, Constants.ContentPackTemp);
            return packFile;
        }

        private void FixConfig()
        {
            Directory.GetFiles(Constants.ContentPackTemp, "web.config", SearchOption.AllDirectories)
                .ForEach(file => _config.UpdateConfig(file, WizardContext));
        }

        #endregion
    }
}
