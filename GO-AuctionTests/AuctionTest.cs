//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using GOCore;

//namespace GO_AuctionTests
//{
//    [TestClass]
//    public class AuctionServiceTests
//    {
//        private AuctionService _auctionService;

//        [TestInitialize]
//        public void Setup()
//        {
//            _auctionService = new AuctionService(); // evt. med mockede dependencies
//        }

//        [TestMethod]
//        public void CreateAuction_ShouldAddAuctionToList()
//        {
//            var auction = new Auction
//            {
//                Id = Guid.NewGuid(),
//                Item = new Item { Id = Guid.NewGuid(), Name = "Antik vase" },
//                IsOnline = true,
//                AuctionStart = DateTime.Now.AddHours(1),
//                AuctionEnd = DateTime.Now.AddHours(2),
//                Status = "Upcoming"
//            };

//            _auctionService.CreateAuction(auction);
//            var result = _auctionService.GetAllAuctions();

//            Assert.AreEqual(1, result.Count);
//            Assert.AreEqual("Antik vase", result.First().Item.Name);
//        }

//        [TestMethod]
//        public void DeleteAuction_ShouldRemoveAuction()
//        {
//            var auction = new Auction { Id = Guid.NewGuid() };
//            _auctionService.CreateAuction(auction);

//            _auctionService.DeleteAuction(auction.Id);
//            var result = _auctionService.GetAllAuctions();

//            Assert.AreEqual(0, result.Count);
//        }

//        [TestMethod]
//        public void EditAuction_ShouldUpdateAuction()
//        {
//            var auction = new Auction
//            {
//                Id = Guid.NewGuid(),
//                Status = "Upcoming"
//            };

//            _auctionService.CreateAuction(auction);

//            auction.Status = "Cancelled";
//            _auctionService.EditAuction(auction);

//            var updated = _auctionService.GetAuctionById(auction.Id);
//            Assert.AreEqual("Cancelled", updated.Status);
//        }

//        [TestMethod]
//        public void GetAllAuctions_ShouldReturnAllAuctions()
//        {
//            _auctionService.CreateAuction(new Auction { Id = Guid.NewGuid() });
//            _auctionService.CreateAuction(new Auction { Id = Guid.NewGuid() });

//            var all = _auctionService.GetAllAuctions();

//            Assert.AreEqual(2, all.Count);
//        }

//        [TestMethod]
//        public void GetAuctionById_ShouldReturnCorrectAuction()
//        {
//            var auction = new Auction { Id = Guid.NewGuid() };
//            _auctionService.CreateAuction(auction);

//            var result = _auctionService.GetAuctionById(auction.Id);

//            Assert.AreEqual(auction.Id, result.Id);
//        }

//        [TestMethod]
//        public void UserGetAuctionWinner_ShouldReturnHighestBidder()
//        {
//            var auction = new Auction
//            {
//                Id = Guid.NewGuid(),
//                Bids = new List<Bidding>
//                {
//                    new Bidding { Amount = 100, UserId = Guid.NewGuid() },
//                    new Bidding { Amount = 200, UserId = Guid.NewGuid() },
//                    new Bidding { Amount = 150, UserId = Guid.NewGuid() }
//                }
//            };

//            _auctionService.CreateAuction(auction);

//            var winnerId = _auctionService.UserGetAuctionWinner(auction.Id);

//            Assert.AreEqual(200, auction.Bids.First(b => b.CustomerId == winnerId).Amount);
//        }

//        [TestMethod]
//        public void GetAuctionByStartTime_ShouldReturnAuctionsStartingAtSpecificTime()
//        {
//            var startTime = new DateTime(2025, 5, 13, 10, 0, 0);
//            _auctionService.CreateAuction(new Auction { Id = Guid.NewGuid(), AuctionStart = startTime });

//            var result = _auctionService.GetAuctionByStartTime(startTime);

//            Assert.AreEqual(1, result.Count);
//            Assert.AreEqual(startTime, result[0].AuctionStart);
//        }

//        [TestMethod]
//        public void GetAuctionByEndTime_ShouldReturnAuctionsEndingAtSpecificTime()
//        {
//            var endTime = new DateTime(2025, 5, 13, 12, 0, 0);
//            _auctionService.CreateAuction(new Auction { Id = Guid.NewGuid(), AuctionEnd = endTime });

//            var result = _auctionService.GetAuctionByEndTime(endTime);

//            Assert.AreEqual(1, result.Count);
//            Assert.AreEqual(endTime, result[0].AuctionEnd);
//        }
//    }
//}
