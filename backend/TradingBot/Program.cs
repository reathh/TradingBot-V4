using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Services;
using TradingBot.Application.Commands.VerifyBotBalance;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5001")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<TradingBotDbContext>(options => options.UseInMemoryDatabase("TradingBot"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddDbContext<TradingBotDbContext>(options => options.UseNpgsql(connectionString));
}

// Register TimeProvider as a singleton
builder.Services.AddSingleton(TimeProvider.System);

// Register ExchangeApiRepository as a singleton
builder.Services.AddSingleton<IExchangeApiRepository, BinanceExchangeApiRepository>();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<VerifyBotBalanceCommand>());

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
    .AddClasses(classes => classes.AssignableTo<IHostedService>().Where(c => c != typeof(BackgroundJobProcessor)))
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
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; });

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();
    
    if (!app.Environment.IsDevelopment())
    {
        dbContext.Database.Migrate();
    }
    else
    {
        app.MapOpenApi();
        
        DataSeeder.SeedDatabase(dbContext);
    }
}

// Use CORS middleware
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();