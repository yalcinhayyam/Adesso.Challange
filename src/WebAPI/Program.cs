using AdessoLeague.Repositories.Contexts;
using Contract.Events;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Repositories.Features;
using Services.Abstraction.Repositories;
using Services.Features;
using WebAPI;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AdessoLeagueDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories & Services
builder.Services.AddScoped<IDrawRepository, DrawRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IDrawService, DrawService>();
builder.Services.AddScoped<IRandomProvider, RandomProvider>();
builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<DrawRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// RabbitMQ (Fixed Configuration)
builder.Services.AddRebus(configure => configure
    .Logging(l => l.ColoredConsole())
    .Transport(t => t.UseRabbitMqAsOneWayClient(
        connectionString: builder.Configuration["RabbitMQ:ConnectionString"]!)
        .ExchangeNames(topicExchangeName: "operation-events"))
    .Routing(r => r.TypeBased()
        .Map<OperationEvent>("operation-events"))
);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Adesso World League API", 
        Version = "v1",
        Description = "Football draw system with RabbitMQ event monitoring"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdessoLeagueDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapRoutes();

app.Run();