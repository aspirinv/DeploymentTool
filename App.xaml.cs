using System.Net;
using System.Windows;
using incadea.WsCrm.DeploymentTool.ViewModel;

namespace incadea.WsCrm.DeploymentTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// startup handler
        /// </summary>
        /// <param name="e">event ergs</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            ViewModelLocator.InitIoc();
            base.OnStartup(e);
        }
    }
}
