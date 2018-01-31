using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CsvLoader.Core;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Controls;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// crm settings data
    /// </summary>
    public class CrmSettingsViewModel : StepViewModel<CrmSettings>
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public CrmSettingsViewModel(WizardContext context) : base(context)
        {
        }

        #region Overrides of StepViewModel<CrmSettings>

        /// <summary>
        /// check if wizard can go next, current page is valid
        /// </summary>
        /// <returns>true if data on page is valid</returns>
        public override bool CanGoNext()
        {
            return ManagersTeam.IsValid() && CallCenterTeam.IsValid() && DealerRule != null;
        }

        /// <summary>
        /// check if wizard can go back
        /// </summary>
        /// <returns>true if there it's possible to go back</returns>
        public override bool CanGoBack() => false;

        /// <summary>
        /// step name
        /// </summary>
        public override string Name { get; } = "CRM setup";

        /// <summary>
        /// executed if step becomes active
        /// </summary>
        public override void Run()
        {
            CanChangeAvatars = false;
            SetStateMessage("Loading data from CRM");
            Task.Run(() =>
            {
                if (!WizardContext.IsProfileRun || WizardContext.CrmSettings == null)
                {
                    using (var service = WizardContext.CrmFactory.CreateProxyService())
                    {
                        var settings = service.RetrieveMultiple(new QueryExpression("wscrm_settings")
                        {
                            ColumnSet = new ColumnSet(true)
                        });
                        WizardContext.CrmSettings = settings.Entities.FirstOrDefault();
                    }
                }

                InvokeInUiThread(() =>
                {
                    if (WizardContext.CrmSettings == null)
                    {
                        CanChangeAvatars = true;
                        WizardContext.CrmSettings = new Entity("wscrm_settings")
                        {
                            ["wscrm_name"] = "Default",
                            ["wscrm_contactsearchmaxnoofresults"] = 100,
                            ["wscrm_vehiclesearchmaxnoofresults"] = 100,
                            ["wscrm_avatarsenabled"] = false,
                            ["wscrm_servicereminderperioddays"] = 30,
                            ["wscrm_useoverridendistribution"] = false,
                        };
                    }
                    FillSettings(WizardContext.CrmSettings);
                    SetStateMessage(string.Empty);
                });

            }).ContinueWith(task=>
            {
                using (Service = WizardContext.CrmFactory.CreateProxyService())
                {
                    var query = new RetrieveAttributeRequest
                    {
                        EntityLogicalName = "wscrm_settings",
                        LogicalName = "wscrm_defaultdealerrule"
                    };

                    var result = Service.Execute(query);
                    var rules = ((PicklistAttributeMetadata)((RetrieveAttributeResponse)result).AttributeMetadata)
                        .OptionSet.Options
                        .Select(option => new OptionSetViewModel
                        {
                            Value = option.Value ?? 0,
                            Caption = option.Label?.UserLocalizedLabel?.Label
                        });
                    InvokeInUiThread(() =>
                    {
                        DealerRules.Clear();
                        rules.ForEach(option => DealerRules.Add(option));
                        var ruleValue = WizardContext.CrmSettings?.GetAttributeValue<OptionSetValue>("wscrm_defaultdealerrule");
                        if (ruleValue != null)
                        {
                            DealerRule = DealerRules.FirstOrDefault(rule => rule.Value == ruleValue.Value);
                        }
                        RunProfile();
                    });
                }
            });
        }

        private void FillSettings(Entity settings)
        {
            ContactSearchMaxResults = settings.GetAttributeValue<int>("wscrm_contactsearchmaxnoofresults");
            VehicleSearchMaxResult = settings.GetAttributeValue<int>("wscrm_vehiclesearchmaxnoofresults");
            AvatarsEnabled = settings.GetAttributeValue<bool>("wscrm_avatarsenabled");
            ManagersTeam.SetSelected(settings.GetAttributeValue<EntityReference>("wscrm_customermanagers"));
            CallCenterTeam.SetSelected(settings.GetAttributeValue<EntityReference>("wscrm_callcenterteamid"));
            ServiceReminder = settings.GetAttributeValue<int>("wscrm_servicereminderperioddays");
            OverridenDistribution = settings.GetAttributeValue<bool>("wscrm_useoverridendistribution");
        }

        #region Overrides of StepViewModel<CrmSettings>

        /// <summary>
        /// fills gathered parameters into withard context
        /// </summary>
        public override void FillWizardContext()
        {
            WizardContext.CrmSettings["wscrm_contactsearchmaxnoofresults"] = ContactSearchMaxResults;
            WizardContext.CrmSettings["wscrm_vehiclesearchmaxnoofresults"] = VehicleSearchMaxResult;
            WizardContext.CrmSettings["wscrm_avatarsenabled"] = AvatarsEnabled;
            WizardContext.CrmSettings["wscrm_servicereminderperioddays"] = ServiceReminder;
            WizardContext.CrmSettings["wscrm_useoverridendistribution"] = OverridenDistribution;
            WizardContext.CrmSettings["wscrm_defaultdealerrule"] = DealerRule.ToOptionSet();
            WizardContext.CrmSettings["wscrm_customermanagers"] =
                ManagersTeam.Selected ?? new EntityReference("team", Guid.Empty)
                {
                    Name = ManagersTeam.SearchString
                };
            WizardContext.CrmSettings["wscrm_callcenterteamid"] =
                CallCenterTeam.Selected ?? new EntityReference("team", Guid.Empty)
                {
                    Name = CallCenterTeam.SearchString
                };
        }

        #endregion

        #endregion

        #region props and fields

        /// <summary>
        /// dealer rules
        /// </summary>
        public ObservableCollection<OptionSetViewModel> DealerRules { get; set; } = new ObservableCollection<OptionSetViewModel>();


        private OptionSetViewModel _dealerRule;
        /// <summary>
        /// Sets and gets the DealerRule property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public OptionSetViewModel DealerRule
        {
            get { return _dealerRule; }
            set
            {
                Set(() => DealerRule, ref _dealerRule, value);
            }
        }
        private int _contactSearchMaxResults;
        /// <summary>
        /// Sets and gets the ContactSearchMaxResults property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int ContactSearchMaxResults
        {
            get { return _contactSearchMaxResults; }
            set
            {
                if (value > 100 || value < 0) value = 100;
                Set(() => ContactSearchMaxResults, ref _contactSearchMaxResults, value);
            }
        }


        private int _vehicleSearchMaxResult;
        /// <summary>
        /// Sets and gets the VehicleSearchMaxResult property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int VehicleSearchMaxResult
        {
            get { return _vehicleSearchMaxResult; }
            set
            {
                if (value > 100 || value < 0) value = 100;
                Set(() => VehicleSearchMaxResult, ref _vehicleSearchMaxResult, value);
            }
        }


        private bool _avatarsEnabled;
        /// <summary>
        /// Sets and gets the AvatarsEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool AvatarsEnabled
        {
            get { return _avatarsEnabled; }
            set
            {
                Set(() => AvatarsEnabled, ref _avatarsEnabled, value);
            }
        }


        private bool _canChangeAvatars;
        /// <summary>
        /// Sets and gets the CanChangeAvatars property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool CanChangeAvatars
        {
            get { return _canChangeAvatars; }
            set
            {
                Set(() => CanChangeAvatars, ref _canChangeAvatars, value);
            }
        }

        /// <summary>
        /// managers
        /// </summary>
        public LookupViewModel ManagersTeam { get; } = new LookupViewModel("team", "name");

        /// <summary>
        /// call center teams
        /// </summary>
        public LookupViewModel CallCenterTeam { get; } = new LookupViewModel("team", "name");
       
        private OrganizationServiceProxy _service;
        /// <summary>
        /// service object
        /// </summary>
        private OrganizationServiceProxy Service
        {
            get { return _service; }
            set
            {
                _service?.Dispose();
                _service = value;
                ManagersTeam.Service = value;
                CallCenterTeam.Service = value;
            }
        }


        private int _serviceReminder;
        /// <summary>
        /// Sets and gets the ServiceRminder property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int ServiceReminder
        {
            get { return _serviceReminder; }
            set
            {
                Set(() => ServiceReminder, ref _serviceReminder, value);
            }
        }


        private bool _overridenDistribution;
        /// <summary>
        /// Sets and gets the OverridenDistribution property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool OverridenDistribution
        {
            get { return _overridenDistribution; }
            set
            {
                Set(() => OverridenDistribution, ref _overridenDistribution, value);
            }
        }


        #endregion

    }
}
