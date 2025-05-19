using AuctionService.Data;
using AuctionService.Repositories;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;

var builder = WebApplication.CreateBuilder(args);

// Fix for MongoDB Guid serialization (for latest driver versions)
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IAuctionRepository, AuctionRepository>();

// Register the background worker
builder.Services.AddHostedService<BiddingWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
await Task.Delay(5000);
app.Run();