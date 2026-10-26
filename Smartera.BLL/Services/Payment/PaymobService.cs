using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.PaymentDto;
using Smartera.BLL.Errors;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Services.Payment
{
    public class PaymobService : IPaymobService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly PaymobOptions _options;
        private readonly ILogger<PaymobService> _logger;

        public PaymobService(
            HttpClient httpClient,
            AppDbContext context,
            IOptions<PaymobOptions> options,
            ILogger<PaymobService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _options = options.Value;
            _logger = logger;

            if (!string.IsNullOrEmpty(_options.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            }
        }

        public async Task<Result<PaymobInitiateResponse>> InitiatePaymentAsync(
            Order order,
            ApplicationUser user,
            CancellationToken cancellationToken = default)
        {
            // 1. Calculate and validate payable amount strictly using server-side order total
            var payableAmount = order.TotalAmount;
            if (payableAmount <= 0)
            {
                return Result.Failure<PaymobInitiateResponse>(
                    new Error("Payment.InvalidAmount", "Order amount must be greater than zero.", 400));
            }

            var amountCents = (long)Math.Round(payableAmount * 100, MidpointRounding.AwayFromZero);
            var merchantOrderId = $"SMARTERA-ORD-{order.Id}-{DateTime.UtcNow.Ticks}";

            // 2. Persist or update pending payment record
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
            if (payment == null)
            {
                payment = new DAL.Entities.Payment
                {
                    OrderId = order.Id,
                    TotalAmount = payableAmount,
                    Currency = "EGP",
                    Status = PaymentStatus.Pending,
                    PaymentMethod = "Paymob",
                    MerchantOrderId = merchantOrderId,
                    TransactionDate = DateTime.UtcNow
                };
                await _context.Payments.AddAsync(payment, cancellationToken);
            }
            else
            {
                payment.MerchantOrderId = merchantOrderId;
                payment.Status = PaymentStatus.Pending;
                payment.TransactionDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 3. Initiate payment with Paymob API
            string paymentUrl;
            string? clientSecret = null;

            if (!string.IsNullOrWhiteSpace(_options.SecretKey) || !string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                try
                {
                    var nameParts = (user.FullName ?? "Smartera Customer").Trim().Split(' ', 2);
                    var firstName = nameParts.Length > 0 && !string.IsNullOrWhiteSpace(nameParts[0]) ? nameParts[0] : "Customer";
                    var lastName = nameParts.Length > 1 && !string.IsNullOrWhiteSpace(nameParts[1]) ? nameParts[1] : "Smartera";

                    var requestPayload = new
                    {
                        amount = amountCents,
                        currency = "EGP",
                        payment_methods = int.TryParse(_options.IntegrationId, out var integrationId)
                            ? new object[] { integrationId }
                            : Array.Empty<object>(),
                        billing_data = new
                        {
                            first_name = firstName,
                            last_name = lastName,
                            email = user.Email ?? "customer@smartera.com",
                            phone_number = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : "+201000000000",
                            street = !string.IsNullOrWhiteSpace(order.ShippingAddress) ? order.ShippingAddress : "Main Street",
                            city = "Cairo",
                            country = "EG",
                            apartment = "NA",
                            floor = "NA",
                            building = "NA",
                            postal_code = "11511",
                            state = "Cairo"
                        },
                        customer = new
                        {
                            first_name = firstName,
                            last_name = lastName,
                            email = user.Email ?? "customer@smartera.com"
                        },
                        special_reference = merchantOrderId,
                        notification_url = _options.CallbackUrl,
                        redirection_url = _options.ReturnUrl
                    };

                    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/intention/");
                    var token = !string.IsNullOrWhiteSpace(_options.SecretKey) ? _options.SecretKey : _options.ApiKey;
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
                    requestMessage.Content = new StringContent(
                        JsonSerializer.Serialize(requestPayload),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("client_secret", out var csElement))
                        {
                            clientSecret = csElement.GetString();
                            payment.PaymobIntentionId = clientSecret;
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Paymob intention API returned status {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to call Paymob intention API, generating standard payment URL");
                }
            }

            if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(_options.PublicKey))
            {
                paymentUrl = $"{_options.BaseUrl.TrimEnd('/')}/unifiedcheckout/?publicKey={_options.PublicKey}&clientSecret={clientSecret}";
            }
            else if (!string.IsNullOrEmpty(_options.IframeId))
            {
                paymentUrl = $"{_options.BaseUrl.TrimEnd('/')}/api/acceptance/iframes/{_options.IframeId}?payment_token={clientSecret ?? merchantOrderId}";
            }
            else
            {
                paymentUrl = $"{_options.BaseUrl.TrimEnd('/')}/unifiedcheckout/?publicKey={_options.PublicKey}&merchantOrderId={merchantOrderId}";
            }

            var result = new PaymobInitiateResponse(
                paymentUrl,
                merchantOrderId,
                clientSecret,
                payableAmount,
                "EGP"
            );

            return Result.Success(result);
        }

        public bool ValidateHmac(PaymobTransactionObject obj, string receivedHmac)
        {
            if (string.IsNullOrWhiteSpace(_options.HmacSecret) || string.IsNullOrWhiteSpace(receivedHmac))
            {
                _logger.LogWarning("HMAC verification failed: Missing HMAC secret or received HMAC token.");
                return false;
            }

            var concatenated = new StringBuilder()
                .Append(obj.AmountCents)
                .Append(obj.CreatedAt ?? string.Empty)
                .Append(obj.Currency ?? string.Empty)
                .Append(obj.ErrorOccured.ToString().ToLower())
                .Append(obj.HasParentTransaction.ToString().ToLower())
                .Append(obj.Id)
                .Append(obj.IntegrationId)
                .Append(obj.Is3dSecure.ToString().ToLower())
                .Append(obj.IsAuth.ToString().ToLower())
                .Append(obj.IsCapture.ToString().ToLower())
                .Append(obj.IsRefunded.ToString().ToLower())
                .Append(obj.IsStandalonePayment.ToString().ToLower())
                .Append(obj.IsVoided.ToString().ToLower())
                .Append(obj.Order?.Id.ToString() ?? string.Empty)
                .Append(obj.Owner)
                .Append(obj.Pending.ToString().ToLower())
                .Append(obj.SourceData?.Pan ?? string.Empty)
                .Append(obj.SourceData?.SubType ?? string.Empty)
                .Append(obj.SourceData?.Type ?? string.Empty)
                .Append(obj.Success.ToString().ToLower())
                .ToString();

            using var hmacSha512 = new HMACSHA512(Encoding.UTF8.GetBytes(_options.HmacSecret));
            var computedHash = hmacSha512.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
            var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHex),
                Encoding.UTF8.GetBytes(receivedHmac.ToLowerInvariant()));
        }

        public async Task<Result<PaymentStatusResponse>> ProcessWebhookAsync(
            PaymobWebhookPayload payload,
            string hmac,
            CancellationToken cancellationToken = default)
        {
            if (payload?.Obj == null)
            {
                return Result.Failure<PaymentStatusResponse>(
                    new Error("Payment.InvalidPayload", "Webhook payload object is missing.", 400));
            }

            var obj = payload.Obj;

            // 1. Verify HMAC authenticity
            if (!ValidateHmac(obj, hmac))
            {
                _logger.LogWarning("Invalid HMAC received for Paymob transaction ID {TransactionId}", obj.Id);
                return Result.Failure<PaymentStatusResponse>(PaymentErrors.InvalidHmacSignature);
            }

            var merchantOrderId = obj.Order?.MerchantOrderId;
            var transactionIdStr = obj.Id.ToString();

            // 2. Find payment by MerchantOrderId or PaymobTransactionId
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p =>
                    (!string.IsNullOrEmpty(merchantOrderId) && p.MerchantOrderId == merchantOrderId) ||
                    p.PaymobTransactionId == transactionIdStr,
                    cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment record not found for merchant order ID: {MerchantOrderId}", merchantOrderId);
                return Result.Failure<PaymentStatusResponse>(PaymentErrors.PaymentNotFound);
            }

            // 3. Idempotency check: if already completed, return existing success state without duplicate fulfillment
            if (payment.Status == PaymentStatus.Completed)
            {
                _logger.LogInformation("Payment {PaymentId} is already marked Completed. Skipping duplicate processing.", payment.Id);
                return Result.Success(MapToResponse(payment));
            }

            payment.PaymobTransactionId = transactionIdStr;
            payment.ProviderResponse = $"TransactionId: {obj.Id}, Type: {obj.SourceData?.Type}, SubType: {obj.SourceData?.SubType}";

            // 4. Update order and payment status based on verified success flag
            if (obj.Success && !obj.Pending && !obj.ErrorOccured)
            {
                payment.Status = PaymentStatus.Completed;
                payment.PaymentConfirmedAt = DateTime.UtcNow;
                payment.Order.Status = OrderStatus.Processing;
                _logger.LogInformation("Payment {PaymentId} for Order {OrderId} verified and marked as Completed.", payment.Id, payment.OrderId);
            }
            else if (!obj.Pending)
            {
                payment.Status = PaymentStatus.Failed;
                _logger.LogWarning("Payment {PaymentId} failed with Paymob status success={Success}.", payment.Id, obj.Success);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToResponse(payment));
        }

        public async Task<Result<PaymentStatusResponse>> GetPaymentStatusAsync(
            int paymentId,
            string currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return Result.Failure<PaymentStatusResponse>(PaymentErrors.PaymentNotFound);
            }

            if (!isAdmin && payment.Order.ApplicationUserId != currentUserId)
            {
                return Result.Failure<PaymentStatusResponse>(UserErrors.UnauthorizedAccess);
            }

            return Result.Success(MapToResponse(payment));
        }

        private static PaymentStatusResponse MapToResponse(DAL.Entities.Payment payment)
        {
            return new PaymentStatusResponse(
                payment.Id,
                payment.OrderId,
                payment.TotalAmount,
                payment.Currency,
                payment.Status.ToString(),
                payment.PaymentMethod,
                payment.MerchantOrderId,
                payment.PaymobTransactionId,
                payment.PaymentConfirmedAt
            );
        }
    }
}
