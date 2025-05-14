using GOCore;

namespace AuctionService.Services
{
    public class AuctionService
    {
        private List<Auction> _auctions = new List<Auction>();

        public void CreateAuction(Auction auction)
        {
            _auctions.Add(auction);
        }

        public void DeleteAuction(Guid auctionId)
        {
            _auctions.RemoveAll(a => a.Id == auctionId);
        }

        public void EditAuction(Auction auction)
        {
            var existing = _auctions.FirstOrDefault(a => a.Id == auction.Id);
            if (existing != null)
            {
                existing.Id = auction.Id;
                existing.Status = auction.Status;
                // Update other properties as needed
            }
        }

        public List<Auction> GetAllAuctions()
        {
            return _auctions;
        }

        public Auction GetAuctionById(Guid auctionId)
        {
            return _auctions.FirstOrDefault(a => a.Id == auctionId);
        }

        public Guid UserGetAuctionWinner(Guid auctionId)
        {
            var auction = _auctions.FirstOrDefault(a => a.Id == auctionId);
            return auction?.Bids.OrderByDescending(b => b.Amount).FirstOrDefault()?.UserId ?? Guid.Empty;
        }

        public List<Auction> GetAuctionByStartTime(DateTime startTime)
        {
            return _auctions.Where(a => a.AuctionStart == startTime).ToList();
        }

        public List<Auction> GetAuctionByEndTime(DateTime endTime)
        {
            return _auctions.Where(a => a.AuctionEnd == endTime).ToList();
        }
    }
}
