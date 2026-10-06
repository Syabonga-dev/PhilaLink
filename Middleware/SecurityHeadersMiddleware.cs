namespace PersonalProject.Middleware
{
    public sealed class
        SecurityHeadersMiddleware
    {
        private readonly RequestDelegate
            _next;

        public SecurityHeadersMiddleware(
            RequestDelegate next
        )
        {
            _next =
                next;
        }

        public async Task InvokeAsync(
            HttpContext context
        )
        {
            var headers =
                context.Response.Headers;

            headers[
                "X-Content-Type-Options"
            ] =
                "nosniff";

            headers[
                "X-Frame-Options"
            ] =
                "DENY";

            headers[
                "Referrer-Policy"
            ] =
                "no-referrer";

            headers[
                "Permissions-Policy"
            ] =
                "camera=(), microphone=(), payment=(), usb=(), geolocation=()";

            /*
             * The backend serves API responses rather than
             * executable web pages. A deny-by-default CSP is
             * therefore appropriate and prevents an accidental
             * HTML response becoming an execution surface.
             */
            headers[
                "Content-Security-Policy"
            ] =
                "default-src 'none'; " +
                "base-uri 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'none';";

            headers[
                "X-Permitted-Cross-Domain-Policies"
            ] =
                "none";

            /*
             * PhilaLink API responses may contain personal and
             * medical information. Prevent browser/intermediary
             * caching by default.
             */
            if (
                context.Request.Path
                    .StartsWithSegments(
                        "/api"
                    )
            )
            {
                headers[
                    "Cache-Control"
                ] =
                    "no-store, no-cache, must-revalidate";

                headers[
                    "Pragma"
                ] =
                    "no-cache";

                headers[
                    "Expires"
                ] =
                    "0";
            }

            await _next(
                context
            );
        }
    }
}
