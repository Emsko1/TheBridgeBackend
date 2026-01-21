using Microsoft.AspNetCore.Mvc;
using Bridge.Backend.Services;
using System.Threading.Tasks;
using Bridge.Backend.Data;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class PayoutController : ControllerBase {
    private readonly IPaymentProvider _payment;
    private readonly BridgeDbContext _db;
    public PayoutController(IPaymentProvider payment, BridgeDbContext db){ _payment = payment; _db = db; }

    [HttpPost("create-recipient")]
    public async Task<IActionResult> CreateRecipient([FromBody] dynamic req){
      // req: { name, account_number, bank_code }
      if (!(_payment is Bridge.Backend.Services.PaystackPaymentProvider paystack)) {
        return BadRequest("Payment provider does not support Paystack transfers.");
      }
      var res = await paystack.CreateTransferRecipientAsync((string)req.name, (string)req.account_number, (string)req.bank_code);
      return Ok(res);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] dynamic req){
      // req: { recipient, amount, reason }
      var recipient = (string)req.recipient;
      decimal amount = (decimal)req.amount;
      string reason = (string)req.reason;
      if (!(_payment is Bridge.Backend.Services.PaystackPaymentProvider paystack)) {
        return BadRequest("Payment provider does not support Paystack transfers.");
      }
      var res = await paystack.InitiateTransferAsync(recipient, amount, reason);
      return Ok(res);
    }
  }
}
