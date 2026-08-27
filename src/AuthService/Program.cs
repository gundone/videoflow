using AuthService.Abstractions;
using AuthService.Data.Dapper;
using AuthService.Data.EfCore;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using VideoFlow.Api;


var builder = WebApplication.CreateBuilder(args);

// --JWT Authentication--
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);

var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt configuration is missing");

// Generate RSA key pair (2048-bit)
var rsa = RSA.Create(2048);
var signingKey = new RsaSecurityKey(rsa);

builder.Services.AddSingleton<RsaSecurityKey>(signingKey);

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
            // AuthService validates with its OWN public key
            IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
            ClockSkew = TimeSpan.Zero
        };
    });

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

// --Data Layer--
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string is missing");


// Switch between "EfCore" and "Dapper" via appsettings.json
var dataProvider = builder.Configuration.GetValue<string>("DataProvider") ?? "EfCore";
if (dataProvider == "EfCore")
{
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseNpgsql(connectionString));

    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<IAuthService, AuthenticationService>();
}
else
{
    builder.Services.AddSingleton<IUserRepository>(new DapperUserRepository(connectionString));
    builder.Services.AddSingleton<IAuthService, AuthenticationService>();
}


// ── Application Services ─────────────────────────────────────────
builder.Services.AddSingleton<IJwtService, JwtService>();


// ── Controllers ──────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/.well-known/jwks.json", () =>
{
    var publicParams = rsa.ExportParameters(false);
    var publicKey = new RsaSecurityKey(publicParams);
    var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(publicKey);
    jwk.Alg = "RS256";
    jwk.Use = "sig";
    return Results.Json(new { keys = new[] { jwk } });
});

app.Run();