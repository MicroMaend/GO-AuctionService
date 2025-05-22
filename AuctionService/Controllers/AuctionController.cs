using Microsoft.AspNetCore.Mvc;
using AuctionService.Repositories;
using GOCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("auction")]
    [Authorize] // Kræver autentificering for alle endpoints i denne controller som standard
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAuction([FromBody] Auction auction)
        {
            _logger.LogInformation("Received request to create auction");

            if (auction == null)
            {
                _logger.LogError("CreateAuction received null auction object");
                return BadRequest("Auction object cannot be null");
            }

            await _repository.CreateAuction(auction);
            _logger.LogInformation("Auction with ID: {AuctionId} created successfully", auction.Id);
            return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, auction);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeleteAuction(Guid id)
        {
            _logger.LogInformation("Received request to delete auction with ID: {AuctionId}", id);

            if (id == Guid.Empty)
            {
                _logger.LogError("DeleteAuction received empty Guid");
                return BadRequest("Auction ID cannot be empty");
            }

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
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> EditAuction(Guid id, [FromBody] Auction auction)
        {
            _logger.LogInformation("Received request to edit auction with ID: {AuctionId}", id);

            if (auction == null)
            {
                _logger.LogError("EditAuction received null auction object");
                return BadRequest("Auction object cannot be null");
            }

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
        [AllowAnonymous]
        public async Task<IActionResult> GetAllAuctions()
        {
            _logger.LogInformation("Received request to get all auctions");
            var auctions = await _repository.GetAllAuctions();
            _logger.LogInformation("Retrieved {Count} auctions", auctions?.Count() ?? 0);
            return Ok(auctions);
        }

        [HttpGet("{id}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetAuctionById(Guid id)
        {
            _logger.LogInformation("Received request to get auction with ID: {AuctionId}", id);

            if (id == Guid.Empty)
            {
                _logger.LogError("GetAuctionById received empty Guid");
                return BadRequest("Auction ID cannot be empty");
            }

            var auction = await _repository.GetAuctionById(id);

            if (auction != null)
            {
                _logger.LogInformation("Auction with ID: {AuctionId} retrieved successfully", id);
                return Ok(auction);
            }

            _logger.LogWarning("Auction with ID: {AuctionId} not found", id);
            return NotFound();
        }

        [HttpGet("{auctionId}/winner")]
        [AllowAnonymous] // Tillader uautoriserede forespørgsler for at se vinderen
        public async Task<IActionResult> GetAuctionWinner(Guid auctionId)
        {
            _logger.LogInformation("Received request to get winner for auction ID: {AuctionId}", auctionId);

            if (auctionId == Guid.Empty)
            {
                _logger.LogError("GetAuctionWinner received empty Guid");
                return BadRequest("Auction ID cannot be empty");
            }

            var user = await _repository.UserIdGetAuctionWinner(auctionId);
            _logger.LogInformation("Winner for auction ID: {AuctionId} retrieved successfully", auctionId);
            return Ok(user);
        }

        [HttpGet("start")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetByStartTime([FromQuery] DateTime start)
        {
            _logger.LogInformation("Received request to get auctions starting at: {StartTime}", start);
            var auctions = await _repository.GetAuctionByStartTime(start);
            return Ok(auctions);
        }

        [HttpGet("end")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByEndTime([FromQuery] DateTime end)
        {
            _logger.LogInformation("Received request to get auctions ending at: {EndTime}", end);
            var auctions = await _repository.GetAuctionByEndTime(end);
            return Ok(auctions);
        }

        [HttpGet("status")]
        [AllowAnonymous]
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