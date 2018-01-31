using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.Command;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;
using Microsoft.Win32;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// choise between azure and on premise
    /// </summary>
    public class InstallationTypeChoiseViewModel : StepViewModel<InstallationTypeChoise>
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public InstallationTypeChoiseViewModel(WizardContext context) : base(context)
        {
            UseProfileCommand = new RelayCommand(LoadProfile);
        }

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext() => IsFull || IsDataImport || IsDataOnly;

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack() => true;

        /// <summary>
        /// step name
        /// </summary>
        public override string Name => "Installation Mode";

        /// <summary>
        /// command to choose profile and run according to it
        /// </summary>
        public RelayCommand UseProfileCommand { get; }

        #region Overrides of StepViewModel<InstallationTypeChoise>

        /// <summary>
        /// executed after run in profile mode 
        /// </summary>
        protected override Task OnProfileRun()
        {
            IsDataOnly = WizardContext.IsTestDataOnly;
            IsFull = WizardContext.SolutionImportMode == SolutionImportMode.Full;
            return base.OnProfileRun();
        }

        /// <summary>
        /// fills gathered parameters into withard context
        /// </summary>
        public override void FillWizardContext()
        {
            WizardContext.SolutionImportMode = IsFull ? SolutionImportMode.Full : SolutionImportMode.DataImport;
            WizardContext.IsTestDataOnly = IsDataOnly;
        }

        #endregion

        private bool _isFull;
        /// <summary>
        /// Sets and gets the IsFull property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsFull
        {
            get { return _isFull; }
            set
            {
                Set(() => IsFull, ref _isFull, value);
                if (value)
                {
                    WizardContext.RemoveStepsAfter(Position + 1);
                    AddCrmSteps();
                    WizardContext.AppendStep<ServiceOptionsViewModel>();
                    WizardContext.AppendStep<ServiceSetupViewModel>();
                    WizardContext.AppendStep<ServiceInstallViewModel>();

                    WizardContext.AppendStep<FinishedViewModel>();
                }
            }
        }


        private bool _isDataImport;
        /// <summary>
        /// Sets and gets the IsDataImport property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDataImport
        {
            get { return _isDataImport; }
            set
            {
                Set(() => IsDataImport, ref _isDataImport, value);
                if (value)
                {
                    WizardContext.RemoveStepsAfter(Position + 1);
                    AddCrmSteps();
                    WizardContext.AppendStep<FinishedViewModel>();
                }
            }
        }
        private bool _isDataOnly;
        /// <summary>
        /// Sets and gets the IsDataOnly property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDataOnly
        {
            get { return _isDataOnly; }
            set
            {
                Set(() => IsDataOnly, ref _isDataOnly, value);
                if (value)
                {
                    WizardContext.RemoveStepsAfter(Position + 1);
                    WizardContext.AppendStep<FinishedViewModel>();
                }
            }
        }

        private void AddCrmSteps()
        {
            WizardContext.AppendStep<CrmImportViewModel>();
            WizardContext.AppendStep<CrmSettingsViewModel>();
            WizardContext.AppendStep<ConfigurationsImportViewModel>();
        }

        private void LoadProfile()
        {
            var dialog = new OpenFileDialog {Filter = Constants.ProfileFilter};
            if (dialog.ShowDialog() ?? false)
            {
                Task.Run(() =>
                {
                    var profile = (WizardContext) new DataContractSerializer(typeof (WizardContext))
                        .ReadObject(File.OpenRead(dialog.FileName));
                    WizardContext.SetProfile(profile);
                    InvokeInUiThread(RunProfile);
                });
            }
        }
    }
}
