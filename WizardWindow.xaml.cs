using System.Windows;
using incadea.WsCrm.DeploymentTool.Contracts;

namespace incadea.WsCrm.DeploymentTool
{
    /// <summary>
    /// Interaction logic for Wizard.xaml
    /// </summary>
    public partial class WizardWindow : Window, IClosable
    {
        /// <summary>
        /// .ctor
        /// </summary>
        public WizardWindow()
        {
            InitializeComponent();
        }
    }
}
