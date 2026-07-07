using Jomla.Application.Features.GroupRequests.Queries.GetSupplierMatchedGroupRequests;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class GetSupplierMatchedGroupRequestsQueryHandlerTests : ApplicationTestBase
{
    private readonly GetSupplierMatchedGroupRequestsQueryHandler _handler;

    public GetSupplierMatchedGroupRequestsQueryHandlerTests()
    {
        _handler = new GetSupplierMatchedGroupRequestsQueryHandler(Context);
    }

    [Fact]
    public async Task Handle_FilterByParentCategory_ReturnsSubcategoryAlerts()
    {
        // Arrange
        var supplierId = Guid.NewGuid();

        var parentCategory = new Category { Id = Guid.NewGuid(), Name = "Electronics" };
        var subCategory = new Category { Id = Guid.NewGuid(), Name = "Laptops", ParentId = parentCategory.Id };

        var request1 = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Title = "Laptop request",
            CategoryId = subCategory.Id,
            InitiatorId = Guid.NewGuid(),
            CurrentQuantity = 10,
            Status = GroupRequestStatus.Active
        };

        var request2 = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Title = "Other request",
            CategoryId = Guid.NewGuid(), // different category
            InitiatorId = Guid.NewGuid(),
            CurrentQuantity = 10,
            Status = GroupRequestStatus.Active
        };

        var alert1 = new GroupRequestAlert
        {
            SupplierId = supplierId,
            GroupRequestId = request1.Id,
            Status = GroupRequestAlertStatus.Pending,
            GroupRequest = request1
        };

        var alert2 = new GroupRequestAlert
        {
            SupplierId = supplierId,
            GroupRequestId = request2.Id,
            Status = GroupRequestAlertStatus.Pending,
            GroupRequest = request2
        };

        Context.Categories.AddRange(parentCategory, subCategory);
        Context.GroupRequests.AddRange(request1, request2);
        Context.GroupRequestAlerts.AddRange(alert1, alert2);
        await Context.SaveChangesAsync();

        var query = new GetSupplierMatchedGroupRequestsQuery(
            SupplierId: supplierId,
            Page: 1,
            PageSize: 10,
            Search: null,
            CategoryId: parentCategory.Id,
            Status: null
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Laptop request", result.Items[0].Title);
    }
}
