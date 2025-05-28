using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Tilføj IConfiguration
using System.Text;
using System.Text.Json;
using AuctionService.Repositories;
using GOCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class BiddingWorker : BackgroundService
{
    private readonly ConnectionFactory _factory;
    private readonly IAuctionRepository _repository;
    private readonly ILogger<BiddingWorker> _logger;
    private readonly IConfiguration _configuration; // Tilføj IConfiguration

    public BiddingWorker(IAuctionRepository repository, ILogger<BiddingWorker> logger, IConfiguration configuration) // Tilføj IConfiguration i konstruktøren
    {
        _repository = repository;
        _logger = logger;
        _configuration = configuration;
        _factory = new ConnectionFactory
        {
            HostName = _configuration["RABBITMQ_HOST"] ?? "rabbitmq", // Brug konfiguration eller default
            Port = int.TryParse(_configuration["RABBITMQ_PORT"], out int port) ? port : 5672, // Brug konfiguration eller default
            UserName = _configuration["RABBITMQ_USER"] ?? "admin", // Brug konfiguration eller default
            Password = _configuration["RABBITMQ_PASSWORD"] ?? "admin" // Brug konfiguration eller default
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = null;
        IModel channel = null;
        bool connected = false;
        int retryDelaySeconds = 5;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!connected)
            {
                try
                {
                    _logger.LogInformation($"Forsøger at oprette RabbitMQ forbindelse med: Host={_factory.HostName}, Port={_factory.Port}, User={_factory.UserName}");
                    connection = await _factory.CreateConnectionAsync();
                    channel = await connection.CreateChannelAsync();
                    _logger.LogInformation("RabbitMQ forbindelse og kanal oprettet.");

                    await channel.QueueDeclareAsync(
                        queue: "bidding",
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (sender, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            var bidding = JsonSerializer.Deserialize<Bidding>(message);

                            if (bidding != null)
                            {
                                var auction = await _repository.GetAuctionById(bidding.AuctionId);

                                if (auction.AuctionEnd < DateTime.UtcNow)
                                {
                                    _logger.LogInformation($"Auction ID: {bidding.AuctionId} has ended.");
                                    return;
                                }

                                if (auction != null)
                                {
                                    auction.HighestBidId = bidding.Id;
                                    await _repository.EditAuction(auction);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Fejl under behandling af RabbitMQ besked.");
                        }
                    };

                    await channel.BasicConsumeAsync(
                        queue: "bidding",
                        autoAck: true,
                        consumer: consumer
                    );

                    connected = true;
                    _logger.LogInformation("BiddingWorker lytter nu til beskeder...");

                    channel.ConnectionShutdown += (sender, ea) =>
                    {
                        _logger.LogWarning($"RabbitMQ forbindelse lukket. Grund: {ea.Reason}. Forsøger at genoprette...");
                        connected = false;
                    };
                }
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogError(ex, $"RabbitMQ broker utilgængelig. Forsøger igen om {retryDelaySeconds} sekunder...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
                }
                catch (ConnectFailureException ex)
                {
                    _logger.LogError(ex, $"Fejl under oprettelse af RabbitMQ forbindelse. Forsøger igen om {retryDelaySeconds} sekunder...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Generel fejl under oprettelse af RabbitMQ forbindelse. Forsøger igen om {retryDelaySeconds} sekunder...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
                }
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        if (channel != null && channel.IsOpen)
        {
            await channel.CloseAsync();
        }
        if (connection != null && connection.IsOpen)
        {
            await connection.CloseAsync();
        }
        _logger.LogInformation("BiddingWorker stopper.");
    }
}