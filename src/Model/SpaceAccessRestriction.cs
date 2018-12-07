namespace Morph.Server.Sdk.Model
{
    public enum SpaceAccessRestriction
    {
        /// <summary>
        /// anonymous accessible space
        /// </summary>
        None = 0,
        /// <summary>
        /// password protected space
        /// </summary>
        BasicPassword = 1,
        /// <summary>
        /// Windows Authentication
        /// </summary>
        WindowsAuthentication = 2,
        /// <summary>
        /// not supported auth type
        /// </summary>
        NotSupported = -1
    }


}
