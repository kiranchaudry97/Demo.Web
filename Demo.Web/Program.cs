using Demo.Web.Data;
using Demo.Web.Middleware;
using Demo.Web.Services;
using Demo.Web.Validators;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Demo.Web.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ?? Encryption Service
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// ?? RabbitMQ Services (EENVOUDIG)
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// ?? Business Services
builder.Services.AddScoped<ISapService, SapService>();
builder.Services.AddScoped<ISalesforceService, SalesforceService>();
builder.Services.AddScoped<ISalesforceMapper, SalesforceMapper>();
builder.Services.AddScoped<IMessageValidationService, MessageValidationService>();

// ? FluentValidation Validators
builder.Services.AddScoped<IValidator<OrderMessage>, OrderMessageValidator>();

// ?? RabbitMQ Consumer (alleen voor Salesforce orders)
builder.Services.AddHostedService<RabbitMqConsumerService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Bookstore API",
        Version = "v1",
        Description = "API voor boekverkoop met RabbitMQ en SAP integratie"
    });

    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key voor authenticatie (gebruik: demo-api-key-12345)"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
    
    db.Database.EnsureCreated();
    
    // ?? Seed encrypted data if database is empty
    if (!db.Klanten.Any())
    {
        db.Klanten.AddRange(
            new Klant 
            { 
                Naam = "Jan Jansen", 
                Email = encryptionService.Encrypt("jan.jansen@example.com"), 
                Telefoon = encryptionService.Encrypt("0612345678"), 
                Adres = encryptionService.Encrypt("Hoofdstraat 1, Amsterdam") 
            },
            new Klant 
            { 
                Naam = "Piet Pietersen", 
                Email = encryptionService.Encrypt("piet.pietersen@example.com"), 
                Telefoon = encryptionService.Encrypt("0687654321"), 
                Adres = encryptionService.Encrypt("Kerkstraat 25, Rotterdam") 
            },
            new Klant 
            { 
                Naam = "Test Gebruiker", 
                Email = encryptionService.Encrypt("test@test.be"), 
                Telefoon = encryptionService.Encrypt("0698765432"), 
                Adres = encryptionService.Encrypt("Teststraat 10, Utrecht") 
            },
            new Klant 
            { 
                Naam = "Sophie van der Berg", 
                Email = encryptionService.Encrypt("sophie.vandenberg@example.com"), 
                Telefoon = encryptionService.Encrypt("0612345001"), 
                Adres = encryptionService.Encrypt("Leidsestraat 45, Amsterdam") 
            },
            new Klant 
            { 
                Naam = "Lucas Hendriks", 
                Email = encryptionService.Encrypt("lucas.hendriks@example.com"), 
                Telefoon = encryptionService.Encrypt("0612345002"), 
                Adres = encryptionService.Encrypt("Marktplein 12, Utrecht") 
            },
            new Klant 
            { 
                Naam = "Emma de Vries", 
                Email = encryptionService.Encrypt("emma.devries@example.com"), 
                Telefoon = encryptionService.Encrypt("0612345003"), 
                Adres = encryptionService.Encrypt("Stationsweg 88, Rotterdam") 
            },
            new Klant 
            { 
                Naam = "Daan Bakker", 
                Email = encryptionService.Encrypt("daan.bakker@example.com"), 
                Telefoon = encryptionService.Encrypt("0612345004"), 
                Adres = encryptionService.Encrypt("Kerkstraat 23, Den Haag") 
            },
            new Klant 
            { 
                Naam = "Lisa Janssen", 
                Email = encryptionService.Encrypt("lisa.janssen@example.com"), 
                Telefoon = encryptionService.Encrypt("0612345005"), 
                Adres = encryptionService.Encrypt("Dorpsstraat 56, Eindhoven") 
            }
        );
        db.SaveChanges();
        Console.WriteLine("? Seed data added: 8 klanten with encrypted PII");
    }
}

// ?? Initialize RabbitMQ Service (eenvoudige versie)
try
{
    var rabbitMqService = app.Services.GetRequiredService<IRabbitMqService>();
    Console.WriteLine("? RabbitMQ Service initialized at startup");
}
catch (Exception ex)
{
    Console.WriteLine($"?? Warning: RabbitMQ Service initialization failed: {ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookstore API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

// ?? Open browser automatisch na succesvolle start (alleen in Development)
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            // Wacht 2 seconden zodat app volledig gestart is
            Task.Delay(2000).ContinueWith(_ =>
            {
                var urls = new[]
                {
                    "https://localhost:7000/swagger",           // Swagger API
                    "http://localhost:15672/#/connections"      // RabbitMQ Connections
                };

                foreach (var url in urls)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                        Console.WriteLine($"?? Browser opened: {url}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"?? Could not open browser for {url}: {ex.Message}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"?? Auto-open browser failed: {ex.Message}");
        }
    });
}

app.Run();

