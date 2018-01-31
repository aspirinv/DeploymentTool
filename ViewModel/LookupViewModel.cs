using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CsvLoader.Core;
using GalaSoft.MvvmLight;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// CRM lookup view model with search function
    /// </summary>
    public class LookupViewModel : ViewModelBase
    {
        private readonly string _entity;
        private readonly string _nameField;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="entity">entity to look up</param>
        /// <param name="nameField">field name to display</param>
        public LookupViewModel(string entity, string nameField)
        {
            _entity = entity;
            _nameField = nameField;
            _searchString = string.Empty;
        }

        private EntityReference _selected;
        /// <summary>
        /// Sets and gets the Property property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public EntityReference Selected
        {
            get { return _selected; }
            set
            {
                Set(() => Selected, ref _selected, value);
            }
        }


        private string _searchString;

        public OrganizationServiceProxy Service { get; set; }

        /// <summary>
        /// Sets and gets the ManagersSearch property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SearchString
        {
            get { return _searchString; }
            set
            {
                if (value.Length > 3 && value != Selected?.Name)
                {
                    var reload = value.Length < _searchString.Length || value.Contains(_searchString);
                    if (reload)
                    {
                        Task.Run(() =>
                        {
                            var query = new QueryExpression(_entity) { ColumnSet = new ColumnSet(_nameField) };
                            query.Criteria.AddCondition(_nameField, ConditionOperator.Like, $"%{value}%");
                            var entities = Service.RetrieveMultiple(query).Entities;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Source.Where(item => entities.All(e => e.Id != item.Id)).ToList()
                                    .ForEach(item => Source.Remove(item));
                                entities.Where(e => Source.All(item => item.Id != e.Id)).ForEach(e =>
                                    Source.Add(new EntityReference(_entity, e.Id)
                                    {
                                        Name = e.GetAttributeValue<string>(_nameField)
                                    }));
                            });
                        });
                    }
                    else
                    {
                        Source.Where(item => !item.Name.Contains(value)).ToList()
                            .ForEach(item => Source.Remove(item));
                    }
                }
                Set(() => SearchString, ref _searchString, value);
            }
        }

        /// <summary>
        /// values
        /// </summary>
        public ObservableCollection<EntityReference> Source { get; } = new ObservableCollection<EntityReference>();

        /// <summary>
        /// selects the value if search wasn't performed
        /// </summary>
        /// <param name="defaultValue"></param>
        public void SetSelected(EntityReference defaultValue)
        {
            if (defaultValue == null)
            {
                return;
            }
            Source.Add(defaultValue);
            Selected = defaultValue;
        }

        /// <summary>
        /// validates the data
        /// </summary>
        /// <returns>true if item selected or text filled</returns>
        public bool IsValid() => Selected != null || !string.IsNullOrWhiteSpace(SearchString);
    }
}
