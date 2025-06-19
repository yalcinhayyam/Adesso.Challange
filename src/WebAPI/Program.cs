using AdessoLeague.Repositories.Contexts;
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

// Add services
builder.Services.AddDbContext<AdessoLeagueDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis Configuration (uncomment to enable)
// 1. Add package: dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
// 2. Uncomment below:
// builder.Services.AddStackExchangeRedisCache(options => {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
//     options.InstanceName = "AdessoLeague_";
// });
// builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
//     builder.Configuration.GetConnectionString("Redis")));

// Elasticsearch Configuration (uncomment to enable)
// 1. Add package: dotnet add package NEST
// 2. Uncomment below:
// var elasticSettings = new ConnectionSettings(new Uri(builder.Configuration["Elasticsearch:Url"]))
//     .DefaultIndex(builder.Configuration["Elasticsearch:DefaultIndex"]);
// if (bool.Parse(builder.Configuration["Elasticsearch:EnableDebug"] ?? "false"))
//     elasticSettings.EnableDebugMode();
// builder.Services.AddSingleton<IElasticClient>(new ElasticClient(elasticSettings));

// Repository Selection (uncomment your preferred implementation)
builder.Services.AddScoped<IDrawRepository, DrawRepository>(); // Default SQL
// builder.Services.AddScoped<IDrawRepository, DrawRepositoryRedis>(); // Redis
// builder.Services.AddScoped<IDrawRepository, DrawRepositoryElasticsearch>(); // Elasticsearch
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

builder.Services.AddScoped<IDrawService, DrawService>();
builder.Services.AddScoped<IRandomProvider, RandomProvider>();
builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<DrawRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Get RabbitMQ configurations
string rabbitMqConnectionString = builder.Configuration["RabbitMQ:ConnectionString"]!;
string producerQueueName = builder.Configuration["RabbitMQ:ProducerQueueName"]!;
string operationEventRoutingKey = builder.Configuration["RabbitMQ:OperationEventRoutingKey"]!;


builder.Services.AddRebus(configure => configure
    .Logging(l => l.ColoredConsole())
    .Transport(t => t.UseRabbitMqAsOneWayClient(
        connectionString: builder.Configuration["RabbitMQ:ConnectionString"]!)
        .ExchangeNames(topicExchangeName: "operation-events"))
    .Routing(r => r.TypeBased()
        .Map<OperationEvent>("operation-events")) // Direct exchange mapping
);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Adesso World League API", Version = "v1" });
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

// DB EnsureCreated
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdessoLeagueDbContext>();
    db.Database.EnsureCreated();
}

app.MapRoutes();

app.Run();