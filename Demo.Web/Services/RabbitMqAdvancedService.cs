using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Demo.Web.Services;

public interface IRabbitMqAdvancedService
{
    Task PublishToExchangeAsync(string exchange, string routingKey, object message, MessagePriority priority = MessagePriority.Normal);
    Task PublishOrderEventAsync(string eventType, object message);
    Task PublishEntityEventAsync(string entityType, string action, object message);
}

public enum MessagePriority
{
    Low = 1,
    Normal = 5,
    High = 10
}

public class RabbitMqAdvancedService : IRabbitMqAdvancedService, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqAdvancedService> _logger;

    // Exchange names
    private const string OrderExchange = "orders.topic";
    private const string EntityExchange = "entities.topic";
    private const string DeadLetterExchange = "dead-letter.exchange";

    // Queue names
    private const string SalesforceOrderQueue = "salesforce.orders";
    private const string SapOrderQueue = "sap.orders";
    private const string EntityChangeQueue = "entity.changes";
    private const string DeadLetterQueue = "dead-letter.queue";

    public RabbitMqAdvancedService(IConfiguration configuration, ILogger<RabbitMqAdvancedService> logger)
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

            SetupAdvancedTopology();

            _logger.LogInformation("? RabbitMQ Advanced Topology succesvol geconfigureerd");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"?? RabbitMQ verbinding mislukt: {ex.Message}");
            _connection = null;
            _channel = null;
        }
    }

    private void SetupAdvancedTopology()
    {
        if (_channel == null) return;

        // 1. Declare Dead Letter Exchange & Queue
        _channel.ExchangeDeclare(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: "dead-letter");

        // 2. Declare Orders Topic Exchange
        _channel.ExchangeDeclare(
            exchange: OrderExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // 3. Declare Salesforce Orders Queue with DLX
        var salesforceQueueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", DeadLetterExchange },
            { "x-dead-letter-routing-key", "dead-letter" },
            { "x-message-ttl", 86400000 }, // 24 hours
            { "x-max-priority", 10 } // Enable priority
        };

        _channel.QueueDeclare(
            queue: SalesforceOrderQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: salesforceQueueArgs);

        // Bind with routing patterns
        _channel.QueueBind(
            queue: SalesforceOrderQueue,
            exchange: OrderExchange,
            routingKey: "order.created");

        _channel.QueueBind(
            queue: SalesforceOrderQueue,
            exchange: OrderExchange,
            routingKey: "order.updated");

        // 4. Declare SAP Orders Queue with DLX
        var sapQueueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", DeadLetterExchange },
            { "x-dead-letter-routing-key", "dead-letter" },
            { "x-message-ttl", 86400000 },
            { "x-max-priority", 10 }
        };

        _channel.QueueDeclare(
            queue: SapOrderQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: sapQueueArgs);

        // Bind SAP queue to all order events
        _channel.QueueBind(
            queue: SapOrderQueue,
            exchange: OrderExchange,
            routingKey: "order.*");

        // 5. Declare Entity Topic Exchange
        _channel.ExchangeDeclare(
            exchange: EntityExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // 6. Declare Entity Changes Queue
        var entityQueueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", DeadLetterExchange },
            { "x-dead-letter-routing-key", "dead-letter" },
            { "x-message-ttl", 86400000 }
        };

        _channel.QueueDeclare(
            queue: EntityChangeQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: entityQueueArgs);

        // Bind to all entity events
        _channel.QueueBind(
            queue: EntityChangeQueue,
            exchange: EntityExchange,
            routingKey: "entity.*.created");

        _channel.QueueBind(
            queue: EntityChangeQueue,
            exchange: EntityExchange,
            routingKey: "entity.*.updated");

        _channel.QueueBind(
            queue: EntityChangeQueue,
            exchange: EntityExchange,
            routingKey: "entity.*.deleted");

        _logger.LogInformation("?? Exchanges: {OrderExchange}, {EntityExchange}, {DLX}", 
            OrderExchange, EntityExchange, DeadLetterExchange);
        _logger.LogInformation("?? Queues: {SalesforceQueue}, {SapQueue}, {EntityQueue}, {DLQ}", 
            SalesforceOrderQueue, SapOrderQueue, EntityChangeQueue, DeadLetterQueue);
    }

    public Task PublishToExchangeAsync(string exchange, string routingKey, object message, MessagePriority priority = MessagePriority.Normal)
    {
        if (_channel == null)
        {
            _logger.LogWarning("?? RabbitMQ niet beschikbaar. Bericht wordt niet verstuurd.");
            return Task.CompletedTask;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Priority = (byte)priority;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.AppId = "Demo.Web";
            
            // Add correlation ID for tracing
            properties.CorrelationId = Guid.NewGuid().ToString();
            
            // Add custom headers
            properties.Headers = new Dictionary<string, object>
            {
                { "published-at", DateTime.UtcNow.ToString("o") },
                { "version", "1.0" },
                { "source", "Demo.Web.API" }
            };

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("? Bericht gepubliceerd naar exchange '{Exchange}' met routing key '{RoutingKey}' (Priority: {Priority})", 
                exchange, routingKey, priority);
        }
        catch (Exception ex)
        {
            _logger.LogError("? Fout bij publiceren naar exchange '{Exchange}': {Error}", exchange, ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task PublishOrderEventAsync(string eventType, object message)
    {
        var routingKey = $"order.{eventType.ToLower()}";
        var priority = eventType.ToLower() == "created" ? MessagePriority.High : MessagePriority.Normal;
        
        return PublishToExchangeAsync(OrderExchange, routingKey, message, priority);
    }

    public Task PublishEntityEventAsync(string entityType, string action, object message)
    {
        var routingKey = $"entity.{entityType.ToLower()}.{action.ToLower()}";
        return PublishToExchangeAsync(EntityExchange, routingKey, message);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
