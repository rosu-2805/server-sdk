namespace Morph.Server.Sdk.Model
{
    public abstract class ComputationState
    {
       
        public sealed class Starting:ComputationState
        {

            public Starting()
            {
                
            }

            
        }
        
        public sealed class Running:ComputationState
        {
          
            public Running()
            {
                
            }
        }
        
        public sealed class Stopping:ComputationState
        {
            
            public Stopping()
            {
                
            }
        }
        
        
        
        
        
        public sealed class Finished:ComputationState
        {
            /// <summary>
            /// Token to get a result from the server.
            /// </summary>
            public string ResultObtainingToken { get; }
            
            public Finished(string resultObtainingToken)
            {
                ResultObtainingToken = resultObtainingToken;
                
            }
        }
    }
    // public enum ComputationStateDto
    // {
    //     Enqueued,
    //     Starting,
    //     Running,
    //     Stopping,
    //     Finished,
    //     Unknown
    // }
}