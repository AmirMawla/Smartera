using System.Text.Json.Serialization;

namespace Smartera.BLL.Dtos.PaymentDto
{
    public record InitiatePaymentRequest(int OrderId);

    public record PaymobInitiateResponse(
        string PaymentUrl,
        string MerchantOrderId,
        string? IntentionId,
        decimal Amount,
        string Currency
    );

    public record PaymentStatusResponse(
        int PaymentId,
        int OrderId,
        decimal Amount,
        string Currency,
        string Status,
        string PaymentMethod,
        string MerchantOrderId,
        string? PaymobTransactionId,
        DateTime? ConfirmedAt
    );

    public record TransactionResponse(
        int Id,
        int OrderId,
        string CustomerName,
        decimal TotalAmount,
        string Currency,
        string Status,
        string PaymentMethod,
        string MerchantOrderId,
        DateTime TransactionDate
    );

    public class PaymobWebhookPayload
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("obj")]
        public PaymobTransactionObject? Obj { get; set; }
    }

    public class PaymobTransactionObject
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("is_auth")]
        public bool IsAuth { get; set; }

        [JsonPropertyName("is_capture")]
        public bool IsCapture { get; set; }

        [JsonPropertyName("is_standalone_payment")]
        public bool IsStandalonePayment { get; set; }

        [JsonPropertyName("is_voided")]
        public bool IsVoided { get; set; }

        [JsonPropertyName("is_refunded")]
        public bool IsRefunded { get; set; }

        [JsonPropertyName("is_3d_secure")]
        public bool Is3dSecure { get; set; }

        [JsonPropertyName("integration_id")]
        public long IntegrationId { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("error_occured")]
        public bool ErrorOccured { get; set; }

        [JsonPropertyName("has_parent_transaction")]
        public bool HasParentTransaction { get; set; }

        [JsonPropertyName("owner")]
        public long Owner { get; set; }

        [JsonPropertyName("order")]
        public PaymobOrderInfo? Order { get; set; }

        [JsonPropertyName("source_data")]
        public PaymobSourceData? SourceData { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, object>? Data { get; set; }
    }

    public class PaymobOrderInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("merchant_order_id")]
        public string? MerchantOrderId { get; set; }
    }

    public class PaymobSourceData
    {
        [JsonPropertyName("pan")]
        public string? Pan { get; set; }

        [JsonPropertyName("sub_type")]
        public string? SubType { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
