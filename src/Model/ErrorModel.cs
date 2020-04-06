namespace Morph.Server.Sdk.Model
{
    public class ErrorModel
    {

        /// <summary>
        ///  One of a server-defined set of error codes. (required)
        /// </summary>        
        public string Code { get; set; }

        /// <summary>
        /// A human-readable representation of the error. (required)
        /// </summary>
        
        public string Message { get; set; }
        /// <summary>
        /// The target of the error.
        /// </summary>
        


    }
   

}
