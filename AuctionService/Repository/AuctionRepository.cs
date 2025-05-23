using MongoDB.Driver;
using AuctionService.Repositories;
using GOCore;
// Fjern denne using, da IConfiguration ikke længere injiceres direkte her
// using Microsoft.Extensions.Configuration;

namespace AuctionService.Data
{
    public class AuctionRepository : IAuctionRepository
    {
        private readonly IMongoCollection<Auction> _auctions;

        // Konstruktoren tager nu connection string direkte
        public AuctionRepository(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("GO-AuctionServiceDB"); // Sørg for at dette er det korrekte database navn

            _auctions = database.GetCollection<Auction>("Auctions"); // Sørg for at dette er det korrekte kollektions navn
        }

        public async Task CreateAuction(Auction auction)
        {
            await _auctions.InsertOneAsync(auction);
        }

        public async Task<bool> DeleteAuction(Guid id)
        {
            var result = await _auctions.DeleteOneAsync(a => a.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> EditAuction(Auction auction)
        {
            var result = await _auctions.ReplaceOneAsync(a => a.Id == auction.Id, auction);
            return result.ModifiedCount > 0;
        }

        public async Task<List<Auction>> GetAllAuctions()
        {
            return await _auctions.Find(_ => true).ToListAsync();
        }

        public async Task<Auction> GetAuctionById(Guid id)
        {
            return await _auctions.Find(a => a.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Auction>> GetAuctionByStartTime(DateTime start)
        {
            return await _auctions.Find(a => a.AuctionStart == start).ToListAsync();
        }

        public async Task<List<Auction>> GetAuctionByEndTime(DateTime end)
        {
            return await _auctions.Find(a => a.AuctionEnd == end).ToListAsync();
        }

        public async Task<List<Auction>> GetAuctionStatus(string status)
        {
            var filter = Builders<Auction>.Filter.Eq(a => a.Status, status);
            return await _auctions.Find(filter).ToListAsync();
        }
    }
}