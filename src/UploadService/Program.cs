using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using UploadService.Services;
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
    "http://localhost:5251/.well-known/jwks.json",
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

var s3Section = builder.Configuration.GetSection(S3Options.SectionName);
builder.Services.Configure<S3Options>(s3Section);

var s3Options = s3Section.Get<S3Options>() ?? throw new InvalidOperationException("s3 configuration is missing");

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    s3Options.AccessKey,
    s3Options.SecretKey,
    new AmazonS3Config
    {
        ServiceURL = s3Options.ServiceUrl, 
        UseHttp = true, 
        ForcePathStyle = true
    }));

builder.Services.AddSingleton<IStorageService, S3StorageService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();