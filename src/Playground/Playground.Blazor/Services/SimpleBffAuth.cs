using FSH.Playground.Blazor.ApiClient;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FSH.Playground.Blazor.Services;

#pragma warning disable CA1515 // Extension method classes must be public
internal static class SimpleBffAuth
#pragma warning restore CA1515
{
    public static void MapSimpleBffAuthEndpoints(this WebApplication app)
    {
        // Login endpoint - calls identity API, sets cookie, returns success
        // Note: Uses /bff/ prefix to avoid conflict with ALB routing /api/* to the API service
        app.MapPost("/bff/auth/login", async (
            HttpContext httpContext,
            ITokenClient tokenClient,
            ILogger<Program> logger) =>
        {
            try
            {
                // Read form data
                var form = await httpContext.Request.ReadFormAsync();
                var email = form["Email"].ToString();
                var password = form["Password"].ToString();
                var tenant = form["Tenant"].ToString();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Login attempt for {EmailHash}", ComputeEmailHash(email));
                }

                // Call the identity API to get token
                var token = await tokenClient.IssueAsync(
                    tenant ?? "root",
                    new GenerateTokenCommand
                    {
                        Email = email,
                        Password = password
                    });

                if (token == null || string.IsNullOrEmpty(token.AccessToken))
                {
                    return Results.Unauthorized();
                }

                // Parse JWT to extract claims
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(token.AccessToken);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, jwtToken.Subject ?? Guid.NewGuid().ToString()),
                    new(ClaimTypes.Email, email),
                    new("access_token", token.AccessToken), // Store JWT for API calls
                    new("refresh_token", token.RefreshToken), // Store refresh token for token renewal
                    new("tenant", tenant ?? "root"), // Store tenant for token refresh
                };

                // Add name claim
                var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name);
                if (nameClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
                }

                // Add role claims
                var roleClaims = jwtToken.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role);
                claims.AddRange(roleClaims.Select(r => new Claim(ClaimTypes.Role, r.Value)));

                // Create identity and sign in with cookie
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Login successful for {EmailHash}", ComputeEmailHash(email));
                }

                // Redirect to home page - this ensures the cookie is properly read on the next request
                return Results.Redirect("/");
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login failed");
                return Results.Problem("Login failed");
            }
        })
        .AllowAnonymous()
        .DisableAntiforgery();

        // Logout endpoint - POST for API calls
        app.MapPost("/bff/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("Cookies");
            return Results.Ok();
        })
        .DisableAntiforgery();

        // Logout endpoint - GET for browser redirects (ensures cookie is cleared in browser)
        app.MapGet("/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("Cookies");
            return Results.Redirect("/login?toast=logout_success");
        })
        .AllowAnonymous();
    }

    private static string ComputeEmailHash(string email)
    {
        if (string.IsNullOrEmpty(email)) return "empty";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email));
        return Convert.ToHexString(hash.AsSpan(0, 4));
    }
}
