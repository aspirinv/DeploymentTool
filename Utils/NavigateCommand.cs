using System;
using System.Diagnostics;
using System.Windows.Input;

namespace incadea.WsCrm.DeploymentTool.Utils
{
    /// <summary>
    /// Command to open browser in case of navigation
    /// </summary>
    public class NavigateCommand : ICommand
    {
        #region Implementation of ICommand

        /// <summary>
        /// Definiert die Methode, die bestimmt, ob der Befehl im aktuellen Zustand ausgeführt werden kann.
        /// </summary>
        /// <returns>
        /// true, wenn der Befehl ausgeführt werden kann, andernfalls false.
        /// </returns>
        /// <param name="parameter">Vom Befehl verwendete Daten.Wenn der Befehl keine Datenübergabe erfordert, kann das Objekt auf null festgelegt werden.</param>
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Definiert die Methode, die aufgerufen wird, wenn der Befehl aufgerufen wird.
        /// </summary>
        /// <param name="parameter">Vom Befehl verwendete Daten.Wenn der Befehl keine Datenübergabe erfordert, kann das Objekt auf null festgelegt werden.</param>
        public void Execute(object parameter)
        {
            Process.Start(Convert.ToString(parameter));
        }

        public event EventHandler CanExecuteChanged;

        #endregion
    }
}
