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
        var connection = await _factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

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
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var bidding = JsonSerializer.Deserialize<Bidding>(message);

            if (bidding != null)
            {
                var auction = await _repository.GetAuctionById(bidding.AuctionId);
                
                if(auction.AuctionEnd < DateTime.UtcNow)
                {
                    Console.WriteLine($"Auction ID: {bidding.AuctionId} has ended.");
                    return;
                }

                if (auction != null)
                {
                    auction.HighestBidId = bidding.Id;
                    await _repository.EditAuction(auction);
                }
            }

            await Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            queue: "bidding",
            autoAck: true,
            consumer: consumer
        );

        // Keep the task alive while the worker is running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await channel.CloseAsync();
        await connection.CloseAsync();
    }
}