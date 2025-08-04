using LTWNC.Services;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using System.Globalization;
using PayPalOrder = PayPalCheckoutSdk.Orders.Order;

public class PayPalService : IPayPalService
{
    private readonly PayPalClient _client;

    public PayPalService(PayPalClient client)
    {
        _client = client;
    }

    public async Task<(string orderId, string approveUrl)> CreateOrder(decimal usdAmount)
    {
        var request = new OrdersCreateRequest();
        request.Prefer("return=representation");
        request.RequestBody(new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = "USD",
                        Value = usdAmount.ToString("F2", CultureInfo.InvariantCulture)

                    }
                }
            },
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = "https://unistyle40th.vercel.app/payment-success",
                CancelUrl = "https://your-frontend.com/paypal-cancel"
            }
        });

        var response = await _client.Client().Execute(request);
        var result = response.Result<PayPalOrder>();
        var approveUrl = result.Links.FirstOrDefault(l => l.Rel == "approve")?.Href;

        return (result.Id, approveUrl);
    }

   
    public async Task<bool> CaptureOrder(string orderId)
    {
        var request = new OrdersCaptureRequest(orderId);
        request.RequestBody(new OrderActionRequest());

        try
        {
            var response = await _client.Client().Execute(request);
            var result = response.Result<PayPalOrder>();

            Console.WriteLine($"PayPal Capture Status: {result.Status}");

            // ✅ So sánh đúng kiểu với Status
            return string.Equals(result.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase);
        }
        catch (HttpException httpEx)
        {
            Console.WriteLine($"PayPal Capture Error: {httpEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during capture: {ex.Message}");
            return false;
        }
    }


}
