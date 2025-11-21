// server/api/Etc/GlobalExceptionHandler.cs
using System.Net;
using Microsoft.AspNetCore.Diagnostics;

namespace api.Etc;

/// <summary>Very small exception handler to satisfy Program.cs.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken token)
    {
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = "Unhandled",
            message = exception.Message
        }, cancellationToken: token);
        return true;
    }
}