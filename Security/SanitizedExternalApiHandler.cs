namespace PersonalProject.Security
{
    public sealed class
        SanitizedExternalApiHandler
        : DelegatingHandler
    {
        protected override async Task<
            HttpResponseMessage
        > SendAsync(
            HttpRequestMessage request,
            CancellationToken
                cancellationToken
        )
        {
            try
            {
                return await base
                    .SendAsync(
                        request,
                        cancellationToken
                    );
            }
            catch (
                HttpRequestException
            )
            {
                /*
                 * Do not preserve the provider URL in the
                 * exception message because some external APIs
                 * place their secret key in the query string.
                 */
                throw new
                    HttpRequestException(
                        "External API request failed."
                    );
            }
            catch (
                TaskCanceledException
            )
            when (
                !cancellationToken
                    .IsCancellationRequested
            )
            {
                throw new
                    TimeoutException(
                        "External API request timed out."
                    );
            }
        }
    }
}
