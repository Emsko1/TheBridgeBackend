using Microsoft.AspNetCore.Mvc;
using Bridge.Backend.Services;
using Bridge.Backend.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class PaystackController : ControllerBase {
    private readonly IPaymentProvider _payment;
    private readonly Bridge.Backend.Data.BridgeDbContext _db;
    private readonly IConfiguration _cfg;
    public PaystackController(IPaymentProvider payment, Bridge.Backend.Data.BridgeDbContext db, IConfiguration cfg){
      _payment = payment; _db = db; _cfg = cfg;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] EscrowTransaction req){
      req.Id = System.Guid.NewGuid();
      req.Status = "PendingPayment";
      _db.EscrowTransactions.Add(req);
      await _db.SaveChangesAsync();

      var init = await _payment.InitializeTransactionAsync(req);
      return Ok(init);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook() {
      var secret = _cfg["Paystack:SecretKey"] ?? string.Empty;
      if(string.IsNullOrEmpty(secret)) return BadRequest("Paystack secret not configured.");

      var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
      using var reader = new StreamReader(Request.Body, Encoding.UTF8);
      var body = await reader.ReadToEndAsync();

      if(string.IsNullOrEmpty(signature)){
        return BadRequest();
      }

      using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(secret));
      var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
      var computed = BitConverter.ToString(computedHash).Replace("-","").ToLower();

      if(computed != signature){
        return Unauthorized();
      }

      var payload = JObject.Parse(body);
      var @event = payload.SelectToken("event")?.ToString();
      if(@event == "charge.success"){
        var data = payload.SelectToken("data");
        var reference = data.SelectToken("reference")?.ToString();
        var ok = await _payment.VerifyTransactionAsync(reference);
        if(ok){
          var txId = data.SelectToken("metadata.txId")?.ToString();
          if(Guid.TryParse(txId, out var guid)){
            var tx = await _db.EscrowTransactions.FindAsync(guid);
            if(tx != null){
              tx.Status = "FundsHeld";
              tx.ProviderReference = reference;
              await _db.SaveChangesAsync();

              // Auto-payout logic (demo): if seller has bank info stored in Users table, initiate transfer
              var seller = await _db.Users.FindAsync(tx.SellerId);
              if(seller != null){
                // For demo purposes we expect seller.Email to hold account number and Name to hold bank code in placeholder format:
                // In production, store proper bank account fields. Here: Email = accountNumber, Name = bankCode (placeholder).
                try {
                  var accountNumber = seller.Email ?? string.Empty;
                  var bankCode = seller.Name ?? string.Empty;
                  // Create recipient
                  var provider = _payment as Bridge.Backend.Services.PaystackPaymentProvider;
                  if(provider != null && !string.IsNullOrEmpty(accountNumber) && !string.IsNullOrEmpty(bankCode)){
                    var recipientRes = await provider.CreateTransferRecipientAsync(seller.Name ?? "Seller", accountNumber, bankCode);
                    // If recipient creation successful, parse recipient code and initiate transfer
                    // parsing is left generic because Paystack response shape may vary
                    await provider.InitiateTransferAsync(((dynamic)recipientRes)?.data?.recipient_code?.ToString() ?? "", tx.Amount - 1000 /* fee demo */, "Payout for listing " + tx.ListingId.ToString());
                    tx.Status = "Released";
                    await _db.SaveChangesAsync();
                  }
                } catch(Exception ex){
                  // Log and keep funds held for manual payout
                }
              }
            }
          }
        }
      }

      return Ok();
    }
  }
}
