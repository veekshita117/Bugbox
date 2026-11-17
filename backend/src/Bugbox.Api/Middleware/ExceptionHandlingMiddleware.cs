using System.Net;
using System.Text.Json;
using Bugbox.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Bugbox.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException notFound => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                notFound.Message,
                null as IDictionary<string, string[]>),

            KeyNotFoundException keyNotFound => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                keyNotFound.Message,
                null as IDictionary<string, string[]>),

            BadRequestException badRequest => (
                (int)HttpStatusCode.BadRequest,
                "Bad Request",
                badRequest.Message,
                badRequest.Errors),

            ConflictException conflict => (
                (int)HttpStatusCode.Conflict,
                "Conflict",
                conflict.Message,
                null as IDictionary<string, string[]>),

            ForbiddenException forbidden => (
                (int)HttpStatusCode.Forbidden,
                "Forbidden",
                forbidden.Message,
                null as IDictionary<string, string[]>),

            UnauthorizedAccessException unauthorized => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                unauthorized.Message,
                null as IDictionary<string, string[]>),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                _env.IsDevelopment() ? exception.ToString() : "An unexpected error occurred. Please try again later.",
                null as IDictionary<string, string[]>)
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
