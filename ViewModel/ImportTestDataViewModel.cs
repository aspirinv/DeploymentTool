using System;
using System.IO;
using System.Linq;
using System.Text;
using CsvLoader.Core;
using CsvLoader.Core.Parsers;
using incadea.WsCrm.DeploymentTool.Contracts;

namespace incadea.WsCrm.DeploymentTool.ViewModel
{
    /// <summary>
    /// test data progress
    /// </summary>
    public class ImportTestDataViewModel : ProgressViewModel
    {
        /// <summary>
        /// .cotr
        /// </summary>
        /// <param name="context">current wizard context</param>
        public ImportTestDataViewModel(WizardContext context) : base(context)
        {
        }

        #region Overrides of StepViewModel<Progress>

        /// <summary>
        /// step name
        /// </summary>
        public override string Name { get; } = "Import test data";

        #endregion

        #region Overrides of ProgressViewModel

        /// <summary>
        /// real work goes here
        /// </summary>
        protected override void RunInternal()
        {
            var testDataFiles = Directory.GetFiles(Constants.TestDataPath);
            if (testDataFiles.Length == 0)
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
                var step = 90 / testDataFiles.Length;

                foreach (var dataFile in testDataFiles)
                {
                    LogInfo($"importing {dataFile}");
                    var dataSource = new FileDataSource(dataFile, Encoding.UTF8);
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
                    var results = processor.CreateBulk(WizardContext.CrmFactory, storage, false);
                    if (results.Any(result => result.IsFaulted))
                    {
                        results.Where(result => result.IsFaulted).ForEach(item=>LogError(item.GetError()));
                        throw new Exception($"File {dataFile} failed on import, please review errors and fix configuration of the system");
                    }
                    Progress += step;
                }
            }
            Progress = 100;
        }

        #endregion
    }
}
