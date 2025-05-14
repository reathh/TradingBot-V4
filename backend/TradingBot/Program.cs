using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Data.Interceptors;
using TradingBot.Services;
using System.Text.Json.Serialization;
using MediatR;
using Serilog;
using Serilog.Events;

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
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        dbContext.Database.Migrate();

        DataSeeder.SeedDatabase(dbContext);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
    }

    // Use CORS middleware
    app.UseCors("AllowFrontend");

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