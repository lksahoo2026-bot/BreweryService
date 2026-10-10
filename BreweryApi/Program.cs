using BreweryApi.App.Filters;
using BreweryApi.Clients;
using BreweryApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add Swagger services.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add in-memory caching.
builder.Services.AddMemoryCache();

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

// Enable Swagger.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
