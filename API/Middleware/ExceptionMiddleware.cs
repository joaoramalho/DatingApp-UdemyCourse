using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using API.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _snext;
        private readonly ILogger<ExceptionMiddleware> _slogger;
        private readonly IHostEnvironment _senv;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _senv = env;
            _slogger = logger;
            _snext = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _snext(context);
            }
            catch(Exception ex)
            {
                _slogger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                var response = _senv.IsDevelopment() ?
                                    new ApiException(context.Response.StatusCode, ex.Message, ex.StackTrace?.ToString()) :
                                    new ApiException(context.Response.StatusCode, "Internal Server Error");

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}