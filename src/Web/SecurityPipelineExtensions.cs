using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Web;

internal static class SecurityPipelineExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

                return Task.CompletedTask;
            });

            return next(context);
        });
    }

    public static IApplicationBuilder UseAntiforgeryTokens(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

            if (HttpMethods.IsGet(context.Request.Method) ||
                HttpMethods.IsHead(context.Request.Method) ||
                HttpMethods.IsOptions(context.Request.Method) ||
                HttpMethods.IsTrace(context.Request.Method))
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                if (!string.IsNullOrEmpty(tokens.RequestToken))
                {
                    context.Response.Headers["X-CSRF-TOKEN"] = tokens.RequestToken;
                }
            }
            else if (HttpMethods.IsPost(context.Request.Method) ||
                     HttpMethods.IsPut(context.Request.Method) ||
                     HttpMethods.IsDelete(context.Request.Method) ||
                     HttpMethods.IsPatch(context.Request.Method))
            {
                await antiforgery.ValidateRequestAsync(context);
            }

            await next();
        });
    }
}
