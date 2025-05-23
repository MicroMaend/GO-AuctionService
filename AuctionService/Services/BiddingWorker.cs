using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
using AuctionService.Repositories;
using GOCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class BiddingWorker : BackgroundService
{
    private readonly ConnectionFactory _factory;
    private readonly IAuctionRepository _repository;


    public BiddingWorker(IAuctionRepository repository)
    {
        _repository = repository;
        _factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            Port = 5672,
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