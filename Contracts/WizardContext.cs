using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using incadea.WsCrm.DeploymentTool.ViewModel;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// context for wizard
    /// </summary>
    public class WizardContext
    {
        private readonly List<IStep> _steps = new List<IStep>();
        private readonly Dictionary<Type, Lazy<IStep>> _stepPool = new Dictionary<Type, Lazy<IStep>>();

        /// <summary>
        /// .ctor
        /// </summary>
        public WizardContext()
        {
            AzureData = new AzureData();
            OnPremiseData = new OnPremiseData();

            _stepPool.Add(typeof (CrmConnectViewModel), new Lazy<IStep>(() => new CrmConnectViewModel(this)));
            _stepPool.Add(typeof (InstallationTypeChoiseViewModel),
                new Lazy<IStep>(() => new InstallationTypeChoiseViewModel(this)));
            _stepPool.Add(typeof (ServiceOptionsViewModel), new Lazy<IStep>(() => new ServiceOptionsViewModel(this)));
            _stepPool.Add(typeof (CrmImportViewModel), new Lazy<IStep>(() => new CrmImportViewModel(this)));
            _stepPool.Add(typeof (FinishedViewModel), new Lazy<IStep>(() => new FinishedViewModel(this)));
            _stepPool.Add(typeof (CrmSettingsViewModel), new Lazy<IStep>(() => new CrmSettingsViewModel(this)));
            _stepPool.Add(typeof (ConfigurationsImportViewModel), new Lazy<IStep>(() => new ConfigurationsImportViewModel(this)));
            _stepPool.Add(typeof (ServiceSetupViewModel), new Lazy<IStep>(() => new ServiceSetupViewModel(this)));
            _stepPool.Add(typeof (ServiceInstallViewModel), new Lazy<IStep>(() => new ServiceInstallViewModel(this)));
            _stepPool.Add(typeof (ImportTestDataViewModel), new Lazy<IStep>(() => new ImportTestDataViewModel(this)));

            AppendStep<InstallationTypeChoiseViewModel>();
            AppendStep<CrmConnectViewModel>();
            CrmConfig = (CrmConfigSection) ConfigurationManager.GetSection("CrmConfigSection");
        }

        /// <summary>
        /// list of steps
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<IStep> Steps => _steps.OrderBy(step => step.Position);

        /// <summary>
        /// adds new step to wizard
        /// </summary>
        /// <param name="isAfter">defines if new step should be added before or after the old one</param>
        /// <returns>added step</returns>
        public IStep AddStep<TNewStep, TOldStep>(bool isAfter = true) 
            where TNewStep : IStep 
            where TOldStep : IStep
        {
            var step = _stepPool[typeof (TNewStep)].Value;
            var baseStep = _stepPool[typeof(TOldStep)].Value;
            step.WizardContext = this;
            if (baseStep != null)
            {
                step.Position = baseStep.Position + (isAfter ? 1 : 0);
                foreach (var latestStep in _steps.Where(oldStep => oldStep.Position >= step.Position))
                {
                    latestStep.Position++;
                }
            }
            else
            {
                step.Position = _steps.Count + 1;
            }
            _steps.Add(step);

            StepsListChanged?.Invoke(this, EventArgs.Empty);
            return step;
        }

        public void AppendStep<T>() where T : IStep
        {
            var step = _stepPool[typeof(T)].Value;
            step.Position = _steps.Count + 1;
            _steps.Add(step);
            StepsListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// removes step of provided type
        /// </summary>
        /// <typeparam name="T">step type</typeparam>
        public void RemoveStep<T>() where T : IStep
        {
            RemoveStep(_stepPool[typeof(T)].Value.Position);
        }
        /// <summary>
        /// removes step from postition
        /// </summary>
        /// <param name="position">step position to remove</param>
        public void RemoveStep(int position)
        {
            var step = _steps.FirstOrDefault(item => item.Position == position);
            if (step != null)
            {
                _steps.Remove(step);
                foreach (var latestStep in _steps.Where(oldStep => oldStep.Position > position))
                {
                    latestStep.Position--;
                }
            }
            StepsListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// removes all steps after specified position
        /// </summary>
        /// <param name="position">current position</param>
        public void RemoveStepsAfter(int position)
        {
            _steps.RemoveAll(s => s.Position > position);
            StepsListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// returns next step
        /// </summary>
        /// <param name="currentStep">current step</param>
        /// <returns>step after current step</returns>
        public IStep GetNext(IStep currentStep)
        {
            return _steps.FirstOrDefault(step => step.Position == currentStep.Position + 1);
        }

        /// <summary>
        /// returns previous step
        /// </summary>
        /// <param name="currentStep">current step</param>
        /// <returns>step before current step</returns>
        public IStep GetPrevious(IStep currentStep)
            => _steps.FirstOrDefault(step => step.Position == currentStep.Position - 1);

        /// <summary>
        /// check if there any possible steps to go next
        /// </summary>
        /// <param name="currentStep">current step</param>
        /// <returns>true if there is one</returns>        
        public bool HasNext(IStep currentStep) => _steps.Any(step => step.Position > currentStep.Position);

        /// <summary>
        /// check if there any possible steps to go back
        /// </summary>
        /// <param name="currentStep">current step</param>
        /// <returns>true if there is one</returns>
        public bool HasPrevious(IStep currentStep) => _steps.Any(step => step.Position < currentStep.Position);

        /// <summary>
        /// stores azure data
        /// </summary>
        public AzureData AzureData { get; set; }

        /// <summary>
        /// crm factory
        /// </summary>
        public CrmServiceFactory CrmFactory { get; set; }

        /// <summary>
        /// crm configuration data
        /// </summary>
        public CrmConfigSection CrmConfig { get; }

        /// <summary>
        /// Solution import mode
        /// </summary>
        public SolutionImportMode SolutionImportMode { get; set; }

        /// <summary>
        /// defines if only test data import 
        /// </summary>
        public bool IsTestDataOnly { get; set; }

        /// <summary>
        /// current settings id
        /// </summary>
        public Entity CrmSettings { get; set; }

        /// <summary>
        /// defines if service will be hosted to azure
        /// </summary>
        public bool IsAzureHosting { get; set; }

        /// <summary>
        /// data for local service hosting
        /// </summary>
        public OnPremiseData OnPremiseData { get; set; }

        /// <summary>
        /// defines if data loaded by profile
        /// </summary>
        [IgnoreDataMember]
        public bool IsProfileRun { get; set; }

        /// <summary>
        /// defines if profile deployment should goes with minimal user interraction
        /// </summary>
        public bool ProfileAutoRun { get; set; }

        /// <summary>
        /// data that will be placed in web.config
        /// </summary>
        public Dictionary<string, string> AppSettings { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Used to save app settings in profile
        /// </summary>
        public List<KeyValuePair<string, string>> AppSettingsSerialization
        {
            get { return AppSettings.Select(pair => pair).ToList(); }
            set { AppSettings = value?.ToDictionary(pair => pair.Key, pair => pair.Value); }
        }
        /// <summary>
        /// defines if sftp is used for ftp file uploading
        /// </summary>
        public bool IsSftp { get; set; }

        /// <summary>
        /// Logs from all Progress view models
        /// </summary>
        public StringBuilder FullLogs { get; set; } = new StringBuilder();

        /// <summary>
        /// Url to the service
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// event handler for step list changes
        /// </summary>
        public event EventHandler StepsListChanged;

        /// <summary>
        /// event for automatic move to next step
        /// </summary>
        public event EventHandler SwitchToNext;
        
        /// <summary>
        /// raises SwitchToNext event
        /// </summary>
        public void RaiseSwitchToNext()
        {
            SwitchToNext?.Invoke(this, EventArgs.Empty);
        }

        public void SetProfile(WizardContext profile)
        {
            IsProfileRun = true;
            SolutionImportMode = profile.SolutionImportMode;
            AppSettings = profile.AppSettings;
            AzureData = profile.AzureData;
            OnPremiseData = profile.OnPremiseData;
            CrmSettings = profile.CrmSettings;
            IsAzureHosting= profile.IsAzureHosting;
            IsSftp= profile.IsSftp;
            IsTestDataOnly= profile.IsTestDataOnly;
            CrmFactory = profile.CrmFactory;
            ProfileAutoRun = profile.ProfileAutoRun;
        }
    }
}
