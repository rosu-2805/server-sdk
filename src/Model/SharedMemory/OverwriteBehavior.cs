namespace Morph.Server.Sdk.Model.SharedMemory
{
    /// <summary>
    /// What to do if value already exists
    /// </summary>
    public enum OverwriteBehavior
    {
        /// <summary>
        /// Overwrite existing value
        /// </summary>
        Overwrite,
        /// <summary>
        /// Throw an exception if value already exists
        /// </summary>
        Fail,
        /// <summary>
        /// Do nothing if value already exists
        /// </summary>
        DoNothing,
    }
}