using Jomla.Application.Common.DTOs;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.CreateGroupRequest;
using Jomla.Application.Jobs.Agents;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class CreateGroupRequestCommandHandler : IRequestHandler<CreateGroupRequestCommand, CreateGroupRequestResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMediator _mediator;
    private readonly ICategoryAgent _categoryAgent;
    private readonly IBackgroundJobDispatcher _jobDispatcher;

    public CreateGroupRequestCommandHandler(
        IAppDbContext context,
        IMediator mediator,
        ICategoryAgent categoryAgent,
        IBackgroundJobDispatcher jobDispatcher)
    {
        _context = context;
        _mediator = mediator;
        _categoryAgent = categoryAgent;
        _jobDispatcher = jobDispatcher;
    }

    public async Task<CreateGroupRequestResponse> Handle(CreateGroupRequestCommand request, CancellationToken cancellationToken)
    {
        // Step 1: جيب الـcategories من الـDB وبعتهم للـCategoryAgent
        var categories = await _context.Categories
            .Include( C => C.Parent)
            .ToListAsync(cancellationToken);

        var categoryDtos = categories.Select(c => new CategoryDto(
        
             c.Id,
              c.Parent !=null ? $"{c.Parent.Name} > {c.Name}" : c.Name
        ));

        var categoryId = await _categoryAgent.ResolveCategoryAsync(request.Title, categoryDtos, cancellationToken);

        // Step 2: عمل الـGroupRequest
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            InitiatorId = request.InitiatorId,
            CategoryId = categoryId,
            Title = request.Title,
            Description = request.Description,
            ImageUrls = request.ImageUrls != null
                ? JsonSerializer.Serialize(request.ImageUrls)
                : null,
            CurrentQuantity = request.Quantity,
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.GroupRequests.Add(groupRequest);

        // Step 3: عمل الـParticipant للـinitiator في نفس الـtransaction
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = request.InitiatorId,
            Quantity = request.Quantity,
            Status = GroupRequestParticipantStatus.Active,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupRequestParticipants.Add(participant);

        // Step 4: Save الاتنين في نفس الـtransaction
        await _context.SaveChangesAsync(cancellationToken);

        // Step 5: Fire الـModerateGroupRequestJob في الـbackground
        _jobDispatcher.Enqueue<IModerateGroupRequestJob>(j =>
            j.ExecuteAsync(groupRequest.Id, CancellationToken.None));

        return new CreateGroupRequestResponse(true, groupRequest.Id, null);
    }
}