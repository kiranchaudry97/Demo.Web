using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Demo.Web.Models;

namespace Demo.Web.Services;

public class EntityChangeConsumerService : BackgroundService
{
    private readonly ILogger<EntityChangeConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channelEntityChanges;
    private IModel? _channelKlantDeleted;
    private IModel? _channelBoekDeleted;

    public EntityChangeConsumerService(
        ILogger<EntityChangeConsumerService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var port = int.TryParse(_configuration["RabbitMQ:Port"], out var parsedPort) ? parsedPort : 5672;
            var userName = _configuration["RabbitMQ:UserName"];
            var password = _configuration["RabbitMQ:Password"];
            var virtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("RabbitMQ credentials ontbreken in configuratie. Entity Change Consumer wordt uitgeschakeld.");
                _connection = null;
                _channelEntityChanges = null;
                _channelKlantDeleted = null;
                _channelBoekDeleted = null;
                return;
            }

            _logger.LogInformation(
                "EntityChangeConsumer connecting to {HostName}:{Port} vhost '{VirtualHost}' as user '{UserName}'",
                hostName,
                port,
                virtualHost,
                userName);

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password,
                VirtualHost = virtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channelEntityChanges = _connection.CreateModel();
            _channelKlantDeleted = _connection.CreateModel();
            _channelBoekDeleted = _connection.CreateModel();

            // Declare queues
            _channelEntityChanges.QueueDeclare("entity_changes", durable: true, exclusive: false, autoDelete: false);
            _channelKlantDeleted.QueueDeclare("klant_deleted", durable: true, exclusive: false, autoDelete: false);
            _channelBoekDeleted.QueueDeclare("boek_deleted", durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("Entity Change Consumer succesvol geinitialiseerd");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij initialiseren Entity Change Consumer");
            _connection = null;
            _channelEntityChanges = null;
            _channelKlantDeleted = null;
            _channelBoekDeleted = null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection == null)
        {
            _logger.LogWarning("Entity Change Consumer niet beschikbaar");
            return;
        }

        _logger.LogInformation("Entity Change Consumer Service gestart");

        // Consumer voor entity_changes
        StartEntityChangesConsumer(stoppingToken);
        
        // Consumer voor klant_deleted
        StartKlantDeletedConsumer(stoppingToken);
        
        // Consumer voor boek_deleted
        StartBoekDeletedConsumer(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Entity Change Consumer Service gestopt");
    }

    private void StartEntityChangesConsumer(CancellationToken stoppingToken)
    {
        if (_channelEntityChanges == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channelEntityChanges);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var entityChange = JsonSerializer.Deserialize<EntityChangeMessage>(message);
                
                if (entityChange != null)
                {
                    _logger.LogInformation(
                        "Entity Change: {Action} {EntityType} '{EntityName}' (ID: {EntityId}) at {Timestamp}",
                        entityChange.Action, entityChange.EntityType, entityChange.EntityName, 
                        entityChange.EntityId, entityChange.Timestamp);

                    // Here you could:
                    // - Send to external audit system
                    // - Update search index
                    // - Notify administrators
                    // - Sync with other systems

                    _channelEntityChanges.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwerken entity change");
                _channelEntityChanges.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channelEntityChanges.BasicConsume("entity_changes", autoAck: false, consumer: consumer);
    }

    private void StartKlantDeletedConsumer(CancellationToken stoppingToken)
    {
        if (_channelKlantDeleted == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channelKlantDeleted);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var klantDeleted = JsonSerializer.Deserialize<KlantDeletedMessage>(message);
                
                if (klantDeleted != null)
                {
                    _logger.LogInformation(
                        "Klant Verwijderd: {KlantNaam} (ID: {KlantId}, Email: {Email}) - Reason: {Reason}",
                        klantDeleted.KlantNaam, klantDeleted.KlantId, klantDeleted.Email, klantDeleted.Reason);

                    // Here you could:
                    // - Archive klant data
                    // - Send to data warehouse
                    // - Notify CRM system
                    // - Update external systems (e.g., mailing list)
                    // - Create GDPR compliance report

                    await Task.Delay(100); // Simulate processing

                    _channelKlantDeleted.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwerken klant deleted event");
                _channelKlantDeleted.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channelKlantDeleted.BasicConsume("klant_deleted", autoAck: false, consumer: consumer);
    }

    private void StartBoekDeletedConsumer(CancellationToken stoppingToken)
    {
        if (_channelBoekDeleted == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channelBoekDeleted);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var boekDeleted = JsonSerializer.Deserialize<BoekDeletedMessage>(message);
                
                if (boekDeleted != null)
                {
                    _logger.LogInformation(
                        "Boek Verwijderd: {Titel} (ID: {BoekId}, ISBN: {ISBN}, Laatste Voorraad: {Voorraad}) - Reason: {Reason}",
                        boekDeleted.Titel, boekDeleted.BoekId, boekDeleted.ISBN, 
                        boekDeleted.LaatsteVoorraad, boekDeleted.Reason);

                    // Here you could:
                    // - Archive boek data
                    // - Update inventory system
                    // - Notify warehouse
                    // - Remove from search index
                    // - Update product catalog in external systems

                    await Task.Delay(100); // Simulate processing

                    _channelBoekDeleted.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwerken boek deleted event");
                _channelBoekDeleted.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channelBoekDeleted.BasicConsume("boek_deleted", autoAck: false, consumer: consumer);
    }

    public override void Dispose()
    {
        _channelEntityChanges?.Close();
        _channelEntityChanges?.Dispose();
        _channelKlantDeleted?.Close();
        _channelKlantDeleted?.Dispose();
        _channelBoekDeleted?.Close();
        _channelBoekDeleted?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
