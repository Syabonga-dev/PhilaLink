using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PersonalProject.Middleware
{
    public sealed class
        GlobalExceptionMiddleware
    {
        private readonly RequestDelegate
            _next;

        private readonly ILogger<
            GlobalExceptionMiddleware
        > _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<
                GlobalExceptionMiddleware
            > logger
        )
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context
        )
        {
            try
            {
                await _next(
                    context
                );
            }
            catch (
                OperationCanceledException
            )
            when (
                context
                    .RequestAborted
                    .IsCancellationRequested
            )
            {
                // Client disconnected.
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(
                    context,
                    exception
                );
            }
        }

        private async Task
            HandleExceptionAsync(
                HttpContext context,
                Exception exception
            )
        {
            if (
                context.Response
                    .HasStarted
            )
            {
                throw exception;
            }

            var (
                statusCode,
                safeMessage
            ) =
                MapException(
                    exception
                );

            LogException(
                context,
                exception,
                statusCode
            );

            context.Response.Clear();

            context.Response.StatusCode =
                statusCode;

            context.Response.ContentType =
                "application/json";

            await context.Response
                .WriteAsJsonAsync(
                    new
                    {
                        message =
                            safeMessage,

                        traceId =
                            context
                                .TraceIdentifier
                    },
                    context
                        .RequestAborted
                );
        }

        private static (
            int StatusCode,
            string Message
        ) MapException(
            Exception exception
        )
        {
            return exception switch
            {
                UnauthorizedAccessException =>
                    (
                        StatusCodes
                            .Status401Unauthorized,
                        "You are not authorized to perform this request."
                    ),

                KeyNotFoundException =>
                    (
                        StatusCodes
                            .Status404NotFound,
                        "The requested resource was not found."
                    ),

                ArgumentException =>
                    (
                        StatusCodes
                            .Status400BadRequest,
                        "The request is invalid."
                    ),

                DbUpdateException =>
                    (
                        StatusCodes
                            .Status500InternalServerError,
                        "An unexpected server error occurred."
                    ),

                PostgresException =>
                    (
                        StatusCodes
                            .Status500InternalServerError,
                        "An unexpected server error occurred."
                    ),

                HttpRequestException =>
                    (
                        StatusCodes
                            .Status503ServiceUnavailable,
                        "A required external service is temporarily unavailable."
                    ),

                TimeoutException =>
                    (
                        StatusCodes
                            .Status503ServiceUnavailable,
                        "A required external service is temporarily unavailable."
                    ),

                InvalidOperationException =>
                    (
                        StatusCodes
                            .Status400BadRequest,
                        "The request could not be completed."
                    ),

                _ =>
                    (
                        StatusCodes
                            .Status500InternalServerError,
                        "An unexpected server error occurred."
                    )
            };
        }

        private void LogException(
            HttpContext context,
            Exception exception,
            int statusCode
        )
        {
            /*
             * Database exceptions may contain SQL,
             * constraints or persisted values, so
             * never log their raw exception content.
             */
            if (
                exception is
                    DbUpdateException ||
                exception is
                    PostgresException
            )
            {
                _logger.LogError(
                    "Database request failed. " +
                    "Method={Method} Path={Path} " +
                    "StatusCode={StatusCode} " +
                    "TraceId={TraceId} " +
                    "ExceptionType={ExceptionType}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    statusCode,
                    context.TraceIdentifier,
                    exception
                        .GetType()
                        .Name
                );

                return;
            }

            if (
                statusCode >=
                StatusCodes
                    .Status500InternalServerError
            )
            {
                _logger.LogError(
                    exception,
                    "Unhandled request failure. " +
                    "Method={Method} Path={Path} " +
                    "StatusCode={StatusCode} " +
                    "TraceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    statusCode,
                    context.TraceIdentifier
                );

                return;
            }

            _logger.LogWarning(
                "Request rejected. " +
                "Method={Method} Path={Path} " +
                "StatusCode={StatusCode} " +
                "TraceId={TraceId} " +
                "ExceptionType={ExceptionType}",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                context.TraceIdentifier,
                exception
                    .GetType()
                    .Name
            );
        }
    }
}
