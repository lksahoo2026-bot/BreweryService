using Asp.Versioning;
using BreweryApi.App.Filters;
using BreweryApi.Clients;
using BreweryApi.Services;
using Microsoft.OpenApi.Models;
using Serilog;
using BreweryApi.Services.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure Serilog for console and rolling file logging.
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        // Log informational messages and higher severity levels.
        .MinimumLevel.Information()

        // Include contextual properties in log events.
        .Enrich.FromLogContext()

        // Display logs in the Visual Studio console.
        .WriteTo.Console()

        // Write logs to daily rolling files.
        // Example: Logs/brewery-api-20261012.log
        .WriteTo.File(
            "Logs/brewery-api-.log",
            rollingInterval: RollingInterval.Day);
});

//API versioning
// Configure API versioning for the application.
builder.Services
    .AddApiVersioning(options =>
    {
        // Set V1 (1.0) as the default API version.
        options.DefaultApiVersion = new ApiVersion(1, 0);

        // Use the default API version when the client
        // does not specify a version in the request.
        options.AssumeDefaultVersionWhenUnspecified = true;

        // Include supported and deprecated API version
        // information in response headers.
        options.ReportApiVersions = true;

        // Read the API version from the URL segment.
        // Example: /api/v1/brewery or /api/v2/brewery.
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    // Enable API versioning support for MVC controllers.
    .AddMvc()

    // Configure API Explorer to discover and document
    // each API version separately in Swagger.
    .AddApiExplorer(options =>
    {
        // Format version group names as v1, v2, etc.
        // These names are used to identify Swagger documents.
        options.GroupNameFormat = "'v'VVV";

        // Replace the version placeholder in the route
        // with the actual version in API descriptions.
        // Example: api/v{version:apiVersion}/brewery
        // becomes: api/v1/brewery or api/v2/brewery.
        options.SubstituteApiVersionInUrl = true;
    });

// Add Swagger services.
builder.Services.AddEndpointsApiExplorer();

// Swagger
// Configure Swagger documentation for multiple API versions.
builder.Services.AddSwaggerGen(options =>
{
    // Register the Swagger document for API Version 1.
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Brewery", // API name displayed in Swagger.
        Version = "v1"     // API version.
    });

    // Register the Swagger document for API Version 2.
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Brewery", // Keep the same API name for both versions.
        Version = "v2"     // API version.
    });

    // Include each API endpoint only in its matching Swagger document.
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Get the endpoint URL path; use an empty string if it is null.
        var path = apiDesc.RelativePath ?? "";

        // Include the endpoint only if its URL starts with the
        // corresponding version path, such as api/v1/ or api/v2/.
        // Ignore case when comparing the URL.
        return path.StartsWith(
            $"api/{docName}/",
            StringComparison.OrdinalIgnoreCase);
    });
});
// Add in-memory caching.
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IBreweryCache, BreweryCache>();

//Read DefaultPageSize from appsettings file
var defaultPageSize = builder.Configuration["DefaultPageSize"];

if (string.IsNullOrWhiteSpace(defaultPageSize))
{
    throw new InvalidOperationException(
        "DefaultPageSize configuration is missing or empty.");
}

//Read Brewery API Base Url from appsettings file
var breweryApiUrl = builder.Configuration["BreweryApi:BreweryApiUrl"];

if (string.IsNullOrWhiteSpace(breweryApiUrl))
{
    throw new InvalidOperationException("BreweryApiUrl configuration is missing or empty.");
}

// Register the external Brewery API client.
// HttpClient will use this base URL for API calls.
builder.Services.AddHttpClient<IBreweryApiClient, BreweryApiClient>(client =>
{
    client.BaseAddress = new Uri(breweryApiUrl);
});

// Register the Brewery service.
builder.Services.AddScoped<IBreweryService, BreweryService>();

// Register the exception filter
builder.Services.AddScoped<GlobalExceptionFilter>();

var app = builder.Build();

// Log HTTP requests, including method, path, status code,
// and request processing time.
app.UseSerilogRequestLogging();

// Enable Swagger.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Brewery API V1");

    options.SwaggerEndpoint(
        "/swagger/v2/swagger.json",
        "Brewery API V2");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

//Required for enabling ASP.NET Core integration tests.
public partial class Program { }
