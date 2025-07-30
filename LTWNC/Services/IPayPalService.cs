namespace LTWNC.Services
{
    public interface IPayPalService
    {
        Task<(string orderId, string approveUrl)> CreateOrder(decimal usdAmount);
        Task<bool> CaptureOrder(string orderId);
    }

}
