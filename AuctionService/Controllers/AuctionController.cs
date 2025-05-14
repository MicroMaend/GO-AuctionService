using Microsoft.AspNetCore.Mvc;
using AuctionService.Repositories;
using GOCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionController : ControllerBase
    {
        private readonly IAuctionRepository _repository;

        public AuctionController(IAuctionRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAuction([FromBody] Auction auction)
        {
            await _repository.CreateAuction(auction);
            return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, auction);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuction(Guid id)
        {
            var result = await _repository.DeleteAuction(id);
            return result ? NoContent() : NotFound();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditAuction(Guid id, [FromBody] Auction auction)
        {
            if (id != auction.Id)
                return BadRequest("Auction ID mismatch");

            var result = await _repository.EditAuction(auction);
            return result ? Ok(auction) : NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAuctions()
        {
            var auctions = await _repository.GetAllAuctions();
            return Ok(auctions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuctionById(Guid id)
        {
            var auction = await _repository.GetAuctionById(id);
            return auction != null ? Ok(auction) : NotFound();
        }

        [HttpGet("{id}/winner")]
        public async Task<IActionResult> GetAuctionWinner(Guid id)
        {
            var user = await _repository.UserGetAuctionWinner(id);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpGet("start")]
        public async Task<IActionResult> GetByStartTime([FromQuery] DateTime start)
        {
            var auctions = await _repository.GetAuctionByStartTime(start);
            return Ok(auctions);
        }

        [HttpGet("end")]
        public async Task<IActionResult> GetByEndTime([FromQuery] DateTime end)
        {
            var auctions = await _repository.GetAuctionByEndTime(end);
            return Ok(auctions);
        }
    }
}
