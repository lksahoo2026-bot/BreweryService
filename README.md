# BreweryAPI

## Project Overview

BreweryAPI is a .NET 8 Web API that retrieves brewery data from [Open Brewery DB](https://www.openbrewerydb.org/).

It provides API versioning, brewery search, autocomplete, sorting, distance calculation, pagination, in-memory caching, centralized exception handling, and structured logging with Serilog.

## Prerequisites

Before running the application, ensure you have installed:

- .NET 8 SDK
- Visual Studio 2022 or Visual Studio Code
- Git (if cloning the repository)
- Internet access to call Open Brewery DB

## Setup Instructions

### 1. Clone or Open the Project

Clone the repository:

```bash
git clone <repository-url>
cd <repository-directory>
```

Alternatively, open `BreweryAPI.sln` in Visual Studio 2022 or open the project folder in Visual Studio Code.

### 2. Configure the Application

Check `appsettings.json` and configure the required settings:

```json
{
  "BreweryApi": {
    "BreweryApiUrl": "https://api.openbrewerydb.org/v1/breweries",
    "DefaultPageSize": 20
  }
}
```

Ensure the configuration section and property names match the application code.

### 3. Restore and Build

Run these commands from the solution directory:

```bash
dotnet restore
dotnet build
```

### 4. Run the Application

```bash
dotnet run --project BreweryAPI/BreweryAPI.csproj
```

Use the URL displayed in the console to access the API. If Swagger is enabled, navigate to `/swagger`.

## API Usage

### API Versioning

The API exposes versioned endpoints to support future changes while maintaining backward compatibility.

| Version | Endpoint | Description |
|---|---|---|
| V1 | `GET /api/v1/brewery` | Retrieves brewery information using the V1 response contract |
| V2 | `GET /api/v2/brewery` | Retrieves brewery information using the V2 response contract |

V2 includes the `Country` field if the V2 response model and controller mapping have been implemented. The V1 response contract remains unchanged.

### Get Breweries

**Endpoint:** `GET /api/v1/brewery`

Example request:

```http
GET /api/v1/brewery
```

Example with query parameters:

```http
GET /api/v1/brewery?search=austin&sortBy=name&page=1&pageSize=100
```

### Query Parameters

| Parameter | Description | Example |
|---|---|---|
| `search` | Searches supported brewery fields, such as name and city | `search=austin` |
| `sortBy` | Specifies the supported sorting field | `sortBy=name` |
| `page` | Specifies the page number | `page=1` |
| `pageSize` | Specifies the number of results per page | `pageSize=100` |
| Latitude and longitude | Reference coordinates used for distance sorting, when supported by the query model | Use the exact parameter names defined in `BreweryQuery` |

Supported sorting options:

- `name`
- `city`
- `distance`

The default page size is configured through `BreweryApi:DefaultPageSize` when `pageSize` is omitted. The maximum supported page size is 200.

### Autocomplete

Autocomplete supports suggestions based on a partial search term, where implemented by the autocomplete endpoint.

Use the route and query parameter names defined by the autocomplete controller action. Confirm the exact endpoint in Swagger before using it in client applications.

### Example cURL Request

```bash
curl "https://localhost:<port>/api/v1/brewery?search=austin&sortBy=name&page=1&pageSize=100"
```

Replace `<port>` with the HTTPS port displayed when the application starts.

## Features

### Search

Searches brewery information using supported fields, such as brewery name and city.

### Autocomplete

Provides suggestions from partial input to help users discover matching breweries more easily.

### Sorting

Supports sorting by brewery name, city, and distance. Unsupported sorting values return `400 Bad Request` when handled by the query validation logic.

### Distance Calculation

Uses the Haversine formula to calculate the great-circle distance between two geographic coordinates.

- Uses Earth's mean radius of approximately 6,371 kilometres.
- Converts latitude and longitude from degrees to radians before applying the formula.
- Requires valid brewery coordinates and reference coordinates.
- Returns a distance in kilometres when valid coordinates are available.

### Caching

Uses `IMemoryCache` through the custom `IBreweryCache` interface, implemented by `BreweryCache`, to reduce repeated calls to the external brewery API.

- **In-memory storage:** Cached brewery data is stored in application RAM.
- **Cache expiration:** Cached data is retained for 10 minutes.
- **Application restart:** Cache entries are lost when the application or server restarts.
- **Concurrent requests:** Uses `SemaphoreSlim` to coordinate cache misses and help avoid duplicate external API calls.
- **Multiple instances:** Each application instance maintains its own independent in-memory cache.
- **Separation of concerns:** `IBreweryCache` defines the caching operations, while `BreweryCache` implements them using `IMemoryCache`.
- **Testability:** The cache abstraction allows caching behavior to be mocked in unit tests.

### Pagination

Supports page-based retrieval using `page` and `pageSize`.

- The default page size is configurable.
- Page and page-size validation helps prevent invalid requests.
- Open Brewery DB supports a maximum `per_page` value of 200.
- The application should validate page-size values before calling the external API.

## Architecture

The application follows a controller-service-client architecture, with a separate caching abstraction.

```text
HTTP Client
    |
    v
BreweryController
    |
    v
IBreweryService / BreweryService
    |
    +----> IBreweryCache / BreweryCache
    |                 |
    |                 v
    |             IMemoryCache
    |
    v
IBreweryApiClient / BreweryApiClient
    |
    v
Open Brewery DB
```

**Controller:** Handles HTTP requests, query parameters, API version routing, and HTTP responses.

**Service:** Implements brewery search, sorting, distance calculation, pagination, and cache coordination.

**Cache abstraction:** `IBreweryCache` defines the caching operations, and `BreweryCache` implements them using `IMemoryCache`.

**API client:** Communicates with Open Brewery DB and processes the external API response.

**Models:** Represent query parameters and API response data, including version-specific response models where implemented.

**Exception handling:** Handles application errors centrally using the configured exception-handling mechanism.

**Logging:** Uses Serilog for structured application logging and diagnostics.

## Configuration

Application settings are maintained in `appsettings.json` and can be overridden using environment-specific configuration or environment variables.

| Setting | Description |
|---|---|
| `BreweryApi:BreweryApiUrl` | URL used to retrieve brewery information |
| `BreweryApi:DefaultPageSize` | Default number of results returned per page |

Example environment variables:

```text
BreweryApi__BreweryApiUrl=https://api.openbrewerydb.org/v1/breweries
BreweryApi__DefaultPageSize=100
```

Required configuration should be validated during application startup.

## Logging with Serilog

Uses Serilog for structured logging and application diagnostics.

Logging helps track:

- Cache hits and misses.
- Calls to the external brewery API.
- Application errors and exceptions.
- Relevant request-processing information.

Log destinations and minimum logging levels depend on the Serilog configuration in the application.

Generated log files should not be committed to source control. If the application writes logs to a `Logs` directory, add the following entries to `.gitignore`:

```gitignore
Logs/
*.log
```

## Error Handling

The API validates user input and handles failures from the external brewery service.

| Scenario | Expected behavior |
|---|---|
| Invalid page number or page size | `400 Bad Request` with a validation message |
| Unsupported sorting field | `400 Bad Request` with a validation message |
| Missing or invalid configuration | Startup validation error |
| External API failure | Log the failure and return an appropriate error response |
| External API timeout or unavailability | Handle the exception and return an appropriate error response |
| Unexpected application exception | Centralized exception handling returns a safe error response |

Exact status codes and response formats depend on the controller and exception-handling implementation.

## Limitations

- The application depends on Open Brewery DB availability and response format.
- Cached brewery data may become stale until the 10-minute cache entry expires.
- In-memory cache data is not persistent and is not shared between application instances.
- Search is limited to the fields implemented by the service, such as name and city.
- Distance calculations require valid coordinates and reference-location parameters.
- Open Brewery DB limits `per_page` to 200.
- Live external API tests may be affected by network availability or changes in external data.

## Testing

Run all tests from the solution directory:

```bash
dotnet test
```

To run a specific test project:

```bash
dotnet test <path-to-test-project.csproj>
```

Recommended unit-test coverage includes:

- Search by name and city.
- Sorting by name, city, and distance.
- Haversine distance calculations.
- Pagination and default page size.
- Cache hits, cache misses, and concurrent requests.
- Invalid query parameters.
- V1 and V2 response contracts.
- Autocomplete behavior, if implemented.
- External API failures and exception handling.

If an integration/API test project exists, run it separately and verify endpoint routing, HTTP status codes, response structures, and error handling.

For repeatable integration tests, use a test server or mock the external API where possible rather than depending on live third-party data.

## Troubleshooting

**Configuration error:** Verify `BreweryApi:BreweryApiUrl` and `BreweryApi:DefaultPageSize`.

**External API failure:** Check internet connectivity and Open Brewery DB availability.

**No results returned:** Try a broader search term and confirm which fields the service searches.

**Unexpected cached data:** Wait for the cache entry to expire or restart the application in a local development environment.

**Build or test failure:** Run `dotnet restore`, `dotnet build`, and `dotnet test`, then review the reported errors.

## License

Add the license and attribution information applicable to this repository.