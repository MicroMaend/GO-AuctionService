using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
using AuctionService.Repositories;
using GOCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NLog;

public class BiddingWorker : BackgroundService
{
    private readonly ConnectionFactory _factory;
    private readonly IAuctionRepository _repository;
    private readonly ILogger<BiddingWorker> _logger;
    private readonly IConfiguration _configuration;

    public BiddingWorker(IAuctionRepository repository, ILogger<BiddingWorker> logger, IConfiguration configuration)
    {
        _repository = repository;
        _logger = logger;
        _configuration = configuration;

        // Hent RabbitMQ konfiguration fra environment variables eller brug defaults
        var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
        var userName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ??
                      Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER") ?? "admin";
        var password = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS") ?? "admin";

        _logger.LogInformation($"RabbitMQ konfiguration: Host={hostName}, Port={port}, User={userName}");

        _factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation($"Fors�ger at forbinde til RabbitMQ (fors�g {attempt}/{maxRetries})...");

                // Test forbindelse f�rst
                await TestRabbitMQConnection();

                var connection = await _factory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync();

                _logger.LogInformation("Forbundet til RabbitMQ succesfuldt!");

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
                                _logger.LogInformation($"Updated auction {auction.Id} with new highest bid {bidding.Id}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fejl ved behandling af bidding besked");
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: "bidding",
                    autoAck: true,
                    consumer: consumer
                );

                _logger.LogInformation("BiddingWorker k�rer og lytter efter beskeder...");

                // Keep the task alive while the worker is running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }

                await channel.CloseAsync();
                await connection.CloseAsync();

                return; // Success - exit the retry loop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fejl ved forbindelse til RabbitMQ (fors�g {attempt}/{maxRetries}): {ex.Message}");

                if (attempt == maxRetries)
                {
                    _logger.LogCritical("Kunne ikke forbinde til RabbitMQ efter {MaxRetries} fors�g. Worker stopper.", maxRetries);
                    throw;
                }

                _logger.LogInformation($"Venter {retryDelay.TotalSeconds} sekunder f�r n�ste fors�g...");
                await Task.Delay(retryDelay, stoppingToken);

                // �g delay for hvert fors�g (exponential backoff)
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 1.5, 60));
            }
        }
    }

    private async Task TestRabbitMQConnection()
    {
        try
        {
            using var testConnection = await _factory.CreateConnectionAsync();
            await testConnection.CloseAsync();
            _logger.LogInformation("RabbitMQ forbindelsestest OK");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"RabbitMQ forbindelsestest fejlede: {ex.Message}");
            throw;
        }
    }
}