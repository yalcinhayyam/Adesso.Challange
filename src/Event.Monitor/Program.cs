using Microsoft.Extensions.Configuration;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Serialization.Json;

class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var rabbitConfig = config.GetSection("RabbitMQ");

        using (var activator = new BuiltinHandlerActivator())
        {
            activator.Register(() => new OperationEventHandler());

            var bus = Configure.With(activator)
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Info))
                .Serialization(s => s.UseNewtonsoftJson())
                .Transport(t => t.UseRabbitMq(
                    connectionString: rabbitConfig["ConnectionString"],
                    inputQueueName: rabbitConfig["ConsumerQueueName"])
                    .ExchangeNames(topicExchangeName: "operation-events"))
                .Start();

            // Subscribe to all operation events
            Console.WriteLine("Subscribing to operation events...");
            await bus.Advanced.Topics.Subscribe("operation.events.#");

            Console.WriteLine("\n=== EVENT MONITOR STARTED ===");
            Console.WriteLine($"Queue: {rabbitConfig["ConsumerQueueName"]}");
            Console.WriteLine($"Exchange: operation-events");
            Console.WriteLine($"Pattern: operation.events.#");
            Console.WriteLine("Waiting for messages... Press ENTER to quit\n");

            Console.ReadLine();

            Console.WriteLine("Shutting down...");
        }
    }
}

public class OperationEventHandler : IHandleMessages<OperationEvent>
{
    public async Task Handle(OperationEvent message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"\n[{timestamp}] 🎯 EVENT RECEIVED:");
        Console.WriteLine($"├─ Operation: {message.OperationName}");
        Console.WriteLine($"├─ Status: {message.Status}");
        Console.WriteLine($"└─ Args: [{string.Join(", ", message.Args)}]");

        await Task.CompletedTask;
    }
}