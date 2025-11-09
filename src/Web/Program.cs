using System.Diagnostics.Metrics;
using System.Security.Claims;
using Infrastructure;
using Infrastructure.Temporal;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Web;

var builder = WebApplication.CreateBuilder(args);

// Configure service defaults with OpenTelemetry
builder.AddServiceDefaults(
    metrics =>
    {
        metrics.AddMeter("WorkflowMetrics");
        metrics.AddMeter("Temporal.Client");
    },
    tracing =>
    {
        tracing
            .AddSource("Temporal.Client")
            .AddSource("Temporal.Workflow")
            .AddSource("Temporal.Activity");
    });

// Add services
var connectionString = builder.Configuration.GetConnectionString("fototime") ?? 
    throw new InvalidOperationException("Connection string 'fototime' not found.");

var temporalAddress = builder.Configuration["Services:temporal:tcp"] ?? 
    throw new InvalidOperationException("Temporal address not found.");

builder.Services.AddInfrastructureServices(connectionString, temporalAddress);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-fototime-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/account/signin";
        options.LogoutPath = "/account/signout";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(options =>
    {
        builder.Configuration.Bind("Authentication:Schemes:OpenIdConnect", options);
        options.Scope.Clear();

        var configuredScopes = builder.Configuration
            .GetSection("Authentication:Schemes:OpenIdConnect:Scope")
            .Get<string[]?>();

        if (configuredScopes is { Length: > 0 })
        {
            foreach (var scope in configuredScopes)
            {
                if (!string.IsNullOrWhiteSpace(scope))
                {
                    options.Scope.Add(scope);
                }
            }
        }

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-ft-af";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseSecurityHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgeryTokens();

app.MapDefaultEndpoints();

// Temporal test endpoint
app.MapGet("/ping", async (ITemporalGateway gateway, CancellationToken cancellationToken) =>
{
    try
    {
        var timestamp = await gateway.RunPingWorkflowAsync(cancellationToken);
        return Results.Ok(new PingResponse(timestamp));
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).AllowAnonymous();

app.MapGet("/", (ClaimsPrincipal user) =>
    Results.Ok(new HomeResponse($"Welcome back, {user.Identity?.Name ?? "Friend"}!")))
    .RequireAuthorization();

var accountGroup = app.MapGroup("/account");

accountGroup.MapGet("/signin", (string? returnUrl) =>
{
    var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    var properties = new AuthenticationProperties
    {
        RedirectUri = redirectUri
    };

    return Results.Challenge(properties, new[] { OpenIdConnectDefaults.AuthenticationScheme });
}).AllowAnonymous();

accountGroup.MapPost("/signout", () =>
    Results.SignOut(new AuthenticationProperties
    {
        RedirectUri = "/"
    },
    new[]
    {
        CookieAuthenticationDefaults.AuthenticationScheme,
        OpenIdConnectDefaults.AuthenticationScheme
    }))
    .RequireAuthorization();

accountGroup.MapGet("/me", (ClaimsPrincipal user) =>
    Results.Ok(new AccountProfile(
        user.Identity?.Name ?? string.Empty,
        user.FindFirstValue("sub"),
        user.FindFirstValue("email"))))
    .RequireAuthorization();

app.MapGet("/access-denied", () => Results.Problem("Access denied.", statusCode: StatusCodes.Status403Forbidden))
    .AllowAnonymous();


app.Run();

internal sealed record PingResponse(DateTime Timestamp);

internal sealed record HomeResponse(string Message);

internal sealed record AccountProfile(string Name, string? SubjectId, string? Email);

public partial class Program;
