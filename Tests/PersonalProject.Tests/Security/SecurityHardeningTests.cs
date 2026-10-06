using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Middleware;
using PersonalProject.Security;
using System.Text;
using Xunit;

namespace PersonalProject.Tests.Security
{
    public class
        SecurityHardeningTests
    {
        [Fact]
        public void
            WeakJwtKeyIsRejected()
        {
            var configuration =
                BuildConfiguration(
                    new Dictionary<
                        string,
                        string?
                    >
                    {
                        [
                            "Jwt:Key"
                        ] =
                            "too-short"
                    }
                );

            Assert.Throws<
                InvalidOperationException
            >(
                () =>
                    ProductionSecurityConfiguration
                        .Build(
                            configuration,
                            Environments
                                .Development
                        )
            );
        }

        [Fact]
        public void
            ProductionRejectsHttpFrontendOrigin()
        {
            var configuration =
                BuildConfiguration(
                    new Dictionary<
                        string,
                        string?
                    >
                    {
                        [
                            "Frontend:BaseUrl"
                        ] =
                            "http://philalinkmed.vercel.app"
                    }
                );

            Assert.Throws<
                InvalidOperationException
            >(
                () =>
                    ProductionSecurityConfiguration
                        .Build(
                            configuration,
                            Environments
                                .Production
                        )
            );
        }

        [Fact]
        public void
            ProductionCorsContainsOnlyConfiguredFrontendOrigin()
        {
            var configuration =
                BuildConfiguration();

            var settings =
                ProductionSecurityConfiguration
                    .Build(
                        configuration,
                        Environments
                            .Production
                    );

            Assert.Single(
                settings.AllowedOrigins
            );

            Assert.Contains(
                "https://philalinkmed.vercel.app",
                settings.AllowedOrigins
            );

            Assert.DoesNotContain(
                "http://localhost:5173",
                settings.AllowedOrigins
            );
        }

        [Fact]
        public async Task
            UnhandledExceptionDoesNotLeakSqlOrStackDetails()
        {
            var context =
                new DefaultHttpContext();

            context.Request.Path =
                "/api/security-test";

            context.Response.Body =
                new MemoryStream();

            var middleware =
                new GlobalExceptionMiddleware(
                    _ =>
                        throw new Exception(
                            "SELECT * FROM Patients; password=super-secret"
                        ),
                    NullLogger<
                        GlobalExceptionMiddleware
                    >.Instance
                );

            await middleware
                .InvokeAsync(
                    context
                );

            context.Response.Body
                .Position = 0;

            using var reader =
                new StreamReader(
                    context.Response.Body,
                    Encoding.UTF8,
                    leaveOpen: true
                );

            var body =
                await reader
                    .ReadToEndAsync();

            Assert.Equal(
                StatusCodes
                    .Status500InternalServerError,
                context.Response
                    .StatusCode
            );

            Assert.False(
                body.Contains(
                    "SELECT",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            );

            Assert.False(
                body.Contains(
                    "super-secret",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            );

            Assert.True(
                body.Contains(
                    "unexpected server error",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            );
        }

        [Fact]
        public async Task
            SecurityHeadersAreAppliedToApiResponses()
        {
            var context =
                new DefaultHttpContext();

            context.Request.Path =
                "/api/patients";

            var middleware =
                new SecurityHeadersMiddleware(
                    _ =>
                        Task.CompletedTask
                );

            await middleware
                .InvokeAsync(
                    context
                );

            Assert.Equal(
                "nosniff",
                context.Response
                    .Headers[
                        "X-Content-Type-Options"
                    ]
                    .ToString()
            );

            Assert.Equal(
                "DENY",
                context.Response
                    .Headers[
                        "X-Frame-Options"
                    ]
                    .ToString()
            );

            Assert.Contains(
                "frame-ancestors 'none'",
                context.Response
                    .Headers[
                        "Content-Security-Policy"
                    ]
                    .ToString()
            );

            Assert.Contains(
                "no-store",
                context.Response
                    .Headers[
                        "Cache-Control"
                    ]
                    .ToString()
            );
        }

        [Fact]
        public async Task
            ExternalApiHandlerRemovesSecretUrlFromNetworkFailure()
        {
            var handler =
                new SanitizedExternalApiHandler
                {
                    InnerHandler =
                        new ThrowingHttpHandler()
                };

            using var client =
                new HttpClient(
                    handler
                );

            var exception =
                await Assert.ThrowsAsync<
                    HttpRequestException
                >(
                    () =>
                        client.GetAsync(
                            "https://example.test/api?key=top-secret-key"
                        )
                );

            Assert.Equal(
                "External API request failed.",
                exception.Message
            );

            Assert.False(
                exception.Message.Contains(
                    "top-secret-key",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            );
        }

        private static IConfiguration
            BuildConfiguration(
                IDictionary<
                    string,
                    string?
                >? overrides = null
            )
        {
            var values =
                new Dictionary<
                    string,
                    string?
                >
                {
                    [
                        "ConnectionStrings:DefaultConnection"
                    ] =
                        "Host=localhost;Port=5432;Database=philalink_test;Username=test;Password=test;",

                    [
                        "Jwt:Key"
                    ] =
                        "123456789012345678901234567890123456789012345678",

                    [
                        "Jwt:Issuer"
                    ] =
                        "philalink-test-issuer",

                    [
                        "Jwt:Audience"
                    ] =
                        "philalink-test-audience",

                    [
                        "Frontend:BaseUrl"
                    ] =
                        "https://philalinkmed.vercel.app",

                    [
                        "Smtp:EnableSsl"
                    ] =
                        "true"
                };

            if (overrides != null)
            {
                foreach (
                    var item in overrides
                )
                {
                    values[
                        item.Key
                    ] =
                        item.Value;
                }
            }

            return new
                ConfigurationBuilder()
                .AddInMemoryCollection(
                    values
                )
                .Build();
        }

        private sealed class
            ThrowingHttpHandler
            : HttpMessageHandler
        {
            protected override Task<
                HttpResponseMessage
            > SendAsync(
                HttpRequestMessage request,
                CancellationToken
                    cancellationToken
            )
            {
                throw new
                    HttpRequestException(
                        "GET https://example.test/api?key=top-secret-key failed."
                    );
            }
        }
    }
}
