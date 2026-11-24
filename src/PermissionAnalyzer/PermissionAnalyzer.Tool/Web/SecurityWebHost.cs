using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace XrmMockup.PermissionAnalyzer.Tool.Web;

/// <summary>Loopback-only server. No files, credentials or live calls are exposed by the API.</summary>
internal static class SecurityWebHost
{
    private const int MaximumBodyBytes = 64 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        MaxDepth = 16
    };

    internal static async Task RunAsync(SecurityModel model, int port, ILogger logger, CancellationToken cancellationToken)
    {
        if (port is < 1 or > 65535) throw new ToolException("Web port must be between 1 and 65535.");
        var address = $"http://127.0.0.1:{port}/";
        var authority = $"127.0.0.1:{port}";
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var catalog = JsonSerializer.Serialize(model.Catalog(), JsonOptions);
        // No ambient appsettings, ASPNETCORE_URLS or Kestrel configuration may widen the binding.
        // Kestrel avoids HTTP.sys URL ACL/admin requirements on Windows.
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { Args = [], ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseKestrelCore().ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = MaximumBodyBytes;
            options.Listen(IPAddress.Loopback, port, endpoint => endpoint.Protocols = HttpProtocols.Http1);
        });
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        using var evaluation = new SemaphoreSlim(1);
        app.Run(async context =>
        {
            try { await HandleAsync(context, authority, token, catalog, model, evaluation, logger); }
            catch (Exception ex) when (ex is IOException || ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            { logger.LogDebug(ex, "Web client disconnected"); }
        });
        try { await app.StartAsync(cancellationToken); }
        catch (IOException ex) { throw new ToolException($"Cannot start web UI at {address}: {ex.Message}. Try another --port."); }
        logger.LogInformation("Security model web UI: {Address} (local only). Press Ctrl+C to stop", address);
        await app.WaitForShutdownAsync(cancellationToken);
    }

    private static async Task HandleAsync(HttpContext context, string authority, string token,
        string catalog, SecurityModel model, SemaphoreSlim evaluation, ILogger logger)
    {
        var cancellationToken = context.RequestAborted;
        var request = context.Request;
        var response = context.Response;
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
        var origin = request.Headers.Origin.ToString();
        if (!string.Equals(request.Host.Value, authority, StringComparison.OrdinalIgnoreCase)
            || origin.Length > 0 && !string.Equals(origin, $"http://{authority}", StringComparison.OrdinalIgnoreCase)
            || request.Headers["Sec-Fetch-Site"] == "cross-site")
        {
            await ErrorAsync(response, 403, "Only same-origin loopback requests are allowed.", cancellationToken);
            return;
        }
        var path = request.Path.Value;
        if (request.Method == "GET" && path is "/" or "/app.js" or "/styles.css" or "/favicon.svg")
        {
            var asset = path == "/" ? "index.html" : path[1..];
            using var stream = typeof(SecurityWebHost).Assembly.GetManifestResourceStream(
                $"XrmMockup.PermissionAnalyzer.Tool.Web.Assets.{asset}") ?? throw new InvalidOperationException($"Missing web asset {asset}");
            response.ContentType = path switch
            {
                "/" => "text/html; charset=utf-8",
                "/app.js" => "text/javascript; charset=utf-8",
                "/favicon.svg" => "image/svg+xml",
                _ => "text/css; charset=utf-8"
            };
            await stream.CopyToAsync(response.Body, cancellationToken);
            return;
        }
        if (request.Method == "GET" && path == "/api/session")
        {
            await WriteAsync(response, JsonSerializer.Serialize(new { token }, JsonOptions), cancellationToken);
            return;
        }
        if (path is not ("/api/model" or "/api/query"))
        {
            await ErrorAsync(response, 404, "Not found.", cancellationToken);
            return;
        }
        if (request.Headers["X-XrmMockup-Token"] != token)
        {
            await ErrorAsync(response, 403, "Missing or invalid session token.", cancellationToken);
            return;
        }
        if (request.Method == "GET" && path == "/api/model")
        {
            await WriteAsync(response, catalog, cancellationToken);
            return;
        }
        if (request.Method != "POST" || path != "/api/query")
        {
            response.Headers.Allow = path == "/api/model" ? "GET" : "POST";
            await ErrorAsync(response, 405, "Method not allowed.", cancellationToken);
            return;
        }
        if (!string.Equals(request.ContentType?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
        {
            await ErrorAsync(response, 415, "Use application/json.", cancellationToken);
            return;
        }
        try
        {
            if (request.ContentLength > MaximumBodyBytes) throw new QueryTooLargeException();
            using var body = new MemoryStream();
            var buffer = new byte[4096];
            int count;
            while ((count = await request.Body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (body.Length + count > MaximumBodyBytes) throw new QueryTooLargeException();
                body.Write(buffer, 0, count);
            }
            var query = JsonSerializer.Deserialize<SecurityQuery>(body.ToArray(), JsonOptions)
                ?? throw new JsonException("A query object is required.");
            await evaluation.WaitAsync(cancellationToken);
            JsonElement result;
            try { result = model.Query(query); }
            finally { evaluation.Release(); }
            await WriteAsync(response, result.GetRawText(), cancellationToken);
        }
        catch (QueryTooLargeException) { await ErrorAsync(response, 413, "Query exceeds 64 KiB.", cancellationToken); }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex) { await ErrorAsync(response, ex.StatusCode, "Invalid query request.", cancellationToken); }
        catch (JsonException) { await ErrorAsync(response, 400, "Invalid query JSON.", cancellationToken); }
        catch (ToolException ex) { await ErrorAsync(response, 400, ex.Message, cancellationToken); }
        catch (Exception ex) when (ex is not (OperationCanceledException or IOException))
        {
            logger.LogError(ex, "Security model query failed");
            await ErrorAsync(response, 500, "Query failed. Check the CLI log for details.", cancellationToken);
        }
    }

    private static Task ErrorAsync(HttpResponse response, int status, string error, CancellationToken cancellationToken)
    {
        response.StatusCode = status;
        return WriteAsync(response, JsonSerializer.Serialize(new { error }, JsonOptions), cancellationToken);
    }

    private static async Task WriteAsync(HttpResponse response, string body, CancellationToken cancellationToken)
    {
        response.ContentType = "application/json; charset=utf-8";
        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(body), cancellationToken);
    }

    private sealed class QueryTooLargeException : Exception;
}
