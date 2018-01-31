namespace incadea.WsCrm.DeploymentTool.Contracts
{
    /// <summary>
    /// view that have data context
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// data context
        /// </summary>
        object DataContext { get; set; }
    }
}
