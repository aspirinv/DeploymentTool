using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// view model for main wizard window
    /// </summary>
    public class WizardViewModel : ViewModelBase
    {
        private readonly WizardContext _context;
        private RelayCommand _goBackCommand;
        private RelayCommand _goNextCommand;
        private RelayCommand _settingsCommand;
        private RelayCommand<IClosable> _finishCommand;
        private readonly SettingsViewModel _settingsModel;
        private IStep _currentStep;

        private IStep CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (_currentStep != null)
                {
                    _currentStep.PropertyChanged -= StepPropertyChanged;
                    _currentStep.OnSetStateMessage -= CurrentStepOnOnSetStateMessage;
                }
                _currentStep = value;
                _currentStep.PropertyChanged += StepPropertyChanged;
                _currentStep.OnSetStateMessage += CurrentStepOnOnSetStateMessage;
                RaisePropertyChanged(() => CurrentStep);
                RaisePropertyChanged(() => CurrentView);
                RaisePropertyChanged(() => CanChangeSettings);
                GoNextCommand.RaiseCanExecuteChanged();
                GoBackCommand.RaiseCanExecuteChanged();
                FinishCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// .ctor
        /// </summary>
        public WizardViewModel()
        {
            _context = SimpleIoc.Default.GetInstance<WizardContext>();
            CurrentStep = _context.Steps.First();
            CurrentStep.IsActive = true;
            _context.StepsListChanged += (sender, args) => RaisePropertyChanged(() => Steps);
            _context.SwitchToNext +=
                (sender, args) => Application.Current.Dispatcher.Invoke(() => GoNextCommand.Execute(null));
            _settingsModel = new SettingsViewModel();
        }

        /// <summary>
        /// view to display
        /// </summary>
        public IView CurrentView => CurrentStep.CurrentView;

        /// <summary>
        /// list of steps
        /// </summary>
        public IEnumerable<IStep> Steps => _context.Steps;

        /// <summary>
        /// defines if programm settings can be changed at current step
        /// </summary>
        public bool CanChangeSettings => !(CurrentStep is FinishedViewModel) && !(CurrentStep is ProgressViewModel);

        /// <summary>
        /// command for go next button
        /// </summary>
        public RelayCommand GoNextCommand
        {
            get
            {
                return _goNextCommand ?? (_goNextCommand = new RelayCommand(
                    () =>
                    {
                        CurrentStep.IsActive = false;
                        CurrentStep.IsFinished = true;
                        CurrentStep = _context.GetNext(CurrentStep);
                        CurrentStep.IsActive = true;
                    },
                    () => CurrentStep.CanGoNext() && _context.HasNext(CurrentStep)));
            }
        }

        /// <summary>
        /// command for go back button
        /// </summary>
        public RelayCommand GoBackCommand
        {
            get
            {
                return _goBackCommand ?? (_goBackCommand = new RelayCommand(
                    () =>
                    {
                        CurrentStep.IsActive = false;
                        CurrentStep.IsFinished = false;
                        CurrentStep = _context.GetPrevious(CurrentStep);
                        CurrentStep.IsActive = true;
                    },
                    () => CurrentStep.CanGoBack() && _context.HasPrevious(CurrentStep)));
            }
        }
        /// <summary>
        /// command for go back button
        /// </summary>
        public RelayCommand<IClosable> FinishCommand
        {
            get
            {
                return _finishCommand ?? (_finishCommand = new RelayCommand<IClosable>(
                    par => par.Close(),
                    par => !_context.HasNext(CurrentStep)));
            }
        }

        /// <summary>
        /// command to open settings
        /// </summary>
        public RelayCommand OpenSettingsCommand
        {
            get
            {
                return _settingsCommand ?? (_settingsCommand = new RelayCommand(
                    () =>
                    {
                        new SettingsWindow
                        {
                            DataContext = _settingsModel
                        }.ShowDialog();
                    }));
            }
        }
        /// <summary>
        /// steps property changed event handler
        /// </summary>
        /// <param name="sender">step</param>
        /// <param name="e">Property changed event args</param>
        public void StepPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GoNextCommand.RaiseCanExecuteChanged();
            GoBackCommand.RaiseCanExecuteChanged();
        }
        
        private void CurrentStepOnOnSetStateMessage(string state)
        {
            StateMessage = state;
        }

        public bool StateSet => !string.IsNullOrWhiteSpace(StateMessage);

        private string _stateMessage;
        /// <summary>
        /// Sets and gets the StateMessage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string StateMessage
        {
            get { return _stateMessage; }
            set
            {
                Set(() => StateMessage, ref _stateMessage, value);
                RaisePropertyChanged(() => StateSet);
            }
        }
    }
}
