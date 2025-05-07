using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<TradingBotDbContext>(options =>
        options.UseInMemoryDatabase("TradingBot"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddDbContext<TradingBotDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Register TimeProvider as a singleton
builder.Services.AddSingleton(TimeProvider.System);

// Register ExchangeApiRepository as a singleton
builder.Services.AddSingleton<IExchangeApiRepository, BinanceExchangeApiRepository>();

// Register all services with implemented interfaces as scoped, except for IHostedService
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.Where(c => c
        .GetInterfaces()
        .All(i => i != typeof(IHostedService))))
    .AsMatchingInterface()
    .WithScopedLifetime());

// Register hosted services as IHostedService
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo<IHostedService>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();