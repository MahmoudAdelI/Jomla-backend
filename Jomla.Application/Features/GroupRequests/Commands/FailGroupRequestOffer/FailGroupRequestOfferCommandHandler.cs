using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
// باستخدام الـ Enum بتاعك مباشرة
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.FailGroupRequestOffer
{
    public class FailGroupRequestOfferCommandHandler : IRequestHandler<FailGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<FailGroupRequestOfferCommandHandler> _logger;

        public FailGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            ILogger<FailGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }

        public async Task Handle(FailGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ فتح Transaction لتأمين العملية بالكامل
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var offer = await _context.GroupRequestOffers
                    .Include(o => o.Responses)
                    .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

                // التأكد إن العرض موجود ولسه ما بقاش Expired قبل كده
                if (offer == null || offer.Status == GroupRequestOfferStatus.Expired)
                {
                    return;
                }

                // 2️⃣ تحديث حالة العرض نفسه لـ Expired (لأن الإلغاء بيقفل العرض تبعا للـ Enum عندك)
                offer.Status = GroupRequestOfferStatus.Expired;

                // 3️⃣ تصفية المشترين المقبولين اللي حاجزين فلوس
                var activeResponses = offer.Responses
                    .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                    .ToList();

                // 4️⃣ اللوب لفك حجز الفلوس من Stripe واحد واحد
                foreach (var response in activeResponses)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(response.StripePaymentIntentId))
                        {
                            var cancelResult = await _stripePaymentService.CancelPaymentAsync(
                                response.StripePaymentIntentId,
                                cancellationToken: cancellationToken);

                            if (!cancelResult.Success)
                            {
                                _logger.LogWarning("Failed to cancel payment hold {PaymentIntentId} for buyer {BuyerId} on offer {OfferId}: {Error}",
                                    response.StripePaymentIntentId, response.BuyerId, offer.Id, cancelResult.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cancelling Stripe payment hold {PaymentIntentId} for buyer {BuyerId} on offer {OfferId}",
                            response.StripePaymentIntentId, response.BuyerId, offer.Id);
                    }

                    // تحويل حالة رد المشتري لملغي
                    response.Response = BuyerOfferResponseType.Cancelled;
                }

                offer.AcceptedQuantity = 0;

                // 5️⃣ حفظ التغييرات في الداتابيز وعمل Commit للـ Transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed for cancelling offer {OfferId}. Rolling back.", request.OfferId);
                throw;
            }
        }
    }
}