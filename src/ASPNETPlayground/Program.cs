using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "WhichAuthDoWeUse";
    options.DefaultAuthenticateScheme = "WhichAuthDoWeUse";
})
    .AddJwtBearer("Bearer1", "", options => { options.IncludeErrorDetails = true; })
    .AddJwtBearer("Bearer2", "", options => { options.IncludeErrorDetails = true; })
    .AddPolicyScheme("WhichAuthDoWeUse", "", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            {
                var token = authorization["Bearer ".Length..].Trim();
                var jwtHandler = new JsonWebTokenHandler();

                if (jwtHandler.CanReadToken(token))
                {
                    var issuer = jwtHandler.ReadJsonWebToken(token).Issuer;
                    if (issuer == "Test2")
                    {
                        return "Bearer2";
                    }
                }
            }

            return "Bearer1";
        };
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
