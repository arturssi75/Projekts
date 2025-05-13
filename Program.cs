using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Pievienots priekš IConfiguration
using Microsoft.Extensions.DependencyInjection; // Pievienots priekš IServiceProvider
using Microsoft.Extensions.Logging; // Pievienots priekš ILogger, ILoggerFactory
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Project.Data;
using Project.Models;
using Project.Services;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// === Konfigurēt Servisus (Dependency Injection) ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Savienojuma virkne 'DefaultConnection' nav atrasta.");

// 1. Pievienojam datubāzes kontekstu (TransportContext)
builder.Services.AddDbContext<TransportContext>(options =>
    options.UseSqlite(connectionString));

// 2. Konfigurējam ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Izstrādes laikā var būt false
    options.Password.RequireDigit = false; // Simplificētas paroles izstrādei
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6; // Pielāgojiet pēc nepieciešamības
    options.Password.RequiredUniqueChars = 0;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<TransportContext>()
.AddDefaultTokenProviders();

// 3. Konfigurējam JWT autentifikāciju
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key nav konfigurēta sadaļā Jwt:Key appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer nav konfigurēts sadaļā Jwt:Issuer appsettings.json");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience nav konfigurēts sadaļā Jwt:Audience appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = builder.Environment.IsProduction(); // Produkcijā true
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Novērš laika nobīdes problēmas
    };
});

// 4. Pievienojam kontrolierus un JSON serializācijas opcijas
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Korekta enum serializācija
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // IETEIKUMS: Sākotnēji izmantojam IgnoreCycles, lai nodrošinātu, ka 'roles' lauks tiek serializēts pareizi.
        // Ja ReferenceHandler.Preserve ir nepieciešams citur, tad AccountController atbildēm
        // labāk izmantot specifiskus DTO, lai izvairītos no cikliskām atsaucēm.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; // Jūsu iepriekšējais iestatījums
    });

// 5. Reģistrējam pielāgotos servisus
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CargoService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<DispatcherService>();
builder.Services.AddScoped<MapService>();
builder.Services.AddScoped<RouteService>();
builder.Services.AddScoped<VehicleService>();

// 6. Pievienojam Swagger/OpenAPI dokumentācijas ģeneratoru
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Transporta Pārvaldības Sistēmas API",
        Version = "v1",
        Description = "API transporta un kravu pārvaldības sistēmai."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Autorizācijas galvene, izmantojot Bearer shēmu. \r\n\r\n Ievadiet 'Bearer' [atstarpe] un tad savu tokenu teksta laukā zemāk.\r\nPiemērs: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    else
    {
        Console.WriteLine($"Brīdinājums: XML Dokumentācijas fails nav atrasts ceļā {xmlPath}");
    }
});

// 7. Pievienojam CORS politiku
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", // Nosaucam politiku
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin() // Atļauj pieprasījumus no jebkura avota (izstrādei)
                   .AllowAnyMethod()       // Atļauj visas HTTP metodes
                   .AllowAnyHeader();      // Atļauj visas galvenes
        });
});

// Pievieno ILogger servisu
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

var app = builder.Build();

// === Konfigurēt HTTP pieprasījumu konveijeru (Middleware) ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transport API v1");
        // c.RoutePrefix = string.Empty; // Ja Swagger UI jābūt saknes URL
    });
    app.UseDeveloperExceptionPage(); // Detalizēti kļūdu ziņojumi izstrādes laikā
}
else
{
    app.UseExceptionHandler("/Error"); // Produkcijai jābūt Error handling lapai
    // app.UseHsts(); // Apsveriet HSTS produkcijai, ja izmantojat HTTPS
}

// app.UseHttpsRedirection(); // Ieslēdziet, ja konfigurējat HTTPS

app.UseRouting();

app.UseCors("AllowAllOrigins"); // Piemēro CORS politiku PIRMS Authentication/Authorization

app.UseAuthentication(); // Svarīga secība: vispirms autentifikācija
app.UseAuthorization();  // Tad autorizācija

app.UseDefaultFiles();   // Pasniedz index.html no wwwroot kā noklusējuma failu
app.UseStaticFiles();    // Pasniedz citus statiskos failus no wwwroot (CSS, JS, attēli)

app.MapControllers();    // Maršrutē uz API kontrolieriem

app.MapFallbackToFile("index.html"); // SPA fallback - ja neviens cits maršruts neatbilst, atgriež index.html

// === Datubāzes migrācija un datu sēklošana startēšanas laikā ===
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var startupLogger = serviceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = serviceProvider.GetRequiredService<TransportContext>();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        startupLogger.LogInformation("Mēģina piemērot datubāzes migrācijas...");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
        await dbContext.Database.MigrateAsync(); // Automātiski piemēro gaidošās migrācijas
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        startupLogger.LogInformation("Datubāzes migrācijas veiksmīgi piemērotas.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates

#pragma warning disable CA1848 // Use the LoggerMessage delegates
        startupLogger.LogInformation("Mēģina sēklot sākotnējos datus (lomas, admin lietotāju)...");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await SeedData.Initialize(serviceProvider, configuration, loggerFactory); // Izsauc datu sēklošanas metodi
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        startupLogger.LogInformation("Sākotnējā datu sēklošana pabeigta.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
    }
    catch (Exception ex)
    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        startupLogger.LogError(ex, "Kļūda lietojumprogrammas startēšanas laikā (migrācija vai sēklošana).");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
    }
}

await app.RunAsync();

// === Palīgklase datu sēklošanai (var iznest atsevišķā failā) ===
public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = loggerFactory.CreateLogger(nameof(SeedData));

        string[] roleNames = { "Admin", "Dispatcher", "Client" };
        IdentityResult roleResult;
        foreach (var roleName in roleNames)
        {
            // Pievienots .ConfigureAwait(false), lai izvairītos no potenciāliem deadlock
            if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                var newRole = new ApplicationRole(roleName)
                {
                    Description = $"Loma '{roleName}' ar attiecīgām piekļuves tiesībām."
                };
                roleResult = await roleManager.CreateAsync(newRole).ConfigureAwait(false);
                if (roleResult.Succeeded)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    logger.LogInformation("Loma '{RoleName}' veiksmīgi izveidota.", roleName);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                else
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    logger.LogError("Kļūda veidojot lomu '{RoleName}': {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
            }
            else
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                logger.LogInformation("Loma '{RoleName}' jau eksistē.", roleName);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            }
        }

        string adminEmail = configuration["DefaultAdminUser:Email"] ?? "admin@transportsystem.lv";
        string adminPassword = configuration["DefaultAdminUser:Password"] ?? "AdminPass123!";
        string adminFirstName = configuration["DefaultAdminUser:FirstName"] ?? "Admin";
        string adminLastName = configuration["DefaultAdminUser:LastName"] ?? "Admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail).ConfigureAwait(false);
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail, // Lietotājvārdam jābūt unikālam, bieži izmanto e-pastu
                Email = adminEmail,
                FirstName = adminFirstName,
                LastName = adminLastName,
                EmailConfirmed = true // Automātiski apstiprinām e-pastu sēklotam adminam
            };
            var result = await userManager.CreateAsync(user, adminPassword).ConfigureAwait(false);
            if (result.Succeeded)
            {
                adminUser = user; // Piešķiram jaunizveidoto lietotāju mainīgajam adminUser
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                logger.LogInformation("Noklusējuma Admin lietotājs '{AdminEmail}' veiksmīgi izveidots.", adminEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin").ConfigureAwait(false);
                if (addToRoleResult.Succeeded)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    logger.LogInformation("Noklusējuma Admin lietotājs '{AdminEmail}' pievienots 'Admin' lomai.", adminEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                else
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    logger.LogError("Admin lietotājs '{AdminEmail}' izveidots, bet neizdevās pievienot 'Admin' lomai: {Errors}", adminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
            }
            else
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                logger.LogError("Kļūda veidojot noklusējuma Admin lietotāju '{AdminEmail}': {Errors}", adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            }
        }
        else // Ja adminUser jau eksistē
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger.LogInformation("Noklusējuma Admin lietotājs '{AdminEmail}' jau eksistē.", adminEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            // UZLABOJUMS: Pārbaudām un piešķiram lomu, ja tā trūkst esošam admin lietotājam
            if (!await userManager.IsInRoleAsync(adminUser, "Admin").ConfigureAwait(false))
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                logger.LogWarning("Esošajam Admin lietotājam '{AdminEmail}' trūkst 'Admin' lomas. Mēģina pievienot...", adminEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin").ConfigureAwait(false);
                if (addToRoleResult.Succeeded)
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    logger.LogInformation("'Admin' loma veiksmīgi pievienota esošam Admin lietotājam '{AdminEmail}'.", adminEmail);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
                else
                {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                    logger.LogError("Neizdevās pievienot 'Admin' lomu esošam Admin lietotājam '{AdminEmail}': {Errors}", adminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                }
            }
        }
    }
}
