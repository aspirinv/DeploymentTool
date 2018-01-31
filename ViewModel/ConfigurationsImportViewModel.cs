using System;
using System.IO;
using System.Linq;
using System.Text;
using CsvLoader.Core;
using CsvLoader.Core.Parsers;
using incadea.WsCrm.DeploymentTool.Contracts;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// import csv configuration view model
    /// </summary>
    public class ConfigurationsImportViewModel : ProgressViewModel
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public ConfigurationsImportViewModel(WizardContext context) : base(context)
        {
        }

        #region Overrides of StepViewModel<Progress>

        /// <summary>
        /// real work goes here
        /// </summary>
        protected override void RunInternal()
        {
            using (var service = WizardContext.CrmFactory.CreateProxyService())
            {
                var callTeam = MaterializeTeam(service, "wscrm_callcenterteamid");
                var managersTeam = MaterializeTeam(service, "wscrm_customermanagers");

                if (WizardContext.CrmSettings.GetAttributeValue<bool>("wscrm_avatarsenabled"))
                {
                    AssignRole(callTeam, new Guid(Constants.BaseAvatarRole), service);
                    AssignRole(callTeam, new Guid(Constants.CallCenterAvatarRole), service);
                    AssignRole(managersTeam, new Guid(Constants.BaseAvatarRole), service);
                }
                else
                {
                    AssignRole(callTeam, new Guid(Constants.BaseRole), service);
                    AssignRole(callTeam, new Guid(Constants.CallCenterRole), service);
                    AssignRole(managersTeam, new Guid(Constants.BaseRole), service);
                }
                if (WizardContext.CrmSettings.Id != Guid.Empty)
                {
                    service.Update(WizardContext.CrmSettings);
                    Progress = 100;
                    return;
                }
                Progress = 5;

                service.Create(WizardContext.CrmSettings);
            }

            var configurationFiles = Directory.GetFiles(Constants.ConfigurationsPath);
            if (configurationFiles.Length == 0)
            {
                Progress = 100;
                SetStateMessage(string.Empty);
                return;
            }
            Progress = 10;
            using (var service = WizardContext.CrmFactory.CreateProxyService())
            {

                var provider = new MetadataProvider(service);
                var storage = new LookupDataStorage(service, provider);
                var step = 90/configurationFiles.Length;

                foreach (var configurationFile in configurationFiles)
                {
                    LogInfo($"importing {configurationFile}");
                    var dataSource = new FileDataSource(configurationFile, Encoding.UTF8);
                    var entity = provider.GetEntity(dataSource.EntityName);
                    if (entity == null)
                    {
                        throw new Exception($"Entity {dataSource.EntityName} was not found in the system methadata");
                    }
                    dataSource.EntityName = entity.LogicalName;

                    var processor = new BulkQueryProcessor(dataSource, provider, this, storage);

                    processor.Parsers.OfType<StubParser>().ToList().ForEach(p => p.IsIgnore = true);
                    processor.Parsers.ForEach(
                        parser => parser.OnError += (sender, args) => LogError(args.ToString()));
                    processor.CreateBulk(WizardContext.CrmFactory, storage, false);
                    Progress += step;
                }
            }
            Progress = 100;
        }

        private Guid MaterializeTeam(OrganizationServiceProxy service, string field)
        {
            var user = ((WhoAmIResponse) service.Execute(new WhoAmIRequest()));
            var userReference = new EntityReference("systemuser", user.UserId);
            var callReference = WizardContext.CrmSettings.GetAttributeValue<EntityReference>(field);
            if (callReference.Id == Guid.Empty)
            {
                callReference.Id = service.Create(new Entity("team")
                {
                    ["name"] = callReference.Name,
                    ["teamtype"] = new OptionSetValue(0),
                    ["administratorid"] = userReference,
                    ["businessunitid"] = new EntityReference("businessunit", user.BusinessUnitId)
                });
            }
            return callReference.Id;
        }

        private bool HasRole(Guid roleId, Guid teamId, OrganizationServiceProxy service)
        {
            var association = "teamroles";
            var idField = "teamid";
            var query = new QueryByAttribute(association);
            query.AddAttributeValue(idField, teamId);
            query.AddAttributeValue("roleid", roleId);
            return service.RetrieveMultiple(query).Entities.Any();
        }

        private void AssignRole(Guid teamId, Guid roleId, OrganizationServiceProxy service)
        {
            if (HasRole(roleId, teamId, service))
            {
                return;
            }
            service.Associate("team", teamId,
                new Relationship("teamroles_association"),
                new EntityReferenceCollection { new EntityReference("role", roleId)});
        }

        /// <summary>
        /// step name
        /// </summary>
        public override string Name { get; } = "Crm Configurations";

        #endregion
    }
}
