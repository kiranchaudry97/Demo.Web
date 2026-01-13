using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Demo.Web.Services;

public interface IRabbitMqService
{
    Task PublishOrderToSalesforceAsync(object orderData);
    Task PublishEntityChangeAsync(string queueName, object message);
}

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private const string QueueName = "salesforce_orders";

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            // Declare multiple queues
            DeclareQueue(QueueName);
            DeclareQueue("entity_changes");
            DeclareQueue("klant_deleted");
            DeclareQueue("boek_deleted");

            _logger.LogInformation("RabbitMQ verbinding succesvol - Alle queues aangemaakt");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"RabbitMQ verbinding mislukt: {ex.Message}. App draait in fallback modus.");
            _connection = null;
            _channel = null;
        }
    }

    private void DeclareQueue(string queueName)
    {
        _channel?.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public Task PublishOrderToSalesforceAsync(object orderData)
    {
        return PublishMessageAsync(QueueName, orderData);
    }

    public Task PublishEntityChangeAsync(string queueName, object message)
    {
        return PublishMessageAsync(queueName, message);
    }

    private Task PublishMessageAsync(string queueName, object data)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ niet beschikbaar. Bericht wordt niet verstuurd.");
            return Task.CompletedTask;
        }

        try
        {
            var message = JsonSerializer.Serialize(data);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"Bericht gepubliceerd naar RabbitMQ queue: {queueName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fout bij publiceren naar RabbitMQ queue {queueName}: {ex.Message}");
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
