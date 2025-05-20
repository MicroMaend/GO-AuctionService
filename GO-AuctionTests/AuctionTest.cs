using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using AuctionService.Controllers;
using AuctionService.Repositories;
using Microsoft.AspNetCore.Mvc;
using GOCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AuctionService.Tests
{
    [TestClass]
    public class AuctionControllerTests
    {
        private Mock<IAuctionRepository> _mockRepo;
        private Mock<ILogger<AuctionController>> _mockLogger;
        private AuctionController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<IAuctionRepository>();
            _mockLogger = new Mock<ILogger<AuctionController>>();
            _controller = new AuctionController(_mockRepo.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task CreateAuction_ReturnsCreatedAtAction()
        {
            var auction = new Auction { Id = Guid.NewGuid() };
            _mockRepo.Setup(r => r.CreateAuction(auction)).Returns(Task.CompletedTask);

            var result = await _controller.CreateAuction(auction);

            var createdResult = result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual("GetAuctionById", createdResult.ActionName);
            Assert.AreEqual(auction, createdResult.Value);
        }

        [TestMethod]
        public async Task DeleteAuction_ReturnsNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeleteAuction(id)).ReturnsAsync(true);

            var result = await _controller.DeleteAuction(id);

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task EditAuction_ReturnsBadRequest_OnIdMismatch()
        {
            var routeId = Guid.NewGuid();
            var auction = new Auction { Id = Guid.NewGuid() };

            var result = await _controller.EditAuction(routeId, auction);

            var badRequest = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest);
            Assert.AreEqual("Auction ID mismatch", badRequest.Value);
        }

        [TestMethod]
        public async Task GetAuctionById_ReturnsNotFound_WhenNull()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetAuctionById(id)).ReturnsAsync((Auction)null);

            var result = await _controller.GetAuctionById(id);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task GetByStatus_ReturnsBadRequest_WhenStatusMissing()
        {
            var result = await _controller.GetByStatus(" ");

            var badRequest = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest);
            Assert.AreEqual("Status must be provided.", badRequest.Value);
        }

    }
}