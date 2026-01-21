using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Bridge.Backend.Models;
using System.Collections.Generic;

namespace Bridge.Backend.Services {
  public class PaystackPaymentProvider : IPaymentProvider {
    private readonly string _secret;
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    public PaystackPaymentProvider(IConfiguration cfg){
      _cfg = cfg;
      _secret = cfg["Paystack:SecretKey"] ?? string.Empty;
      _http = new HttpClient();
      if(!string.IsNullOrEmpty(_secret)) _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secret);
    }

    public async Task<object> InitializeTransactionAsync(EscrowTransaction tx){
      var body = new {
        email = "buyer@example.com",
        amount = (int)(tx.Amount * 100), // kobo
        callback_url = _cfg["Paystack:CallbackUrl"] ?? "https://your-frontend-domain.com/paystack/callback",
        metadata = new { txId = tx.Id.ToString(), listingId = tx.ListingId.ToString() }
      };
      var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
      var res = await _http.PostAsync("https://api.paystack.co/transaction/initialize", content);
      var text = await res.Content.ReadAsStringAsync();
      if(!res.IsSuccessStatusCode){
        return new { success = false, statusCode = (int)res.StatusCode, body = text };
      }
      var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);
      return parsed;
    }

    public async Task<bool> VerifyTransactionAsync(string reference){
      var res = await _http.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
      if(!res.IsSuccessStatusCode) return false;
      var text = await res.Content.ReadAsStringAsync();
      var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);
      if(parsed != null && parsed.ContainsKey("status") && parsed["status"].ToString().ToLower() == "true"){
        // further checks can be made on data -> amount, status etc.
        return true;
      }
      return false;
    }

    public Task<bool> CaptureAsync(string reference){
      // Paystack generally marks transactions as successful; capture not required separately
      return Task.FromResult(true);
    }

    public async Task<bool> RefundAsync(string reference, decimal amount){
      // For refund, implement Paystack's refund endpoint using POST /refund
      var body = new { transaction = reference, amount = (int)(amount * 100) };
      var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
      var res = await _http.PostAsync("https://api.paystack.co/refund", content);
      return res.IsSuccessStatusCode;
    }

    // Payout (transfer) - uses Paystack Transfer and Transfer Recipient endpoints.
    public async Task<object> CreateTransferRecipientAsync(string name, string accountNumber, string bankCode){
      var body = new {
        type = "nuban",
        name = name,
        account_number = accountNumber,
        bank_code = bankCode,
        currency = "NGN"
      };
      var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
      var res = await _http.PostAsync("https://api.paystack.co/transferrecipient", content);
      var text = await res.Content.ReadAsStringAsync();
      if(!res.IsSuccessStatusCode) return new { success = false, body = text };
      return JsonConvert.DeserializeObject<object>(text);
    }

    public async Task<object> InitiateTransferAsync(string recipientCode, decimal amount, string reason){
      var body = new {
        source = "balance",
        amount = (int)(amount * 100),
        recipient = recipientCode,
        reason = reason
      };
      var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
      var res = await _http.PostAsync("https://api.paystack.co/transfer", content);
      var text = await res.Content.ReadAsStringAsync();
      if(!res.IsSuccessStatusCode) return new { success = false, body = text };
      return JsonConvert.DeserializeObject<object>(text);
    }
  }
}
