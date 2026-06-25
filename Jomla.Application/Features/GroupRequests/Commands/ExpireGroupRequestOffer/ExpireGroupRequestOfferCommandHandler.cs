using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer
{
    public class ExpireGroupRequestOfferCommandHandler(
        IAppDbContext db,
        ISender sender,
        INegotiationRoundIndexer roundIndexer
        )
        : IRequestHandler<ExpireGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly ISender _sender = sender;
        private readonly INegotiationRoundIndexer _roundIndexer = roundIndexer;

        public async Task Handle(ExpireGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ فتح Transaction لتأمين كل العمليات المالية وتغيير الحالات
            using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // جلب العرض مع البيانات المرتبطة به كاملة
                var offer = await _db.GroupRequestOffers
                    .Include(o => o.Responses)
                    .Include(o => o.GroupRequest)
                        .ThenInclude(gr => gr.Participants)
                    .Include(o => o.GroupRequest)
                        .ThenInclude(gr => gr.Category)
                    .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

                // التأكد إن العرض لسه مفتوح وموجود، لو منتهي اخرج فوراً
                if (offer is null || offer.Status != GroupRequestOfferStatus.Open) return;

                // بيانات الـ Indexer المستخرجة من شغل زمايلك
                var categoryName = offer.GroupRequest.Category.Name;
                var totalParticipants = offer.GroupRequest.Participants
                    .Count(p => p.Status == GroupRequestParticipantStatus.Active);

                // تجميع الـ BuyerIds اللي وافقوا فعلياً (شغلك المظبوط)
                var acceptedBuyerIds = offer.Responses
                    .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                    .Select(r => r.BuyerId)
                    .ToHashSet();

                // حساب إجمالي الكمية اللي وافقت حتى الآن
                var acceptedQuantity = offer.GroupRequest.Participants
                    .Where(p => acceptedBuyerIds.Contains(p.BuyerId))
                    .Sum(r => r.Quantity);

                // الشرط: هل وصلنا للحد الأدنى البديل اللي التاجر يرتضيه؟
                var shouldCapture = offer.MinFallbackQuantity.HasValue &&
                    acceptedQuantity >= offer.MinFallbackQuantity.Value;

                // [Path A] — fallback quantity met → capture
                if (shouldCapture)
                {
                    // السيناريو الأول: الكمية مرضية للتاجر -> نفذ البيعة وسحب الفلوس فوراً
                    await _sender.Send(new CompleteGroupRequestOfferCommand(offer.Id), cancellationToken);
                    await _roundIndexer.IndexAsync(offer, categoryName, totalParticipants);

                    await _db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return;
                }

                // [Path B] — no room to negotiate (already at floor)
                var floor = offer.MinUnitPrice ?? offer.UnitPrice;
                if (offer.CurrentUnitPrice <= floor)
                {
                    // السيناريو الثاني: الكمية لم تصل للحد الأدنى والوقت انتهى -> البيعة فشلت
                    // 1. نادي كود الـ Cancel عشان يفك حجز الفلوس من Stripe للناس اللي وافقت
                    await _sender.Send(new CancelGroupRequestOfferCommand(offer.Id), cancellationToken);

                    // 2. تغيير حالة العرض لـ Expired بناءً على الـ Enum بتاعك وشغلك
                    offer.Status = GroupRequestOfferStatus.Expired;

                    await _db.SaveChangesAsync(cancellationToken);
                    await _roundIndexer.IndexAsync(offer, categoryName, totalParticipants);

                    await transaction.CommitAsync(cancellationToken);
                    return;
                }

                // [Path C] — negotiation → counter current round, create child offer (شغل زمايلك للتفاوض الآلي)
                await _sender.Send(new NegotiateGroupRequestOfferCommand(offer.Id, categoryName), cancellationToken);
                await _roundIndexer.IndexAsync(offer, categoryName, totalParticipants);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                // لو أي خطأ مالي أو في الداتابيز حصل، ارجع في كلامك فوراً واحمي السيستم
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}