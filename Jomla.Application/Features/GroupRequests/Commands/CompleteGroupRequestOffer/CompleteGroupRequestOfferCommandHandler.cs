using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer
{
    public class CompleteGroupRequestOfferCommandHandler : IRequestHandler<CompleteGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IMediator _mediator;
        private readonly ILogger<CompleteGroupRequestOfferCommandHandler> _logger;

        public CompleteGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IMediator mediator,
            ILogger<CompleteGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(CompleteGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ بدء عملية Transaction موحدة (لضمان نجاح كل شيء أو التراجع عن كل شيء)
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 2️⃣ جلب العرض مع عمل قفل (Lock) لمنع أي Thread آخر من معالجته بالتزامن (Concurrency Handle)
                // بنستخدم هنا ميزة الـ Pessimistic Locking أو التأكد الصارم من الحالة لمنع الـ Race Condition
                var offer = await _context.GroupRequestOffers
                    .Include(o => o.Responses)
                    .Include(o => o.GroupRequest)
                        .ThenInclude(gr => gr.Participants)
                    .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

                // إذا كان العرض غير موجود أو تم معالجته مسبقاً (حيث تغيرت حالته) نخرج فوراً ونلغي الـ Transaction
                if (offer == null || offer.Status != GroupRequestOfferStatus.Open)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                // 3️⃣ تغيير حالة العرض فوراً إلى Accepted داخل الـ Transaction لمنع أي سحب مزدوج
                offer.Status = GroupRequestOfferStatus.Accepted;
                await _context.SaveChangesAsync(cancellationToken);

                var activeResponses = offer.Responses
                    .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                    .ToList();

                bool hasFailure = false;
                var successfulBuyerIds = new List<Guid>();

                // 4️⃣ بدء معالجة سحب الأموال (Capture) لكل المشترين
                foreach (var response in activeResponses)
                {
                    var participant = offer.GroupRequest.Participants
                        .FirstOrDefault(p => p.BuyerId == response.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

                    if (participant == null)
                    {
                        _logger.LogWarning("Buyer {BuyerId} accepted offer {OfferId} but is not an active participant", response.BuyerId, offer.Id);
                        continue;
                    }

                    var existingOrder = await _context.Orders
                        .FirstOrDefaultAsync(o => o.OfferId == offer.Id && o.BuyerId == response.BuyerId, cancellationToken);

                    if (existingOrder != null && existingOrder.Status == OrderStatus.Paid)
                        continue;

                    if (string.IsNullOrEmpty(response.StripePaymentIntentId))
                    {
                        _logger.LogWarning("StripePaymentIntentId is missing for buyer {BuyerId}", response.BuyerId);
                        continue;
                    }

                    // تنفيذ عملية السحب الفعلي من Stripe
                    var captureResult = await _stripePaymentService.CapturePaymentAsync(response.StripePaymentIntentId, cancellationToken);

                    if (existingOrder != null)
                    {
                        existingOrder.Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed;
                        existingOrder.PaidAt = captureResult.Success ? DateTime.UtcNow : null;
                    }
                    else
                    {
                        var order = new Order
                        {
                            BuyerId = response.BuyerId,
                            BatchId = null, // linked directly to offer, no batch link
                            OfferId = offer.Id,
                            Quantity = participant.Quantity,
                            TotalAmount = participant.Quantity * offer.UnitPrice,
                            Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed,
                            PaidAt = captureResult.Success ? DateTime.UtcNow : null,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Orders.Add(order);
                    }

                    if (captureResult.Success)
                    {
                        successfulBuyerIds.Add(response.BuyerId);
                    }
                    else
                    {
                        hasFailure = true;
                    }
                }

                // 5️⃣ الحماية من الفشل الجزئي: لو أي عملية سحب فشلت، نرمي Exception ليعود السيستم للحالة الأصلية
                if (hasFailure)
                {
                    throw new Exception("One or more captures failed for GroupRequestOffer. Rolling back transaction. Hangfire will retry.");
                }

                // حفظ جميع التغييرات والطلبات في الداتابيز
                await _context.SaveChangesAsync(cancellationToken);

                // 6️⃣ إغلاق الجروب ريكويست رسمياً بعد نجاح العملية المالية بالكامل
                await _mediator.Send(new CloseGroupRequest.CloseGroupRequestCommand(offer.GroupRequestId), cancellationToken);

                // تأكيد وحفظ الـ Transaction بالكامل (Commit)
                await transaction.CommitAsync(cancellationToken);

                // 7️⃣ إنشاء وإرسال الإشعارات للناجحين (تتم بره الـ Transaction عشان لو الـ Realtime علق ما يخربش الحسابات)
                foreach (var buyerId in successfulBuyerIds)
                {
                    var notification = new Notification
                    {
                        UserId = buyerId,
                        Type = NotificationType.GroupRequestOfferFilled,
                        Title = "Group Request Offer Filled",
                        Body = "Your accepted group request offer has been filled and payment was captured.",
                        EntityId = offer.Id,
                        EntityType = nameof(GroupRequestOffer),
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync(cancellationToken);

                    // إطلاق الجرس اللحظي للموبايل
                    await _mediator.Publish(new NotificationCreatedEvent(buyerId, notification.Id), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // في حال حدوث أي خطأ غير متوقع.. تراجع تام عن كل التغييرات بالداتابيز لتأمين البيانات
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Complete process failed and transaction rolled back for Offer {OfferId}", request.OfferId);
                throw;
            }
        }
    }
}