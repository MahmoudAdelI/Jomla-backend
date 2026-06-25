using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer
{
    public class ExpireGroupRequestOfferCommandHandler(IAppDbContext db, ISender sender)
        : IRequestHandler<ExpireGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly ISender _sender = sender;

        public async Task Handle(ExpireGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            // جلب العرض مع البيانات المرتبطة به
            var offer = await _db.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            // التأكد إن العرض لسه مفتوح وموجود
            if (offer is null || offer.Status != GroupRequestOfferStatus.Open) return;

            // تجميع الـ BuyerIds اللي وافقوا فعلياً
            var acceptedBuyerIds = offer.Responses
                .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                .Select(r => r.BuyerId)
                .ToHashSet();

            // حساب إجمالي الكمية اللي وافقت حتى الآن
            var acceptedQuantity = offer.GroupRequest.Participants
                .Where(p =>
                    acceptedBuyerIds.Contains(p.BuyerId) &&
                    p.Status == GroupRequestParticipantStatus.Active)
                .Sum(r => r.Quantity);

            // الشرط: هل وصلنا للحد الأدنى البديل اللي التاجر يرتضيه؟
            var shouldCapture = offer.MinFallbackQuantity.HasValue &&
                acceptedQuantity >= offer.MinFallbackQuantity.Value;

            if (shouldCapture)
            {
                //  السيناريو الأول: الكمية مرضية للتاجر -> نفذ البيعة وسحب الفلوس فوراً
                // كود الـ Complete هو اللي هيتولى تحويل الحالة لـ Accepted وقفل الجروب
                await _sender.Send(new CompleteGroupRequestOfferCommand(offer.Id), cancellationToken);
            }
            else
            {
                //  السيناريو الثاني: الكمية لم تصل للحد الأدنى والوقت انتهى -> البيعة فشلت
                // 1. بنادي كود الـ Cancel عشان يفك حجز الفلوس من Stripe للناس اللي وافقت
                await _sender.Send(new CancelGroupRequestOfferCommand(offer.Id), cancellationToken);

                // 2. هنا بقى نغير حالة العرض لـ Expired لأن البيعة باظت بسبب الوقت
                offer.Status = GroupRequestOfferStatus.Expired;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}