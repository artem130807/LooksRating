using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Constants;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class PaymentOrder
    {
        public Guid Id {get; private set;}
        public Guid UserId {get; private set;}
        public Guid ProductId {get; private set;}
        public string Payload {get; private set;}
        public int AmountStars {get; private set;}
        public string Currency {get; private set;}
        public string? TelegramPaymentChargeId {get; private set;}
        public string? ProviderPaymentChargeId {get; private set;}
        public User User {get; private set;}
        public Product Product {get; private set;}
        public PaymentOrderStatus Status {get; private set;}
        public DateTime CreatedAt {get; private set;}
        public DateTime UpdatedAt {get; private set;}
        public DateTime? PaidAt {get; private set;}
        public DateTime? FailedAt {get; private set;}
        public DateTime? CancelledAt {get; private set;}
        public string? FailureReason {get; private set;}

        public static Result<PaymentOrder> Create(
            Guid userId,
            Guid productId,
            string payload,
            int amountStars,
            string currency = "XTR")
        {
            var paymentOrder = new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                Payload = payload,
                AmountStars = amountStars,
                Currency = currency,
                Status = PaymentOrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return paymentOrder;
        }

        public void MarkPaid(string telegramPaymentChargeId, string? providerPaymentChargeId = null)
        {
            Status = PaymentOrderStatus.Paid;
            TelegramPaymentChargeId = telegramPaymentChargeId;
            ProviderPaymentChargeId = providerPaymentChargeId;
            PaidAt = DateTime.UtcNow;
            FailureReason = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFailed(string? reason = null)
        {
            Status = PaymentOrderStatus.Failed;
            FailureReason = reason;
            FailedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkCancelled(string? reason = null)
        {
            Status = PaymentOrderStatus.Cancelled;
            FailureReason = reason;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Result<PaymentOrder> CreateVipTopExtensionGrant(
            Guid userId,
            Guid productId,
            DateTime extensionAnchorUtc,
            string payload)
        {
            if (userId == Guid.Empty)
            {
                return Result.Failure<PaymentOrder>("UserId is required");
            }

            if (productId == Guid.Empty)
            {
                return Result.Failure<PaymentOrder>("ProductId is required");
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                return Result.Failure<PaymentOrder>("Payload is required");
            }

            var normalizedPayload = payload.Trim();
            if (normalizedPayload.Length > VipTopConstants.ExtensionPayloadMaxLength)
            {
                return Result.Failure<PaymentOrder>("Payload is too long");
            }

            var chargeId = $"system:{normalizedPayload}";
            if (chargeId.Length > 128)
            {
                return Result.Failure<PaymentOrder>("Charge id is too long");
            }

            var now = DateTime.UtcNow;
            var order = new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                Payload = normalizedPayload,
                AmountStars = 0,
                Currency = "XTR",
                Status = PaymentOrderStatus.Paid,
                TelegramPaymentChargeId = chargeId,
                PaidAt = extensionAnchorUtc,
                CreatedAt = now,
                UpdatedAt = now,
            };

            return order;
        }
    }
}