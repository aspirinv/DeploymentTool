using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Xml;
using incadea.WsCrm.DeploymentTool.Contracts;
using incadea.WsCrm.DeploymentTool.Utils.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// progress of CRM setup
    /// </summary>
    public class CrmImportViewModel : ProgressViewModel
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public CrmImportViewModel(WizardContext context) : base(context)
        {
        }

        #region Overrides of StepViewModel<Progress>

        /// <summary>
        /// step name
        /// </summary>
        public override string Name => "CRM setups import";

        #endregion

        #region Overrides of ProgressViewModel

        /// <summary>
        /// real work goes here
        /// </summary>
        protected override void RunInternal()
        {
            using (var service = WizardContext.CrmFactory.CreateProxyService())
            {
                SetStateMessage("Importing Solutions");
                Progress = 5;
                ImportSolutions(service, WizardContext.CrmConfig.Solutions.OrderBy(solution => solution.SequencePosition).ToList());
                Progress = 100;
                SetStateMessage(string.Empty);
            }
        }

        private void ImportSolutions(IOrganizationService service, List<SolutionConfigurationElement> solutions)
        {
            var rootFiles = Directory.GetFiles(Constants.Setups);
            var solutionsQuery = new QueryByAttribute("solution"){ ColumnSet = new ColumnSet("version", "uniquename") };
            solutionsQuery.AddAttributeValue("ismanaged", true);
            var crmSolutions = service.RetrieveMultiple(solutionsQuery).Entities
                .Select(entity => new CrmSolutionModel(entity)).ToList();
            var solutionsToDelete = new Stack<CrmSolutionModel>();
            var step = 95 / solutions.Count;
            foreach (var solution in solutions)
            {
                LogInfo($"Solution Import started for {solution.Name}");
                var installedSolution =
                    crmSolutions.FirstOrDefault(
                        item => solution.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase));
                if (solution.Mode.HasFlag(WizardContext.SolutionImportMode))
                {
                    var solutionFile = rootFiles.FirstOrDefault(file => file.Contains($"{solution.Name}_"));
                    if (string.IsNullOrWhiteSpace(solutionFile))
                    {
                        throw new Exception(
                            $"File for solution {solution.Name} was not found in the package, check package and/or config file and restart the procedure");
                    }
                    if (installedSolution != null)
                    {
                        var fileVersion = Regex.Match(Path.GetFileNameWithoutExtension(solutionFile), "_(.*)_managed")
                            .Groups[1].Value;
                        if (ValidateVersion(installedSolution.Version, fileVersion))
                        {
                            ImportSolution(service, solutionFile, solution);
                        }
                        else
                        {
                            LogInfo(
                                $"Current solution version {installedSolution.Version} more or equal to a new one. Import is skipped");
                        }
                    }
                    else
                    {
                        ImportSolution(service, solutionFile, solution);
                    }
                }
                else
                {
                    if (installedSolution != null)
                    {
                        LogInfo($"Solution {solution.Name} will be removed from CRM");
                        solutionsToDelete.Push(installedSolution);
                        continue;
                    }
                }
                Progress += step;
            }
            while (solutionsToDelete.Count > 0)
            {
                service.Delete("solution", solutionsToDelete.Pop().Id);
                Progress += step;
            }
        }

        private bool ValidateVersion(string oldVersion, string newVersion)
        {
            oldVersion = oldVersion.Replace(".", string.Empty);
            newVersion = newVersion.Replace("_", string.Empty);
            return int.Parse(oldVersion) < int.Parse(newVersion);
        }

        private void ImportSolution(IOrganizationService service, string solutionFile, SolutionConfigurationElement solution)
        {
            var jobId = Guid.NewGuid();
            try
            {
                service.Execute(new ImportSolutionRequest
                {
                    CustomizationFile = File.ReadAllBytes(solutionFile),
                    PublishWorkflows = true,
                    OverwriteUnmanagedCustomizations = false,
                    ImportJobId = jobId
                });
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                if (ex.Detail.ErrorCode == -2147188685) // ImportSolutionError
                {
                    var job = service.Retrieve("importjob", jobId, new ColumnSet("data"));
                    var dataXml = job.GetAttributeValue<string>("data");
                    var document = new XmlDocument();
                    document.LoadXml(dataXml);
                    var errortextNodes = document.SelectNodes("//result/@errortext");
                    foreach (var node in errortextNodes.Cast<XmlNode>()
                        .Where(node => !string.IsNullOrWhiteSpace(node.Value)))
                    {
                        LogError(node.Value);
                    }
                }
                throw new Exception(ex.Message);
            }
            LogInfo($"Solution {solution.Name} Imported");
        }

        #endregion
    }
}
