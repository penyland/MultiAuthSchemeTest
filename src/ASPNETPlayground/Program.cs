using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "defaultScheme";
    options.DefaultAuthenticateScheme = "defaultScheme";
})
    .AddJwtBearer("Bearer1", "", options => { options.IncludeErrorDetails = true; })
    .AddJwtBearer("Bearer2", "", options => { options.IncludeErrorDetails = true; })
    .AddMultipleBearerPolicySchemes(options =>
    {
        options.IssuerSchemeMapping.Add("Test1", "Bearer1");
        options.IssuerSchemeMapping.Add("Test2", "Bearer2");
    });

builder.Services.AddAuthorization(options =>
{
    var bearer1Policy = new AuthorizationPolicyBuilder("Bearer1").RequireAuthenticatedUser().Build();
    options.AddPolicy("Bearer1", bearer1Policy);

    var bearer2Policy = new AuthorizationPolicyBuilder("Bearer2").RequireAuthenticatedUser().Build();
    options.AddPolicy("Bearer2", bearer2Policy);
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal static class Extensions
{
    public static AuthenticationBuilder AddMultipleBearerPolicySchemes(this AuthenticationBuilder builder, Action<MultipleBearerPolicySchemeOptions>? configure = null)
    {
        var options = new MultipleBearerPolicySchemeOptions();
        configure?.Invoke(options);

        return builder.AddPolicyScheme("defaultScheme", "displayName", configureOptions =>
        {
            configureOptions.ForwardDefaultSelector = context =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                {
                    var token = authorization["Bearer ".Length..].Trim();
                    var jwtHandler = new JsonWebTokenHandler();
                    if (jwtHandler.CanReadToken(token))
                    {
                        var issuer = jwtHandler.ReadJsonWebToken(token).Issuer;
                        var scheme = options?.IssuerSchemeMapping[issuer] ?? JwtBearerDefaults.AuthenticationScheme;
                        return scheme;
                    }
                }

                return JwtBearerDefaults.AuthenticationScheme;
            };
        });
    }
}

internal class MultipleBearerPolicySchemeOptions
{
    public string DefaultSelectorScheme { get; set; } = "default";

    public IDictionary<string, string> IssuerSchemeMapping { get; set; } = new Dictionary<string, string>();
}
