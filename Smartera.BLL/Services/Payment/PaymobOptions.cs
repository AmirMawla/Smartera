namespace Smartera.BLL.Services.Payment
{
    public class PaymobOptions
    {
        public const string SectionName = "Paymob";

        public string ApiKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public string IframeId { get; set; } = string.Empty;
        public string HmacSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://accept.paymob.com";
        public string ReturnUrl { get; set; } = "https://localhost:7142/api/payments/return";
        public string CallbackUrl { get; set; } = "https://localhost:7142/api/payments/webhook";
    }
}
