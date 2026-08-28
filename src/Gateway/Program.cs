using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using VideoFlow.Api;

var builder = WebApplication.CreateBuilder(args);

// ── JWT Configuration ───────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);

var jwtOptions = jwtSection.Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt configuration is missing");


// JWKS fetcher with auto-caching (default: 5 minutes, auto-refresh on error)
var docRetriever = new HttpDocumentRetriever { RequireHttps = false };
var jwksManager = new ConfigurationManager<JsonWebKeySet>(
    jwtOptions.JwksUrl,
        new JwksRetriever(),
        docRetriever);

builder.Services.AddSingleton(jwksManager);


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudiences = [jwtOptions.Audience],
            ValidateLifetime = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var jwks = jwksManager.GetConfigurationAsync()
                    .GetAwaiter().GetResult();
                return jwks.GetSigningKeys();
            },
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── CORS ─────────────────────────────────────────────────────────
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(policy =>
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()));
}


var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();