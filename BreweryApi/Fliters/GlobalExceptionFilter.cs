using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BreweryApi.App.Fliters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Service is unavailable");

            //SqlException, HttpRequestException, TimeoutException etc.
            //instead sending actual exception message, send "Service is unavailable" to the client
            context.Result = new ObjectResult("Service is unavailable")
            {
                //explicitly sets the HTTP status code to 500
                StatusCode = StatusCodes.Status500InternalServerError

            };

            //Mark the exception as handled
            context.ExceptionHandled = true;
        }
    }
}
