using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MediatR;
using NSubstitute;
using System;

namespace Jomla.Application.Tests.Infrastructure;

public abstract class ApplicationTestBase : IDisposable
{
    protected AppDbContext InnerContext { get; }
    protected TestAppDbContext Context { get; }
    
    protected IMediator Mediator { get; }
    protected IRealtimeService RealtimeService { get; }
    protected IStripePaymentService StripePaymentService { get; }
    protected IBackgroundJobDispatcher JobDispatcher { get; }
    protected IIdentityService IdentityService { get; }
    protected ICategoryAgent CategoryAgent { get; }
    protected IEmailService EmailService { get; }

    protected ApplicationTestBase()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        InnerContext = new AppDbContext(options);
        // Ensure database starts clean
        InnerContext.Database.EnsureCreated();
        Context = new TestAppDbContext(InnerContext);

        Mediator = Substitute.For<IMediator>();
        RealtimeService = Substitute.For<IRealtimeService>();
        StripePaymentService = Substitute.For<IStripePaymentService>();
        JobDispatcher = Substitute.For<IBackgroundJobDispatcher>();
        IdentityService = Substitute.For<IIdentityService>();
        CategoryAgent = Substitute.For<ICategoryAgent>();
        EmailService = Substitute.For<IEmailService>();
    }

    public virtual void Dispose()
    {
        InnerContext.Database.EnsureDeleted();
        InnerContext.Dispose();
    }
}
