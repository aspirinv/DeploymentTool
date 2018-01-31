namespace incadea.WsCrm.DeploymentTool.Utils.Models
{
    /// <summary>
    /// storage account information
    /// </summary>
    public class AzureStorageData
    {
        /// <summary>
        /// Url to blob with permissions
        /// </summary>
        public string SharedBlobUrl { get; set; }

        /// <summary>
        /// storage account name
        /// </summary>
        public string StorageName { get; set; }

        /// <summary>
        /// key
        /// </summary>
        public string StorageKey { get; set; }

        /// <summary>
        /// Connection string to the storage
        /// </summary>
        public string ConnectionString => $"DefaultEndpointsProtocol=https;AccountName={StorageName};AccountKey={StorageKey}";
    }
}
