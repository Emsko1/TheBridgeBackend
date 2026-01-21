using Microsoft.AspNetCore.Mvc;
using Bridge.Backend.Data;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class AdminController : ControllerBase {
    private readonly BridgeDbContext _db;
    public AdminController(BridgeDbContext db){ _db = db; }

    [HttpGet("escrows")]
    public async Task<IActionResult> GetEscrows([FromQuery] string status = null){
      var q = _db.EscrowTransactions.AsQueryable();
      if(!string.IsNullOrEmpty(status)) q = q.Where(e => e.Status == status);
      var items = await q.ToListAsync();
      return Ok(items);
    }

    [HttpPost("escrows/{id}/approve-release")]
    public async Task<IActionResult> ApproveRelease(Guid id){
      var tx = await _db.EscrowTransactions.FindAsync(id);
      if(tx == null) return NotFound();
      tx.Status = "Released";
      await _db.SaveChangesAsync();
      return Ok(tx);
    }

    [HttpPost("escrows/{id}/mark-dispute")]
    public async Task<IActionResult> MarkDispute(Guid id){
      var tx = await _db.EscrowTransactions.FindAsync(id);
      if(tx == null) return NotFound();
      tx.Status = "InDispute";
      await _db.SaveChangesAsync();
      return Ok(tx);
    }
  }
}
