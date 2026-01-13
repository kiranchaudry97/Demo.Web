using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Demo.Web.Models;

namespace Demo.Web.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "salesforce_orders";

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ Consumer succesvol geinitialiseerd op queue: {QueueName}", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij initialiseren RabbitMQ Consumer. Consumer is uitgeschakeld.");
            _connection = null;
            _channel = null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ Consumer niet beschikbaar. Service wordt overgeslagen.");
            return;
        }

        _logger.LogInformation("RabbitMQ Consumer Service gestart. Luistert naar berichten...");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Bericht ontvangen van RabbitMQ: {Message}", message);

                var orderMessage = JsonSerializer.Deserialize<OrderMessage>(message);
                
                if (orderMessage != null)
                {
                    await ProcessOrderMessageAsync(orderMessage);
                    
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Bericht succesvol verwerkt en bevestigd: {OrderNummer}", orderMessage.OrderNummer);
                }
                else
                {
                    _logger.LogWarning("Bericht kon niet worden gedeserialiseerd");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwerken bericht. Bericht wordt niet bevestigd.");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("RabbitMQ Consumer Service gestopt");
    }

    private async Task ProcessOrderMessageAsync(OrderMessage orderMessage)
    {
        using var scope = _serviceProvider.CreateScope();
        var salesforceService = scope.ServiceProvider.GetRequiredService<ISalesforceService>();

        _logger.LogInformation("Versturen naar Salesforce: Order {OrderNummer} voor klant {KlantNaam}", 
            orderMessage.OrderNummer, orderMessage.KlantNaam);

        var salesforceId = await salesforceService.CreateOrderAsync(orderMessage);

        _logger.LogInformation("Order {OrderNummer} succesvol aangemaakt in Salesforce met ID: {SalesforceId}", 
            orderMessage.OrderNummer, salesforceId);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
