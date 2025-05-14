using MongoDB.Driver;
using AuctionService.Repositories;
using GOCore;

namespace AuctionService.Data
{
    public class AuctionRepository : IAuctionRepository
    {
        private readonly IMongoCollection<Auction> _auctions;

        public AuctionRepository(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDb");
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("GO-AuctionServiceDB");

            _auctions = database.GetCollection<Auction>("Auctions");
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

        public async Task<User> UserGetAuctionWinner(Guid auctionId)
        {
            var auction = await _auctions.Find(a => a.Id == auctionId).FirstOrDefaultAsync();

            if (auction == null || auction.Bids == null || !auction.Bids.Any())
                return null;

            var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            return winningBid == null ? null : new User { Id = winningBid.UserId };
        }

        public async Task<List<Auction>> GetAuctionByStartTime(DateTime start)
        {
            return await _auctions.Find(a => a.AuctionStart == start).ToListAsync();
        }

        public async Task<List<Auction>> GetAuctionByEndTime(DateTime end)
        {
            return await _auctions.Find(a => a.AuctionEnd == end).ToListAsync();
        }
    }
}
