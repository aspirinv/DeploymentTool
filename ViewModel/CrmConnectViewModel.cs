using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// Step to connect to CRM
    /// </summary>
    public class CrmConnectViewModel : StepViewModel<CrmConnect>
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public CrmConnectViewModel(WizardContext context) : base(context)
        {
            TestConnectionCommand = new RelayCommand(()=>TryConnect(), () =>
                !InProgress &&
                !string.IsNullOrWhiteSpace(CrmUrl) && 
                !string.IsNullOrWhiteSpace(Login) &&
                !string.IsNullOrWhiteSpace(Password));
            _factory.Login = Login;
            _factory.Password = Password;
            _factory.Url = CrmUrl;
        }

        #region props and fields

        private readonly CrmServiceFactory _factory = new CrmServiceFactory();

        public RelayCommand TestConnectionCommand { get; }

        private Entity _settings;

        private bool _inProgress;
        /// <summary>
        /// Sets and gets the InProgress property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool InProgress
        {
            get { return _inProgress; }
            set
            {
                Set(() => InProgress, ref _inProgress, value);
                TestConnectionCommand.RaiseCanExecuteChanged();
            }
        }
        private bool _connected;

        private bool Connected
        {
            get { return _connected; }
            set
            {
                Set(() => Connected, ref _connected, value);
                TestConnectionCommand.RaiseCanExecuteChanged();
            }
        }

        private string _crmUrl;
        /// <summary>
        /// Sets and gets the CrmUrl property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string CrmUrl
        {
            get { return _crmUrl; }
            set
            {
                Connected = false;
                _factory.Url = value;
                Set(() => CrmUrl, ref _crmUrl, value);
            }
        }


        private string _login;
        /// <summary>
        /// Sets and gets the Login property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Login
        {
            get { return _login; }
            set
            {
                Connected = false;
                _factory.Login = value;
                Set(() => Login, ref _login, value);
            }
        }

        private string _password;
        /// <summary>
        /// Sets and gets the Password property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                Connected = false;
                _factory.Password = value;
                Set(() => Password, ref _password, value);
            }
        }

        public bool ImportTestDataEnabled => !WizardContext.IsTestDataOnly;

        private bool _importTestData;
        /// <summary>
        /// Sets and gets the ImportTestData property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool ImportTestData
        {
            get { return WizardContext.IsTestDataOnly || _importTestData; }
            set
            {
                Set(() => ImportTestData, ref _importTestData, value);
                if (value)
                {
                    WizardContext.AddStep<ImportTestDataViewModel, FinishedViewModel>(false);
                }
                else
                {
                    WizardContext.RemoveStep<ImportTestDataViewModel>();
                }
            }
        }

        #endregion

        #region Overrides of StepViewModel<CrmConnect>

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public override void Run()
        {
            RaisePropertyChanged(() => ImportTestDataEnabled);
            RaisePropertyChanged(() => ImportTestData);
            WizardContext.RemoveStep<ImportTestDataViewModel>();
            if (ImportTestData)
            {
                WizardContext.AddStep<ImportTestDataViewModel, FinishedViewModel>(false);
            }
            RunProfile();
        }

        /// <summary>
        /// executed after run in profile mode 
        /// </summary>
        protected override Task OnProfileRun()
        {
            CrmUrl = WizardContext.CrmFactory.Url;
            Login = WizardContext.CrmFactory.Login;
            Password = WizardContext.CrmFactory.Password;
            _settings = WizardContext.CrmSettings;
            return TryConnect();
        }

        #endregion

        #region Overrides of StepViewModel

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext() => Connected;

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack() => true;

        /// <summary>
        /// step name
        /// </summary>
        public override string Name { get; } = "Connect to CRM organization";

        /// <summary>
        /// fills gathered parameters into withard context
        /// </summary>
        public override void FillWizardContext()
        {
            WizardContext.CrmFactory = _factory;
            WizardContext.CrmSettings = _settings;
        }

        #endregion

        #region private stuff

        private Task TryConnect()
        {
            InProgress = true;
            SetStateMessage("Connecting...");
            return Task.Run(() =>
            {
                using (var service = _factory.CreateProxyService())
                {
                    service.Execute(new WhoAmIRequest());
                }
            }).ContinueWith(task =>
            {
                InvokeInUiThread(() =>
                {
                    InProgress = false;
                    Connected = !task.IsFaulted;
                    if (task.IsFaulted)
                    {
                        var exception = (Exception) task.Exception;
                        var message = new StringBuilder();
                        while (exception != null)
                        {
                            message.AppendLine(exception.Message);
                            exception = exception.InnerException;
                        }
                        ShowError(message.ToString());
                    }
                    SetStateMessage(string.Empty);
                });
            });
        }

        #endregion

    }
}
