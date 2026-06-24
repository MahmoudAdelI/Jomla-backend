using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe.Forwarding;

namespace Jomla.Application.Features.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(IAppDbContext db)
: IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IAppDbContext _db = db;

public async Task<bool> Handle(DeleteCategoryCommand request,
    CancellationToken cancellationToken)
    { 
            var category = await _db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == request.Id,cancellationToken);

            if (category is null)
                 return false;

                if (category.Children.Any())
                    throw new InvalidOperationException("Cannot delete a category that has child categories.");

            var isUsedInPreferences = await _db.SupplierCategoryPreferences.AnyAsync(
                                  x => x.CategoryId == request.Id,
                                     cancellationToken);

            if (isUsedInPreferences)
            {
                throw new InvalidOperationException(
                    "Cannot delete category because it is being used by supplier preferences.");
            }

            var isUsedInOffers = await _db.SupplierOffers.AnyAsync(
                x => x.CategoryId == request.Id,
                cancellationToken);

            if (isUsedInOffers)
            {
                throw new InvalidOperationException(
                    "Cannot delete category because it is being used by supplier offers.");
            }

            var isUsedInRequests = await _db.GroupRequests.AnyAsync(
                x => x.CategoryId == request.Id,
                cancellationToken);

            if (isUsedInRequests)
            {
                throw new InvalidOperationException(
                    "Cannot delete category because it is being used by group requests.");
            }

            _db.Categories.Remove(category);

            await _db.SaveChangesAsync(cancellationToken);

return true;
    }

}
