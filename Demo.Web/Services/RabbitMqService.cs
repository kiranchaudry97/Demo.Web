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
        
        var hostname = configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        var username = configuration["RabbitMQ:UserName"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        
        _logger.LogInformation($"🔌 Attempting RabbitMQ connection to {hostname}:{port} as user '{username}'");
        
        var factory = new ConnectionFactory
        {
            HostName = hostname,
            Port = port,
            UserName = username,
            Password = password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
            SocketReadTimeout = TimeSpan.FromSeconds(10),
            SocketWriteTimeout = TimeSpan.FromSeconds(10)
        };

        try
        {
            _logger.LogInformation("🔄 Creating RabbitMQ connection...");
            _connection = factory.CreateConnection("RabbitMqService-Producer");
            
            _logger.LogInformation("🔄 Creating RabbitMQ channel...");
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("🔄 Declaring queues...");
            // Declare multiple queues
            DeclareQueue(QueueName);
            DeclareQueue("entity_changes");
            DeclareQueue("klant_deleted");
            DeclareQueue("boek_deleted");

            _logger.LogInformation("✅ RabbitMQ verbinding succesvol - Alle queues aangemaakt");
            _logger.LogInformation($"✅ Connection: {_connection.IsOpen}, Channel: {_channel.IsOpen}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ RabbitMQ verbinding mislukt!");
            _logger.LogError($"❌ Error details: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                _logger.LogError($"❌ Inner exception: {ex.InnerException.Message}");
            }
            _logger.LogWarning("⚠️ App draait in fallback modus (zonder RabbitMQ)");
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
