// See https://aka.ms/new-console-template for more information
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

Console.WriteLine("Hello, World!");

var jwt1 = "";
var jwt2_1 = "";
var jwt2_2 = "";

var signingKeyBase64_1 = "";
var signingKeyBase64_2 = "";

await ValidateTokenAsync(jwt1, "Test1", signingKeyBase64_1);
await ValidateTokenAsync(jwt2_1, "Test2", signingKeyBase64_2);
await ValidateTokenAsync(jwt2_2, "Test2", signingKeyBase64_2);

async Task<TokenValidationResult> ValidateTokenAsync(string token, string issuer, string signingKey)
{
    var jwtHandler = new JsonWebTokenHandler();
    var jwtToken = jwtHandler.ReadJsonWebToken(token);
    var validationResult = await jwtHandler.ValidateTokenAsync(token, new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey))
    });

    Console.WriteLine($"Token issuer: {jwtToken.Issuer}");
    Console.WriteLine($"Token validated result: {validationResult.IsValid}");
    return validationResult;
}
