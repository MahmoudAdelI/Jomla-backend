namespace Jomla.Application.Common.Interfaces
{
    /// <summary>
    /// IStripePaymentService: Stripe payment integration for batch joins.
    /// Handles creating payment holds, capturing, and canceling.
    /// </summary>
    public interface IStripePaymentService
    {
        /// <summary>
        /// Create a payment hold (PaymentIntent) for a batch join.
        /// Amount in dollars, will be converted to cents.
        /// </summary>
        Task<StripePaymentIntentResult> CreatePaymentHoldAsync(
            string buyerId,
            string buyerEmail,
            decimal amountInDollars,
            Guid batchId,
            string currencyCode = "usd",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Capture a held payment (charge the card).
        /// Called when batch completes.
        /// </summary>
        Task<StripePaymentIntentResult> CapturePaymentAsync(
             string paymentIntentId,
             string? idempotencyKey = null,
             CancellationToken cancellationToken = default);


        /// <summary>
        /// Refund a captured payment.
        /// Called when partial capture failure occurs.
        /// </summary>
        Task<StripePaymentIntentResult> RefundPaymentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Cancel a held payment.
        /// Called when buyer leaves batch or batch fails.
        /// </summary>
        Task<StripePaymentIntentResult> CancelPaymentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve PaymentIntent details from Stripe.
        /// </summary>
        Task<StripePaymentIntentResult> GetPaymentIntentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result DTO for Stripe operations.
    /// </summary>
    public class StripePaymentIntentResult
    {
        public bool Success { get; set; }
        public string PaymentIntentId { get; set; }
        public string ClientSecret { get; set; } // For frontend confirmation
        public string Status { get; set; } // "requires_capture", "succeeded", "canceled", etc.
        public long? Amount { get; set; } // In cents
        public string Error { get; set; }
        public string ErrorCode { get; set; }
    }
}