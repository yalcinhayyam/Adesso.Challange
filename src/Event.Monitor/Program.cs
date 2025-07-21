using Contract.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Serialization.Json;

var builder = Host.CreateApplicationBuilder(args);

// Configuration setup
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");

// Rebus configuration
builder.Services.AddRebus(configure => configure
    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Info))
    .Serialization(s => s.UseNewtonsoftJson())
    .Transport(t => t.UseRabbitMq(
        connectionString: rabbitConfig["ConnectionString"]!,
        inputQueueName: rabbitConfig["ConsumerQueueName"]!)
        .ExchangeNames(topicExchangeName: "operation-events"))
    .Options(o => o.SetNumberOfWorkers(1))
);

// Register handler
builder.Services.AddSingleton<IHandleMessages<OperationEvent>, OperationEventHandler>();

var app = builder.Build();

// Get the bus instance
var bus = app.Services.GetRequiredService<Rebus.Bus.IBus>();

Console.WriteLine("🎯 EVENT MONITOR STARTING...");
Console.WriteLine($"Queue: {rabbitConfig["ConsumerQueueName"]}");
Console.WriteLine($"Exchange: operation-events");
Console.WriteLine($"Pattern: operation.events.#");

// Subscribe to topics
await bus.Advanced.Topics.Subscribe("operation.events.#");

Console.WriteLine("\n✅ EVENT MONITOR READY - Press CTRL+C to quit\n");
Console.WriteLine(new string('=', 50));

await app.RunAsync();

public class OperationEventHandler : IHandleMessages<OperationEvent>
{
    private static int _messageCount = 0;

    public async Task Handle(OperationEvent message)
    {
        var count = Interlocked.Increment(ref _messageCount);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        
        Console.WriteLine($"\n[{timestamp}] 📨 EVENT #{count}:");
        Console.WriteLine($"├─ Operation: {message.OperationName}");
        Console.WriteLine($"├─ Status: {GetStatusEmoji(message.Status)} {message.Status}");
        Console.WriteLine($"└─ Args: [{string.Join(", ", message.Args)}]");
        
        await Task.CompletedTask;
    }

    private static string GetStatusEmoji(string status) => status.ToLowerInvariant() switch
    {
        "started" => "🚀",
        "completed" => "✅",
        "failed" => "❌",
        "notfound" => "❓",
        _ => "ℹ️"
    };
}