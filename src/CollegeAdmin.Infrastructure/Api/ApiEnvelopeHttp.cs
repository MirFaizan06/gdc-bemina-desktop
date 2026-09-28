using System.Net.Http.Json;
using System.Text.Json;
using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Infrastructure.Api;

/// <summary>
/// Shared /api/v1 envelope-reading logic. Used directly by both <see cref="ApiClient"/>
/// (business calls, auto-authenticated via <see cref="AuthHeaderHandler"/>) and by
/// AuthSessionService (the auth endpoints themselves, which deliberately use their own
/// unauthenticated HttpClient — see AuthSessionService for why it can't go through ApiClient).
/// Internal: not part of this assembly's public surface, just a de-duplication seam.
/// </summary>
internal static class ApiEnvelopeHttp
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Final Convergence Phase P1-16: only GetPageAsync (the single low-level GET path every read
    // call in the app funnels through) is wrapped in retry — PostAsync (the single low-level write
    // path, including PUT/DELETE-shaped calls, since this API is POST-only with action-suffix URLs
    // like `/{id}/delete`) never is, so a destructive verb structurally cannot be auto-retried.
    private static readonly IRetryPolicy ReadRetryPolicy = new RetryPolicy();

    public static async Task<TData> GetAsync<TData>(HttpClient client, string relativeUrl, CancellationToken cancellationToken, Action<HttpRequestMessage>? configureRequest = null)
    {
        var (data, _) = await GetPageAsync<TData>(client, relativeUrl, cancellationToken, configureRequest);
        return data;
    }

    /// <summary>Same as <see cref="GetAsync{TData}"/> but also returns the envelope's `meta` object
    /// — needed by any caller that has to loop across pages (see ApiClient's FetchAllPagesAsync)
    /// rather than trust a single perPage=200 request to have returned everything.</summary>
    public static Task<(TData Data, PaginationMeta? Meta)> GetPageAsync<TData>(HttpClient client, string relativeUrl, CancellationToken cancellationToken, Action<HttpRequestMessage>? configureRequest = null) =>
        ReadRetryPolicy.ExecuteAsync(() => SendAsync<TData>(cancellationToken, () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            configureRequest?.Invoke(request);
            return client.SendAsync(request, cancellationToken);
        }), cancellationToken);

    /// <summary>For the one class of endpoint that deliberately breaks out of the JSON envelope
    /// contract (see UniversityRrController's own precedent comment on why base64-in-JSON was
    /// chosen there for a SMALL file) — a raw byte stream (e.g. a generated backup ZIP, potentially
    /// much larger than an RR export). Success still means "200 with a body"; failure still means
    /// "the server returned the normal ApiEnvelope error shape", so the error path stays consistent
    /// with every other call even though the success path isn't a TData at all.</summary>
    public static async Task<byte[]> GetBytesAsync(HttpClient client, string relativeUrl, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(relativeUrl, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiRequestException("NETWORK_ERROR", "Could not reach the server. Check your connection.", retryable: true, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiRequestException("TIMEOUT", "The request timed out.", retryable: true, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                ApiEnvelope<object>? errorEnvelope = null;
                try
                {
                    errorEnvelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(JsonOptions, cancellationToken);
                }
                catch (JsonException)
                {
                    // Fall through to the generic error below — the body wasn't a JSON envelope at all.
                }

                throw new ApiRequestException(errorEnvelope?.Error ?? new ApiErrorPayload
                {
                    Code = "UNKNOWN_ERROR",
                    Message = $"The server returned an error (HTTP {(int)response.StatusCode}) with no details.",
                });
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
    }

    public static async Task<TData> PostAsync<TData>(HttpClient client, string relativeUrl, object? body, CancellationToken cancellationToken, Action<HttpRequestMessage>? configureRequest = null)
    {
        var (data, _) = await SendAsync<TData>(cancellationToken, () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
            {
                Content = body is not null ? JsonContent.Create(body, options: JsonOptions) : null,
            };
            configureRequest?.Invoke(request);
            return client.SendAsync(request, cancellationToken);
        });
        return data;
    }

    private static async Task<(TData Data, PaginationMeta? Meta)> SendAsync<TData>(CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> send)
    {
        ApiEnvelope<TData>? envelope;
        try
        {
            // Deliberately not GetFromJsonAsync/reading only on success: the server always
            // returns a structured JSON error envelope alongside 4xx/5xx status codes (verified
            // live in Stage 1) — we need that body, not just the status.
            using var response = await send();
            envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TData>>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiRequestException("NETWORK_ERROR", "Could not reach the server. Check your connection.", retryable: true, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiRequestException("TIMEOUT", "The request timed out.", retryable: true, ex);
        }
        catch (JsonException ex)
        {
            throw new ApiRequestException("MALFORMED_RESPONSE", "The server returned an unexpected response.", retryable: false, ex);
        }

        if (envelope is null)
        {
            throw new ApiRequestException("EMPTY_RESPONSE", "The server returned an empty response.", retryable: true,
                new InvalidOperationException("Response body was null."));
        }

        if (!envelope.Success || envelope.Data is null)
        {
            throw new ApiRequestException(envelope.Error ?? new ApiErrorPayload
            {
                Code = "UNKNOWN_ERROR",
                Message = "The server reported an error with no details.",
            });
        }

        return (envelope.Data, envelope.Meta);
    }
}
