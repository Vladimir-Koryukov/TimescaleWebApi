using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TimescaleWebApi.Services;

namespace TimescaleWebApi.Middleware;

public class CsvValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public CsvValidationExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (CsvValidationException ex)
        {
            var detail = ex.LineNumber > 0 ? $"Line {ex.LineNumber}: {ex.Message}" : ex.Message;

            var problem = new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = detail
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}