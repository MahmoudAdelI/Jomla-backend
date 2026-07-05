using Jomla.Application.Common.Interfaces;
using Stripe;

namespace Jomla.Infrastructure.Payments
{
    /// <summary>
    /// StripePaymentService: Stripe API integration for payment holds and captures.
    /// </summary>
    public class StripePaymentService : IStripePaymentService
    {
        private readonly string _stripeSecretKey;

        public StripePaymentService(string stripeSecretKey)
        {
            _stripeSecretKey = stripeSecretKey;
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        /// <summary>
        /// Create a PaymentIntent with capture_method=manual.
        /// This places a HOLD on the card without charging it yet.
        /// </summary>
        public async Task<StripePaymentIntentResult> CreatePaymentHoldAsync(
            string buyerId,
            string buyerEmail,
            decimal amountInDollars,
            Guid batchId,
            string currencyCode = "usd",
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Convert dollars to cents (Stripe works with smallest currency unit)
                var amountInCents = (long)Math.Round(amountInDollars * 100, MidpointRounding.AwayFromZero);
                //PaymentIntentCreateOptions ورقة الطلب اللي بتكتب فيها اللي محتجاه
                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currencyCode,
                    CaptureMethod = "manual", // CRITICAL: hold now, capture later
                    Description = $"Batch join for buyer {buyerId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "batch_id", batchId.ToString() },
                        { "buyer_id", buyerId },
                        { "type", "batch_join" }
                    },
                    ReceiptEmail = buyerEmail,
                    // Automatic confirmation not required yet
                    // Buyer confirms on frontend with clientSecret
                };
                //PaymentIntentService هي اللي بتاخد مني ورقة الطلب تديها ل استرايب علشان تنفذها 
                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);
                // وهنا ترجعلي ب ال id والكي بعد ما تعمل الطلب
                return new StripePaymentIntentResult
                {
                    Success = true,
                    PaymentIntentId = intent.Id,
                    ClientSecret = intent.ClientSecret,
                    Status = intent.Status,
                    Amount = intent.Amount
                };
            }
            catch (StripeException ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = ex.StripeError?.Code ?? "unknown_error"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    ErrorCode = "internal_error"
                };
            }
        }

        /// <summary>
        /// Capture a held PaymentIntent (charge the card).
        /// Called when batch completes and all participants are ready to be charged.
        /// </summary>
        public async Task<StripePaymentIntentResult> CapturePaymentAsync(
      string paymentIntentId,
      string? idempotencyKey = null,
      CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new PaymentIntentCaptureOptions { };
                var requestOptions = idempotencyKey is not null
                    ? new RequestOptions { IdempotencyKey = idempotencyKey }
                    : null;
                var service = new PaymentIntentService();
                var intent = await service.CaptureAsync(
                    paymentIntentId,
                    options,
                    requestOptions,
                    cancellationToken: cancellationToken);

                return new StripePaymentIntentResult
                {
                    Success = true,
                    PaymentIntentId = intent.Id,
                    Status = intent.Status,
                    Amount = intent.Amount
                };
            }
            catch (StripeException ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = ex.StripeError?.Code ?? "unknown_error"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    ErrorCode = "internal_error"
                };
            }
        }
        public async Task<StripePaymentIntentResult> RefundPaymentAsync(
    string paymentIntentId,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId
                };
                var service = new RefundService();
                var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

                return new StripePaymentIntentResult
                {
                    Success = true,
                    PaymentIntentId = paymentIntentId,
                    Status = refund.Status
                };
            }
            catch (StripeException ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = ex.StripeError?.Code ?? "unknown_error"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    ErrorCode = "internal_error"
                };
            }
        }
        /// <summary>
        /// Cancel a held PaymentIntent.
        /// Called when buyer leaves batch before completion, or batch fails.
        /// </summary>
        public async Task<StripePaymentIntentResult> CancelPaymentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new PaymentIntentCancelOptions
                {
                    CancellationReason = "requested_by_customer"
                };
                var service = new PaymentIntentService();
                var intent = await service.CancelAsync(paymentIntentId, options, cancellationToken: cancellationToken);

                return new StripePaymentIntentResult
                {
                    Success = true,
                    PaymentIntentId = intent.Id,
                    Status = intent.Status
                };
            }
            catch (StripeException ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = ex.StripeError?.Code ?? "unknown_error"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    ErrorCode = "internal_error"
                };
            }
        }

        /// <summary>
        /// Retrieve PaymentIntent details from Stripe.
        /// </summary>
        public async Task<StripePaymentIntentResult> GetPaymentIntentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var service = new PaymentIntentService();
                var intent = await service.GetAsync(paymentIntentId, cancellationToken: cancellationToken);

                return new StripePaymentIntentResult
                {
                    Success = true,
                    PaymentIntentId = intent.Id,
                    Status = intent.Status,
                    Amount = intent.Amount,
                    PaymentMethodId = intent.PaymentMethodId
                };
            }
            catch (StripeException ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = ex.StripeError?.Code ?? "unknown_error"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    ErrorCode = "internal_error"
                };
            }
        }

        /// <summary>
        /// Create a PaymentIntent with capture_method=manual, attach an existing PaymentMethod,
        /// and immediately confirm it. Used for cycling holds on quantity updates.
        /// </summary>
        public async Task<StripePaymentIntentResult> CreateConfirmedPaymentHoldAsync(
            string buyerId,
            string buyerEmail,
            decimal amountInDollars,
            Guid batchId,
            string paymentMethodId,
            string currencyCode = "usd",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var amountInCents = (long)Math.Round(amountInDollars * 100, MidpointRounding.AwayFromZero);
                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currencyCode,
                    CaptureMethod = "manual", // CRITICAL: hold now, capture later
                    Description = $"Batch join for buyer {buyerId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "batch_id", batchId.ToString() },
                        { "buyer_id", buyerId },
                        { "type", "batch_join" }
                    },
                    ReceiptEmail = buyerEmail,
                    PaymentMethod = paymentMethodId,
                    Confirm = true
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

                return new StripePaymentIntentResult
                {
                    Success = true,
                    PaymentIntentId = intent.Id,
                    ClientSecret = intent.ClientSecret,
                    Status = intent.Status,
                    Amount = intent.Amount,
                    PaymentMethodId = intent.PaymentMethodId
                };
            }
            catch (StripeException ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = ex.StripeError?.Code ?? "unknown_error"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentIntentResult
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    ErrorCode = "internal_error"
                };
            }
        }
    }
}