﻿using GOCore;

namespace AuctionService.Repositories
{
    public interface IAuctionRepository
    {
        Task CreateAuction(Auction auction);
        Task<bool> DeleteAuction(Guid id);
        Task<bool> EditAuction(Auction auction);
        Task<List<Auction>> GetAllAuctions();
        Task<Auction> GetAuctionById(Guid id);
        Task<List<Auction>> GetAuctionByStartTime(DateTime start);
        Task<List<Auction>> GetAuctionByEndTime(DateTime end);
        Task<List<Auction>> GetAuctionStatus(string status);
        Task<AuctionHouse> GetAuctionHouseById(Guid id);
        Task<List<AuctionHouse>> GetAllAuctionHouses();
    }
}
