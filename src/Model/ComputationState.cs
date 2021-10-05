namespace Morph.Server.Sdk.Model
{
    public abstract class ComputationState
    {
       
        public abstract bool IsRunning { get; }
        
        public sealed class Starting:ComputationState
        {

            public Starting()
            {
                
            }


            public override bool IsRunning => true;
        }
        
        public sealed class Running:ComputationState
        {
          
            public Running()
            {
                
            }
            public override bool IsRunning => true;
        }
        
        public sealed class Stopping:ComputationState
        {
            
            public Stopping()
            {
                
            }
            public override bool IsRunning => true;
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
            public override bool IsRunning => false;
        }
    }
    
}