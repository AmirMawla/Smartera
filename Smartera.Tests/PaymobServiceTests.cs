using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Smartera.BLL.Dtos.PaymentDto;
using Smartera.BLL.Services.Payment;
using Smartera.DAL.Data;
using Xunit;

namespace Smartera.Tests
{
    public class PaymobServiceTests
    {
        private const string TestHmacSecret = "test_hmac_secret_key_123456789";

        private PaymobService CreateService(AppDbContext context)
        {
            var options = Options.Create(new PaymobOptions
            {
                ApiKey = "test_api_key",
                SecretKey = "test_secret_key",
                PublicKey = "test_public_key",
                HmacSecret = TestHmacSecret,
                BaseUrl = "https://accept.paymob.com",
                CallbackUrl = "https://example.com/api/payments/webhook",
                ReturnUrl = "https://example.com/api/payments/return"
            });

            var loggerMock = new Mock<ILogger<PaymobService>>();
            var httpClient = new HttpClient();

            return new PaymobService(httpClient, context, options, loggerMock.Object);
        }

        private static string CalculateExpectedHmac(PaymobTransactionObject obj, string secret)
        {
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

            using var hmacSha512 = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmacSha512.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
            return Convert.ToHexString(computedHash).ToLowerInvariant();
        }

        [Fact]
        public void ValidateHmac_WithValidSignature_ReturnsTrue()
        {
            // Arrange
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(dbOptions);
            var service = CreateService(context);

            var transactionObj = new PaymobTransactionObject
            {
                Id = 1234567,
                AmountCents = 250000,
                CreatedAt = "2026-10-27T10:00:00.000000",
                Currency = "EGP",
                ErrorOccured = false,
                HasParentTransaction = false,
                IntegrationId = 987654,
                Is3dSecure = true,
                IsAuth = false,
                IsCapture = false,
                IsRefunded = false,
                IsStandalonePayment = true,
                IsVoided = false,
                Order = new PaymobOrderInfo { Id = 55555, MerchantOrderId = "SMART-ORD-1234" },
                Owner = 11111,
                Pending = false,
                SourceData = new PaymobSourceData { Pan = "2345", SubType = "MasterCard", Type = "card" },
                Success = true
            };

            var validHmac = CalculateExpectedHmac(transactionObj, TestHmacSecret);

            // Act
            var isValid = service.ValidateHmac(transactionObj, validHmac);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateHmac_WithTamperedPayload_ReturnsFalse()
        {
            // Arrange
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(dbOptions);
            var service = CreateService(context);

            var transactionObj = new PaymobTransactionObject
            {
                Id = 1234567,
                AmountCents = 250000,
                Currency = "EGP",
                ErrorOccured = false,
                HasParentTransaction = false,
                IntegrationId = 987654,
                Success = true
            };

            var validHmac = CalculateExpectedHmac(transactionObj, TestHmacSecret);

            // Tamper with the amount
            transactionObj.AmountCents = 1000;

            // Act
            var isValid = service.ValidateHmac(transactionObj, validHmac);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateHmac_WithTamperedHmacSignature_ReturnsFalse()
        {
            // Arrange
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(dbOptions);
            var service = CreateService(context);

            var transactionObj = new PaymobTransactionObject
            {
                Id = 1234567,
                AmountCents = 250000,
                Success = true
            };

            var invalidHmac = "abcdef1234567890abcdef1234567890";

            // Act
            var isValid = service.ValidateHmac(transactionObj, invalidHmac);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateHmac_WithEmptyOrNullHmac_ReturnsFalse()
        {
            // Arrange
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(dbOptions);
            var service = CreateService(context);

            var transactionObj = new PaymobTransactionObject { Id = 123 };

            // Act & Assert
            Assert.False(service.ValidateHmac(transactionObj, ""));
            Assert.False(service.ValidateHmac(transactionObj, null!));
        }
    }
}
