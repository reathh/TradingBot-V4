using Microsoft.EntityFrameworkCore;
using Scrutor;
using TradingBot.Data;

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

// Register all IHostedService implementations automatically
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