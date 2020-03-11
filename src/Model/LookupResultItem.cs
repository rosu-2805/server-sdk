namespace Morph.Server.Sdk.Model
{
    public abstract class LookupResultItem<T>
    {
        public string SpaceName { get; set; }
        public ErrorModel Error { get; set; }
        public T Data { get; set; }
    }

}
