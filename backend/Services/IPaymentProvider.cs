using System.Threading.Tasks;
using Bridge.Backend.Models;

namespace Bridge.Backend.Services {
  public interface IPaymentProvider {
    Task<object> InitializeTransactionAsync(EscrowTransaction tx);
    Task<bool> VerifyTransactionAsync(string reference);
    Task<bool> CaptureAsync(string reference);
    Task<bool> RefundAsync(string reference, decimal amount);
  }
}
