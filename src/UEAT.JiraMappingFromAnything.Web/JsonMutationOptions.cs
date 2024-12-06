namespace UEAT.JiraMappingFromAnything.Web;

public class EventTypeOptions
{
    public IDictionary<string, string> Mappings { get; set; }
}

public class JsonMutationOptions
{
    public IDictionary<string, EventTypeOptions> EventTypes { get; set; }
}