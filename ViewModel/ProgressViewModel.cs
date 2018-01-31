using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CsvLoader.Core.Contracts;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// view model for progress display and installation perform
    /// </summary>
    public abstract class ProgressViewModel : StepViewModel<Progress>, ILogger
    {

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext() => Progress >= 100;

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack() => !Installing && Progress < 100;

        private int _progress;
        private readonly StringBuilder _logs = new StringBuilder();

        /// <summary>
        /// Sets and gets the Progress property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Progress
        {
            get { return _progress; }
            set
            {
                InvokeInUiThread(()=> Set(() => Progress, ref _progress, value));
            }
        }

        /// <summary>
        /// real work goes here
        /// </summary>
        protected abstract void RunInternal();

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public override void Run()
        {
            Installing = true;
            _logs.Clear();
            Task.Run(() => RunInternal())
                .ContinueWith(task =>
                {
                    InvokeInUiThread(() => Installing = false);
                    if (task.IsFaulted && task.Exception != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LogError(task.Exception.Message);
                            foreach (var exception in task.Exception.InnerExceptions)
                            {
                                LogError(exception.Message);
                            }
                        });
                    }
                    else
                    {
                        var logs = _logs.ToString();
                        if (!string.IsNullOrWhiteSpace(logs))
                        {
                            WizardContext.FullLogs.AppendLine();
                            WizardContext.FullLogs.AppendLine($"Logs from {GetType().Name}");
                            WizardContext.FullLogs.AppendLine(logs);
                        }
                        WizardContext.RaiseSwitchToNext();
                    }
                });
        }

        /// <summary>
        /// logs content
        /// </summary>
        public string Log => _logs.ToString();

        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        protected ProgressViewModel(WizardContext context) : base(context)
        {
        }

        /// <summary>
        /// logs error
        /// </summary>
        /// <param name="error">info</param>
        public void LogError(string error)
        {
            InvokeInUiThread(() =>
            {
                _logs.AppendLine($"[ERROR] {error}");
                RaisePropertyChanged(() => Log);
            });
        }

        /// <summary>
        /// logs info
        /// </summary>
        /// <param name="info">error</param>
        public void LogInfo(string info)
        {
            InvokeInUiThread(() =>
            {
                _logs.AppendLine($"[INFO] {info}");
                RaisePropertyChanged(() => Log);
            });
        }


        private bool _installing;
        /// <summary>
        /// Sets and gets the Installing property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Installing
        {
            get { return _installing; }
            set
            {
                _installing = value;
                RaisePropertyChanged(() => Installing);
            }
        }

        #region Implementation of ILogger

        /// <summary>
        /// logs message
        /// </summary>
        /// <param name="message">message to be logged</param>
        void ILogger.Log(string message)
        {
            LogInfo(message);
        }

        #endregion
    }
}
