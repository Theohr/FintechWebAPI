using FintechSampleProject.Client;
using FintechSampleProject.DataObjects;
using FintechSampleProject.DataObjects.Ado.Net;
using FintechSampleProject.Server;
using FintechSampleProject.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi.Models;
using static System.Net.WebRequestMethods;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<ITradeMonitorService, TradeMonitorService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<ITradeQueueService, TradeQueueService>();

// Add Redis to the DI container
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "TradeMonitorIssuer", 
        ValidAudience = "TradeMonitorAudience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-here-1234567890ab"))
    };
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FintechSample API",
        Version = "v1",
        Description = "API for monitoring trades and detecting suspicious activity"
    });

    // Explicitly set the OpenAPI version
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First()); // Handle any conflicting actions
    c.DocInclusionPredicate((docName, apiDesc) => true); // Include all endpoints

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, // Use Http for Bearer token
        Scheme = "bearer", // Lowercase as per OpenAPI spec
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TradeMonitor API V1");
        c.RoutePrefix = "swagger"; // Set Swagger UI at the root (e.g., https://localhost:5001/)
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<FxHub>("/fxhub");

// Start AdminClient in a background task
Task.Run(async () =>
{
    while (true)
    {
        try
        {
            var adminToken = GenerateAdminToken.GenerateToken();
            await using var adminClient = new FxClient(adminToken);
            await adminClient.StartAsync();
            await Task.Delay(Timeout.Infinite); // Keep running until the app shuts down
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Program: AdminClient stopped unexpectedly. Error: {ex.Message}");
            Console.WriteLine("Program: Restarting AdminClient in 5 seconds...");
            await Task.Delay(5000);
        }
    }
});

app.Run();
