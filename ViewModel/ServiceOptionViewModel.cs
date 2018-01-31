using GalaSoft.MvvmLight;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// App config option View model
    /// </summary>
    public class ServiceOptionViewModel : ViewModelBase
    {

        private bool _required;
        /// <summary>
        /// Sets and gets the Required property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Required
        {
            get { return _required; }
            set
            {
                Set(() => Required, ref _required, value);
            }
        }
        private string _value;
        /// <summary>
        /// Sets and gets the Value property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Value
        {
            get { return _value; }
            set
            {
                Set(() => Value, ref _value, value);
            }
        }
        private string _name;
        /// <summary>
        /// Sets and gets the Name property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                Set(() => Name, ref _name, value);
            }
        }
    }
}
