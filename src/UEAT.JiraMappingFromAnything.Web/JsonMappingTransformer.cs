using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace UEAT.JiraMappingFromAnything.Web;

public class JsonMappingTransformer
{
    private readonly IOptions<JsonMutationOptions> _options;
    private readonly IHttpContextAccessor _contextAccessor;

    public JsonMappingTransformer(IOptions<JsonMutationOptions> options, IHttpContextAccessor contextAccessor)
    {
        _options = options;
        _contextAccessor = contextAccessor;
    }

    public JsonObject? TryTransform(JsonElement source)
    {
        if (_contextAccessor.HttpContext is not null && IsJson(_contextAccessor.HttpContext.Request))
        {
            if (TryEventType(_contextAccessor.HttpContext.Request, out var eventType) && 
                _options.Value.EventTypes.TryGetValue(eventType, out var options))
            {
                return Transform(source, options);
            }
        }

        return null;
    }

    private bool TryEventType(HttpRequest request, [NotNullWhen(true)] out string? eventType)
    {
        eventType = null;
        if (request.Headers.TryGetValue("EventType", out var eventTypes))
        {
            eventType = eventTypes.FirstOrDefault();
        }
        
        return eventType is not null;
    }

    private JsonObject Transform(JsonElement source, EventTypeOptions options)
    {
        var newJsonObject = new JsonObject();
        foreach (var mapping in options.Mappings)
        {
            if(TryGetElement(source, mapping.Value.Split('/'), out var element))
                AddJsonElement(newJsonObject, mapping.Key.Split('/'), element);
        }
        
        return newJsonObject;
    }

    private static void AddJsonElement(JsonObject newJsonObject, string[] path, [DisallowNull] JsonElement? element)
    {
        var currentJsonObject = newJsonObject;
        var i = 0;
        for (; i < path.Length - 1; i++)
        {
            if (currentJsonObject.TryGetPropertyValue(path[i], out var nested))
            {
                currentJsonObject = (JsonObject)nested!;                    
                continue;
            }
                
            var obj = new JsonObject();
            currentJsonObject.Add(path[i], obj);
            currentJsonObject = obj;
        }
            
        currentJsonObject.Add(path[i], JsonValue.Create(element));
    }

    private bool TryGetElement(JsonElement source, string[] path, [NotNullWhen(true)] out JsonElement? element)
    {
        var currentElement = source;
        
        var i = 0;
        for (; i < path.Length - 1; i++)
        {
            if (currentElement.TryGetProperty(path[i], out var nested))
            {
                currentElement = nested;
                continue;
            }

            element = null;
            return false;
        }

        if(currentElement.TryGetProperty(path[i], out var e))
        {
            element = e;
            return true;
        }

        element = null;
        return false;
    }

    /// <remarks>Copied from HttpRequestJsonExtensions.HasJsonContentType</remarks>
    private bool IsJson(HttpRequest request)
    {
        return MediaTypeHeaderValue.TryParse(request.ContentType, out var mt) &&
               (mt.MediaType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) ||
                mt.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase));
    }
}