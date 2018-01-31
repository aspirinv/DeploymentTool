using System;
using System.ComponentModel;

namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// interface for step 
    /// </summary>
    public interface IStep : INotifyPropertyChanged
    {
        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        bool CanGoNext();

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        bool CanGoBack();

        /// <summary>
        /// step position
        /// </summary>
        int Position { get; set; }

        /// <summary>
        /// displayed view
        /// </summary>
        IView CurrentView { get; }

        /// <summary>
        /// current wizard context
        /// </summary>
        WizardContext WizardContext { get; set; }

        /// <summary>
        /// step is finished
        /// </summary>
        bool IsFinished { get; set; }

        /// <summary>
        /// step is in progress
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// step name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// sets state value
        /// </summary>
        event Action<string> OnSetStateMessage;
    }
}
