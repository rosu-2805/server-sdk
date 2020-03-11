namespace Morph.Server.Sdk.Model
{
    public abstract class LookupResultItem<T>
    {
      
        public ErrorModel Error { get; set; }
        public T Data { get; set; }
    }

}
