# BreweryAPI

## Project Overview

BreweryAPI is a .NET 8 Web API that retrieves brewery data from [Open Brewery DB](https://www.openbrewerydb.org/). 
It provides endpoints to search, sort, calculate distances, cache, and paginate brewery information.

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

Run the following commands from the solution directory:

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
| `search` | Searches brewery names and/or cities, according to the implementation | `search=austin` |
| `sortBy` | Sorts by a supported field | `sortBy=name` |
| `page` | Specifies the page number | `page=1` |
| `pageSize` | Specifies the number of results per page | `pageSize=100` |

Supported sorting options:

- `name`
- `city`
- `distance`

The default page size is configured through `BreweryApi:DefaultPageSize` when `pageSize` is omitted.

Example cURL request:

```bash
curl "https://localhost:<port>/api/v1/brewery?search=austin&sortBy=name&page=1&pageSize=100"
```

Replace `<port>` with the HTTPS port displayed when the application starts.

**Note:** Confirm the exact query parameter names in the `BreweryQuery` model and controller. 
If distance sorting requires latitude and longitude parameters, document those parameters as well.

## Features

### Search

Searches brewery information using the supported fields, such as brewery name and city.

### Sorting

Supports sorting by brewery name, city, and distance. Unsupported sorting values should return `400 Bad Request`.

### Distance Calculation

Uses the Pythagorean formula to calculate the great-circle distance between geographic coordinates when valid coordinates and a reference location are available.

### Caching

Uses `IMemoryCache` to reduce repeated calls to the external brewery API.

- Cached data is retained for 10 minutes.
- Cache entries are lost when the application restarts.
- Concurrent cache misses should be coordinated to avoid duplicate external API calls.
- Each application instance maintains its own in-memory cache.

### Pagination

Supports page-based retrieval using `page` and `pageSize`.

- The default page size is configurable.
- Page-size validation prevents invalid requests.
- Open Brewery DB supports a maximum `per_page` value of 200.

## Architecture

The application follows a controller-service-client architecture.

```text
HTTP Client
    |
    v
BreweryController
    |
    v
IBreweryService / BreweryService
    |
    v
IBreweryApiClient / BreweryApiClient
    |
    v
Open Brewery DB
```

**Controller:** Handles HTTP requests, query parameters, and HTTP responses.

**Service:** Implements search, sorting, distance calculation, pagination, and caching.

**API Client:** Communicates with Open Brewery DB and processes the response.

**Models:** Represent query parameters and brewery response data.

**Exception Handling and Logging:** Provide consistent error handling and diagnostic information.

## Configuration

Application settings are maintained in `appsettings.json` and can be overridden using environment-specific configuration.

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

## Error Handling

The API validates user input and handles failures from the external brewery service.

| Scenario | Expected Behavior |
|---|---|
| Invalid page number or page size | `400 Bad Request` with a validation message |
| Unsupported sorting field | `400 Bad Request` with a validation message |
| Missing or invalid configuration | Clear startup validation error |
| External API returns an unsuccessful response | Log the failure and return an appropriate error response |
| External API timeout or unavailability | Handle the exception and return an appropriate error response |
| Unexpected application exception | Centralized exception handling returns a safe error response |

Exact status codes and response formats depend on the controller and exception-filter implementation.

## Limitations

- The application depends on Open Brewery DB availability and response format.
- Cached brewery data may be stale for approximately 10 minutes.
- In-memory cache data is not persistent and is not shared between application instances.
- Search is limited to `name` and `city`.
- Sorting supports only `name`, `city`, and `distance`.
- Distance calculations require valid coordinates and a supported reference location.
- Open Brewery DB limits `per_page` to 200.
- Integration tests should isolate the external API to ensure repeatable results.

## Testing

### Unit Tests

Run all tests from the solution directory:

```bash
dotnet test
```

To run a specific unit-test project:

```bash
dotnet test <path-to-unit-test-project.csproj>
```

Unit tests should cover:

- Search functionality
- Sorting by name, city, and distance
- Distance calculations
- Pagination and default page size
- Cache hits and cache misses
- Invalid query parameters
- External API errors and exceptions

Use the test framework and mocking libraries configured in the solution, such as xUnit and Moq if applicable.

### Integration/API Tests

If an integration-test project exists, execute it using:

```bash
dotnet test <path-to-integration-test-project.csproj>
```

Integration tests should verify:

- API endpoint routing
- HTTP status codes
- Query parameter validation
- Response structure
- Error handling

Use a test server or mock the external API where possible rather than depending on a live third-party service.

If an integration-test project has not been created, add one before reporting integration tests as implemented.

## Troubleshooting

**Configuration error:** Verify `BreweryApi:BreweryApiUrl` and `BreweryApi:DefaultPageSize`.

**External API failure:** Check internet connectivity and Open Brewery DB availability.

**No results returned:** Try a broader search term and confirm which fields the service searches.

**Unexpected cached data:** Wait for the cache to expire or restart the application in a local development environment.

**Build or test failure:** Run `dotnet restore`, `dotnet build`, and `dotnet test`, then review the reported errors.

## License

Add the license and attribution information applicable to this repository.
