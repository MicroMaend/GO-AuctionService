using Microsoft.AspNetCore.Mvc;
using AuctionService.Repositories;
using GOCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("auction")]
    public class AuctionController : ControllerBase
    {
        private readonly IAuctionRepository _repository;
        private readonly ILogger<AuctionController> _logger;

        public AuctionController(IAuctionRepository repository, ILogger<AuctionController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAuction([FromBody] Auction auction)
        {
            _logger.LogInformation("Received request to create auction with ID: {AuctionId}", auction.Id);
            await _repository.CreateAuction(auction);
            _logger.LogInformation("Auction with ID: {AuctionId} created successfully", auction.Id);
            return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, auction);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuction(Guid id)
        {
            _logger.LogInformation("Received request to delete auction with ID: {AuctionId}", id);
            var result = await _repository.DeleteAuction(id);

            if (result)
            {
                _logger.LogInformation("Auction with ID: {AuctionId} deleted successfully", id);
                return NoContent();
            }

            _logger.LogWarning("Auction with ID: {AuctionId} not found for deletion", id);
            return NotFound();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditAuction(Guid id, [FromBody] Auction auction)
        {
            _logger.LogInformation("Received request to edit auction with ID: {AuctionId}", id);

            if (id != auction.Id)
            {
                _logger.LogWarning("Auction ID mismatch: route ID = {RouteId}, body ID = {BodyId}", id, auction.Id);
                return BadRequest("Auction ID mismatch");
            }

            var result = await _repository.EditAuction(auction);

            if (result)
            {
                _logger.LogInformation("Auction with ID: {AuctionId} updated successfully", id);
                return Ok(auction);
            }

            _logger.LogWarning("Auction with ID: {AuctionId} not found for update", id);
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAuctions()
        {
            _logger.LogInformation("Received request to get all auctions");
            var auctions = await _repository.GetAllAuctions();
            _logger.LogInformation("Retrieved {Count} auctions", auctions?.Count() ?? 0);
            return Ok(auctions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuctionById(Guid id)
        {
            _logger.LogInformation("Received request to get auction with ID: {AuctionId}", id);
            var auction = await _repository.GetAuctionById(id);

            if (auction != null)
            {
                _logger.LogInformation("Auction with ID: {AuctionId} retrieved successfully", id);
                return Ok(auction);
            }

            _logger.LogWarning("Auction with ID: {AuctionId} not found", id);
            return NotFound();
        }

        [HttpGet("{id}/winner")]
        public async Task<IActionResult> GetAuctionWinner(Guid auctionId)
        {
            _logger.LogInformation("Received request to get winner for auction ID: {AuctionId}", auctionId);
            var user = await _repository.UserIdGetAuctionWinner(auctionId);
            _logger.LogInformation("Winner for auction ID: {AuctionId} retrieved successfully", auctionId);
            return Ok(user);
        }

        [HttpGet("start")]
        public async Task<IActionResult> GetByStartTime([FromQuery] DateTime start)
        {
            _logger.LogInformation("Received request to get auctions starting at: {StartTime}", start);
            var auctions = await _repository.GetAuctionByStartTime(start);
            return Ok(auctions);
        }

        [HttpGet("end")]
        public async Task<IActionResult> GetByEndTime([FromQuery] DateTime end)
        {
            _logger.LogInformation("Received request to get auctions ending at: {EndTime}", end);
            var auctions = await _repository.GetAuctionByEndTime(end);
            return Ok(auctions);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetByStatus([FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                _logger.LogWarning("Status query parameter is missing or empty");
                return BadRequest("Status must be provided.");
            }

            _logger.LogInformation("Received request to get auctions with status: {Status}", status);
            var auctions = await _repository.GetAuctionStatus(status);
            return Ok(auctions);
        }
    }
}