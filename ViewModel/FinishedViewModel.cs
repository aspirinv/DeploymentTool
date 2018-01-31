using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.Command;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// view model for last step
    /// </summary>
    public class FinishedViewModel : StepViewModel<Finished>
    {
        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext() => false;

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack()=> false;

        /// <summary>
        /// step name
        /// </summary>
        public override string Name => "Finished";

        /// <summary>
        /// command to save profile
        /// </summary>
        public RelayCommand SaveProfileCommand { get; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="context">wizard context</param>
        public FinishedViewModel(WizardContext context) : base(context)
        {
            SaveProfileCommand = new RelayCommand(SaveProfile);
        }

        #region Overrides of StepViewModel<Finished>

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public override void Run()
        {
            FullLog = WizardContext.FullLogs.ToString();
            SettingsUrl =
                $"{WizardContext.CrmFactory.Url}/main.aspx?pagetype=entityrecord&id={{{WizardContext.CrmSettings.Id}}}&etn=wscrm_settings";
            ServiceUrl = WizardContext.ServiceUrl;
        }

        #endregion

        private string _serviceUrl;
        /// <summary>
        /// Sets and gets the ServiceUrl property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ServiceUrl
        {
            get { return _serviceUrl; }
            set
            {
                Set(() => ServiceUrl, ref _serviceUrl, value);
            }
        }
        private string _settingsUrl;
        /// <summary>
        /// Sets and gets the SettingsUrl property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SettingsUrl
        {
            get { return _settingsUrl; }
            set
            {
                Set(() => SettingsUrl, ref _settingsUrl, value);
            }
        }
        private string _fullLog;
        /// <summary>
        /// Sets and gets the FullLog property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string FullLog
        {
            get { return _fullLog; }
            set
            {
                Set(() => FullLog, ref _fullLog, value);
            }
        }

        private void SaveProfile()
        {
            var dialog = new SaveFileDialog {Filter = Constants.ProfileFilter};
            if (dialog.ShowDialog() ?? false)
            {
                new DataContractSerializer(typeof (WizardContext)).WriteObject(
                    File.Open(dialog.FileName, FileMode.Create, FileAccess.Write), WizardContext);
                SetStateMessage("Profile saved");
            }
        }
    }
}
