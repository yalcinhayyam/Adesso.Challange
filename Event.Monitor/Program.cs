using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Microsoft.Extensions.Configuration;


public record OperationEvent(string OperationName, string Status, string[] Args);

public class OperationEventHandler : IHandleMessages<OperationEvent>
{
    public Task Handle(OperationEvent message)
    {
        Console.WriteLine($"Operation: {message.OperationName}, Status: {message.Status}, Args: {string.Join(", ", message.Args)}");
        return Task.CompletedTask;
    }
}

public class Program
{
    public static Task Main(string[] args)
    {
        // Build configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Get RabbitMQ configurations
        string rabbitMqConnectionString = configuration["RabbitMQ:ConnectionString"]!;
        string consumerQueueName = configuration["RabbitMQ:ConsumerQueueName"]!;

        using var activator = new BuiltinHandlerActivator();
        activator.Register(() => new OperationEventHandler());

        var bus = Configure.With(activator)
            .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, consumerQueueName))
            .Start();

        Console.WriteLine($"Listening for OperationEvent messages on queue '{consumerQueueName}'. Press Enter to quit.");
        Console.ReadLine();

        bus.Dispose();
        return Task.CompletedTask;
    }
}