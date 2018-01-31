/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:incadea.RETWeb.Installer"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using incadea.WsCrm.DeploymentTool.Contracts;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// inits IoC
        /// </summary>
        public static void InitIoc()
        {
            // ServiceLocator.SetLocatorProvider(()=> ServiceLocator.Current);

            SimpleIoc.Default.Register<WizardViewModel>();
            SimpleIoc.Default.Register<WizardContext, WizardContext>();
        }
    }
}