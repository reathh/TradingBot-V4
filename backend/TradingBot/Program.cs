using System.Text;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TradingBot.Data;
using TradingBot.Data.Interceptors;
using TradingBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Seq
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting TradingBot application");

    // Add CORS services
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy =>
            {
                policy
                    .WithOrigins("http://localhost:5001")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Important for SignalR
            });
    });

    // Add SignalR services
    builder.Services.AddSignalR();

    // Add IdentityCore and JWT authentication
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        
        // Configure cookie settings if needed
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
        .AddEntityFrameworkStores<TradingBotDbContext>()
        .AddDefaultTokenProviders();

    // Add cookie configuration
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
        .AddJwtBearer(options =>
        {
            var cfg = builder.Configuration;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = cfg["Jwt:Issuer"],
                ValidAudience = cfg["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(cfg["Jwt:Key"] ?? string.Empty)
                ),
                // Add token expiration validation
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            // Enable using JWT tokens in SignalR
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
    // Register token service
    builder.Services.AddScoped<ITokenService, TradingBot.Services.TokenService>();

    // Register the notification service as a singleton
    builder.Services.AddSingleton<TradingNotificationService>();

    // Register the slow query interceptor
    builder.Services.AddSingleton<SlowQueryInterceptor>();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Register DB context with interceptor
    builder.Services.AddDbContext<TradingBotDbContext>((serviceProvider, options) =>
    {
        options.UseNpgsql(connectionString);

        // Get the registered interceptor from the service provider
        var interceptor = serviceProvider.GetRequiredService<SlowQueryInterceptor>();
        options.AddInterceptors(interceptor);
    });

    // Register TimeProvider as a singleton
    builder.Services.AddSingleton(TimeProvider.System);

    // Register ExchangeApiRepository as a singleton
    builder.Services.AddSingleton<IExchangeApiRepository, BinanceExchangeApiRepository>();

    // Register MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

    // Register pipeline behaviors
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TradingBot.Application.Common.RequestTimingBehavior<,>));

    // Register all services with implemented interfaces as transient, except for IHostedService
    builder.Services.Scan(scan => scan
        .FromAssemblyOf<Program>()
        .AddClasses(classes => classes.Where(c => c
            .GetInterfaces()
            .All(i => i != typeof(IHostedService))))
        .AsMatchingInterface()
        .WithTransientLifetime());

    // Register hosted services as IHostedService
    builder.Services.Scan(scan => scan
        .FromAssemblyOf<Program>()
        .AddClasses(classes => classes
            .AssignableTo<IHostedService>()
            .Where(c => c != typeof(BackgroundJobProcessor)))
        .AsImplementedInterfaces()
        .WithSingletonLifetime());

    // Explicitly register BackgroundJobProcessor so that a single instance is used for
    // both IBackgroundJobProcessor and IHostedService
    builder.Services.AddSingleton<IBackgroundJobProcessor, BackgroundJobProcessor>();
    builder.Services.AddHostedService(provider => (BackgroundJobProcessor)provider.GetRequiredService<IBackgroundJobProcessor>());

    // Configure JSON serialization to handle circular references
    builder
        .Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<TradingBotDbContext>();
        // Migrate database
        dbContext.Database.Migrate();

        // Seed Identity: roles and admin user
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }
        var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@tradingbot.local";
        var adminPassword = builder.Configuration["Admin:Password"] ?? "ChangeMe123!";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            await userManager.CreateAsync(adminUser, adminPassword);
            await userManager.AddToRoleAsync(adminUser, adminRole);
        }

        // Seed application data (bots, trades) for admin if none
        if (!dbContext.Bots.Any())
        {
            // Seed a default bot owned by admin
            var bot = new Bot(
                id: 1,
                name: "BTC/USDC Trading Bot",
                publicKey: "sample-public-key",
                privateKey: "sample-private-key"
            )
            {
                Symbol = "BTCUSDC",
                Enabled = true,
                MaxPrice = 80000m,
                MinPrice = 20000m,
                EntryQuantity = 0.01m,
                EntryStep = 200m,
                ExitStep = 300m,
                IsLong = true,
                PlaceOrdersInAdvance = true,
                EntryOrdersInAdvance = 5,
                ExitOrdersInAdvance = 5,
                StartingBaseAmount = 1m,
                OwnerId = adminUser.Id
            };
            dbContext.Bots.Add(bot);
            dbContext.SaveChanges();
            // Seed trades/orders per existing logic
            DataSeeder.SeedDatabase(dbContext);
        }
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
    }

    // Use CORS middleware
    app.UseCors("AllowFrontend");
    // Enable authentication/authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHttpsRedirection();

    app.MapControllers();

    // Map SignalR hub
    app.MapHub<TradingHub>("/hubs/trading");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TradingBot application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}