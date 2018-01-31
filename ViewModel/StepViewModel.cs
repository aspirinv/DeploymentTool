using System;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using incadea.WsCrm.DeploymentTool.Contracts;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// base step view model
    /// </summary>
    public abstract class StepViewModel<T>: ViewModelBase, IStep 
        where T: IView, new() 
    {
        private bool _isFinished;
        private bool _isActive;

        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        protected StepViewModel(WizardContext context)
        {
            WizardContext = context;
            _currentView = new Lazy<IView>(() => new T { DataContext = this });
        }

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public abstract bool CanGoNext();

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public abstract bool CanGoBack();

        /// <summary>
        /// fills gathered parameters into withard context
        /// </summary>
        public virtual void FillWizardContext()
        {
        }

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public virtual void Run()
        {
        }

        /// <summary>
        /// executed after run in profile mode 
        /// </summary>
        protected virtual Task OnProfileRun()
        {
            return Task.CompletedTask;
        }

        protected void RunProfile()
        {
            if (WizardContext.IsProfileRun)
            {
                OnProfileRun().ContinueWith(task =>
                {
                    if (WizardContext.ProfileAutoRun)
                    {
                        if (CanGoNext())
                        {
                            WizardContext.RaiseSwitchToNext();
                        }
                        else
                        {
                            SetStateMessage("Failed to continue, fix data and press next");
                        }
                    }
                });
            }
        }

        /// <summary>
        /// step position
        /// </summary>
        public int Position { get; set; }

        private readonly Lazy<IView> _currentView;

        /// <summary>
        /// displayed view
        /// </summary>
        public IView CurrentView => _currentView.Value;

        /// <summary>
        /// current wizard context
        /// </summary>
        public WizardContext WizardContext { get; set; }

        /// <summary>
        /// step is finished
        /// </summary>
        public bool IsFinished
        {
            get { return _isFinished; }
            set
            {
                _isFinished = value;
                RaisePropertyChanged(() => IsFinished);
                if (value)
                {
                    FillWizardContext();
                }
            }
        }

        /// <summary>
        /// step is in progress
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                RaisePropertyChanged(() => IsActive);
                if (value)
                {
                    Run();
                }
            }
        }
        
        /// <summary>
        /// step name
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// invokes action in UI thread
        /// </summary>
        /// <param name="action">action to invoke</param>
        protected void InvokeInUiThread(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        /// <summary>
        /// shows error message
        /// </summary>
        /// <param name="error">error content</param>
        protected void ShowError(string error)
        {
            MessageBox.Show(error, "Error", MessageBoxButton.OK);
        }

        /// <summary>
        /// sets state value
        /// </summary>
        public event Action<string> OnSetStateMessage;

        /// <summary>
        /// sets operation status message
        /// </summary>
        /// <param name="state">current state description</param>
        protected void SetStateMessage(string state)
        {
            OnSetStateMessage?.Invoke(state);
        }
    }
}
