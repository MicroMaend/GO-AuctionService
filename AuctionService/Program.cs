using AuctionService.Data;
using AuctionService.Repositories;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using NLog;
using NLog.Web;
using VaultSharp; // Tilf�jet
using VaultSharp.V1.AuthMethods.Token; // Tilf�jet
using System.Text; // Tilf�jet
using MongoDB.Driver; // Tilf�jet
using Microsoft.AspNetCore.Authentication.JwtBearer; // Tilf�jet
using Microsoft.IdentityModel.Tokens; // Tilf�jet
using Microsoft.OpenApi.Models; // Tilf�jet
using Microsoft.Extensions.Configuration; // Tilf�jet

Console.WriteLine("AuctionService starter...");

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

var builder = WebApplication.CreateBuilder(args);

// Fix for MongoDB Guid serialization (for latest driver versions)
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Async Vault secret loader med retry
async Task<Dictionary<string, string>> LoadVaultSecretsAsync()
{
    var retryCount = 0;
    while (true)
    {
        try
        {
            var vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? "http://vault:8200";
            var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? "wopwopwop123";

            Console.WriteLine($"Henter secrets fra Vault p� {vaultAddress} med token...");

            var vaultClientSettings = new VaultClientSettings(vaultAddress, new TokenAuthMethodInfo(vaultToken));
            var vaultClient = new VaultClient(vaultClientSettings);

            var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: "go-authservice", // Brug samme sti for konsistens
                mountPoint: "secret"
            );

            Console.WriteLine("Secrets hentet fra Vault!");

            return secret.Data.Data.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.ToString() ?? ""
            );
        }
        catch (Exception ex)
        {
            retryCount++;
            if (retryCount > 5)
            {
                Console.WriteLine($"Fejl ved indl�sning af Vault secrets efter 5 fors�g: {ex.Message}");
                throw;
            }
            Console.WriteLine($"Vault ikke klar endnu, pr�ver igen om 3 sek... ({retryCount}/5): {ex.Message}");
            await Task.Delay(3000);
        }
    }
}

// Indl�s secrets fra Vault
var vaultSecrets = await LoadVaultSecretsAsync();
builder.Configuration.AddInMemoryCollection(vaultSecrets);

// Hent JWT konfiguration fra Vault
var secretKey = builder.Configuration["Jwt__Secret"];
var issuer = builder.Configuration["Jwt__Issuer"];
var audience = builder.Configuration["Jwt__Audience"];

// Hent AuctionService MongoDB connection string fra Vault
var auctionMongoConnectionString = builder.Configuration["Mongo__AuctionConnectionString"];

Console.WriteLine("Mongo Connection String (AuctionService): " + auctionMongoConnectionString);
Console.WriteLine($"Jwt__Secret fra Vault i AuctionService: '{secretKey}' (Length: {secretKey?.Length ?? 0})");
Console.WriteLine($"Jwt__Issuer fra Vault i AuctionService: '{issuer}'");
Console.WriteLine($"Jwt__Audience fra Vault i AuctionService: '{audience}'");


// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuctionService API", Version = "v1" });
    // Konfigurer Swagger for JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// Registrer AuctionRepository som singleton service ved hj�lp af en factory
builder.Services.AddSingleton<IAuctionRepository>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["Mongo__AuctionConnectionString"]; // Brug samme n�gle som ovenfor

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new Exception("MongoDB connection string for AuctionService mangler fra Vault!");
    }

    return new AuctionRepository(connectionString); // Sender connection string direkte
});

// Register the background worker (assuming it doesn't need IConfiguration directly for DB)
builder.Services.AddHostedService<BiddingWorker>();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Tilf�j autentificering
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new Exception("JWT konfiguration mangler fra Vault!");
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Tilf�j autorisering
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication(); // Tilf�jet
app.UseAuthorization();

app.MapControllers();
// await Task.Delay(5000); // Fjernet, da det normalt ikke er n�dvendigt
app.MapHealthChecks("/health");
app.Run();