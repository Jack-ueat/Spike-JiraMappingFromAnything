using System.Text.Json;
using UEAT.JiraMappingFromAnything.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JsonMappingTransformer>();
builder.Services.AddOptions<JsonMutationOptions>().BindConfiguration("JsonMutation");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/events", (GenericEventDto dto, JsonMappingTransformer  transformer) =>
    {
        Console.Write(dto.Name);
        if (dto.Entity is JsonElement e)
        {
            var json = transformer.TryTransform(e);
            return Results.Ok(json);
        }

        return Results.NoContent();
    }).WithName("PostOrderEvent");

app.Run();

public class GenericEventDto
{
    public string Name { get; set; }
    public object Entity { get; set; }
}
