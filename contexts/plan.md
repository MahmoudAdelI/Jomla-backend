I'll create a comprehensive project plan document for you. Since I cannot directly generate a PDF file, I'll provide you with a detailed markdown document that you can easily convert to PDF using tools like:

- **VS Code** with "Markdown PDF" extension
- **Pandoc** (command line)
- **Online converters** like markdown-pdf.com
- **Print to PDF** from any browser

Here's your detailed project plan:

---

# **Jomla - Graduation Project Development Plan**

**MVP Development Sprint Plan (20 Days)**  
**Team: 5 Developers**  
**Timeline: June 10-30, 2026**

---

## **Table of Contents**

1. Executive Summary
2. Team Roles & Responsibilities
3. Technology Stack & Architecture
4. Sprint Overview
5. Detailed Sprint Plans
   - Sprint 1: Foundation & Seller Flow (Days 1-5)
   - Sprint 2: Buyer Flow & AI Integration (Days 6-10)
   - Sprint 3: Advanced Logic & Real-time (Days 11-15)
   - Sprint 4: Polish & Demo Prep (Days 16-20)
6. Daily Standup Template
7. Risk Management
8. Definition of Done
9. Demo Script Outline

---

## **1. Executive Summary**

**Project:** Jomla - B2B Group Buying Marketplace  
**Deadline:** 20 days from June 10, 2026  
**Team Size:** 5 developers  
**Methodology:** Agile-inspired sprints with daily standups

### **Critical Success Factors**

- ✅ Strict feature freeze on Day 16
- ✅ API contracts defined before parallel development
- ✅ Mock external services (Stripe, AI) for stability
- ✅ Daily integration and testing
- ✅ Backup demo video recorded by Day 18

---

## **2. Team Roles & Responsibilities**

### **You (Team Lead / Full-Stack)**

**Primary Domains:** Authentication, Orders, Architecture

- Project scaffolding and Clean Architecture setup
- JWT authentication and authorization
- Order creation pipeline (both flows)
- Payment integration (Mock Stripe)
- Code reviews and merging
- Unblock team members

### **Mohammed Faress (Backend - Seller Flow)**

**Primary Domains:** Seller Offers, Batches, Background Jobs

- Seller offers CRUD with CQRS
- Batch lifecycle management
- Hangfire jobs for expiry and batch completion
- Batch participant validation logic

### **Sarah (Frontend - Seller & Core UI)**

**Primary Domains:** Angular Setup, Seller Dashboard, Real-time UI

- Angular workspace and component architecture
- Seller offer creation and management UI
- Batch progress visualization with SignalR
- Dual-theme implementation (light/dark)

### **Nour (Backend + AI - Buyer Demand)**

**Primary Domains:** Group Requests, AI Categorization, Notifications

- Group request CRUD with CQRS
- AI Categorization Agent integration
- Seller notification matching logic
- Hangfire jobs for hub inactivity

### **Defrawy (Full-Stack - Offers & Negotiation)**

**Primary Domains:** Group Request Offers, AI Negotiation, Buyer UI

- Group request offers CRUD
- AI Negotiation Agent implementation
- Buyer offer response system
- Buyer "Discover" page and offer voting UI

---

## **3. Technology Stack & Architecture**

### **Backend Technologies**

| Component       | Technology                   | Purpose                                             |
| --------------- | ---------------------------- | --------------------------------------------------- |
| Framework       | ASP.NET Core 8               | Web API foundation                                  |
| ORM             | EF Core 8                    | Database access and migrations                      |
| Database        | SQL Server 2022              | Relational data storage                             |
| Architecture    | Clean Architecture           | Separation of concerns                              |
| Pattern         | CQRS + MediatR               | Command-query separation                            |
| Validation      | FluentValidation             | Request validation                                  |
| Mapping         | AutoMapper                   | DTO ↔ Entity mapping                                |
| Background Jobs | Hangfire                     | Scheduled tasks and retries                         |
| Real-time       | SignalR                      | Live updates and notifications                      |
| AI Integration  | Semantic Kernel / OpenAI API | AI agents (categorization, negotiation, moderation) |
| Error Handling  | ProblemDetails               | RFC 7807 compliant errors                           |
| Documentation   | Swagger/OpenAPI              | API documentation                                   |

### **Frontend Technologies**

| Component   | Technology                  | Purpose                     |
| ----------- | --------------------------- | --------------------------- |
| Framework   | Angular 17+                 | SPA framework               |
| Components  | Standalone Components       | Modern Angular architecture |
| State       | Signals                     | Reactive state management   |
| Real-time   | @microsoft/signalr          | WebSocket client            |
| UI Library  | Angular Material / Tailwind | Component library           |
| HTTP Client | Angular HttpClient + RxJS   | API communication           |
| Routing     | Angular Router              | Navigation and guards       |
| Forms       | Reactive Forms              | Complex form handling       |

### **DevOps & Tools**

| Tool                         | Purpose                           |
| ---------------------------- | --------------------------------- |
| Git + GitHub                 | Version control and collaboration |
| GitHub Projects              | Task board and sprint tracking    |
| Postman / Insomnia           | API testing                       |
| SQL Server Management Studio | Database management               |
| Visual Studio 2022 / Rider   | Backend IDE                       |
| VS Code                      | Frontend IDE                      |

---

## **4. Sprint Overview**

```
┌─────────────────────────────────────────────────────────────┐
│                    20-DAY TIMELINE                          │
├─────────────┬─────────────┬─────────────┬─────────────┬─────┤
│   SPRINT 1  │   SPRINT 2  │   SPRINT 3  │   SPRINT 4  │DEMO │
│  Days 1-5   │  Days 6-10  │ Days 11-15  │ Days 16-20  │     │
├─────────────┼─────────────┼─────────────┼─────────────┼─────
│ Foundation  │ Buyer Flow  │ Advanced    │ Polish &    │Final│
│ + Seller    │ + AI        │ Logic       │ Testing     │Demo │
│ Flow        │ Integration │ + Real-time │             │     │
└─────────────┴─────────────┴─────────────┴─────────────┴─────┘
```

---

## **5. Detailed Sprint Plans**

---

### **SPRINT 1: Foundation & Seller Flow**

**Duration:** Days 1-5 (June 10-14)  
**Goal:** Working backend with auth, seller offers, and batch creation. Angular app connected.

---

#### **Day 1-2 Tasks**

**TASK 1.1: Project Scaffolding**  
**Assigned to:** You (Team Lead)  
**Priority:** 🔴 Critical  
**Estimated Time:** 6 hours

**Description:**
Set up the Clean Architecture solution structure with all necessary projects and dependencies.

**Techniques & Technologies:**

```bash
# Create solution structure
dotnet new sln -n Jomla
dotnet new classlib -n Jomla.Domain
dotnet new classlib -n Jomla.Application
dotnet new webapi -n Jomla.API
dotnet new classlib -n Jomla.Infrastructure

# Add project references
dotnet sln add Jomla.API/Jomla.API.csproj
dotnet sln add Jomla.Domain/Jomla.Domain.csproj
dotnet sln add Jomla.Application/Jomla.Application.csproj
dotnet sln add Jomla.Infrastructure/Jomla.Infrastructure.csproj

# Add NuGet packages
dotnet add Jomla.Application package MediatR
dotnet add Jomla.Application package FluentValidation
dotnet add Jomla.Application package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add Jomla.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Jomla.Infrastructure package Microsoft.EntityFrameworkCore.Tools
dotnet add Jomla.Infrastructure package Hangfire
dotnet add Jomla.Infrastructure package Hangfire.SqlServer
dotnet add Jomla.API package Swashbuckle.AspNetCore
dotnet add Jomla.API package Microsoft.AspNetCore.Authentication.JwtBearer
```

**Folder Structure:**

```
Jomla/
├── src/
│   ├── Jomla.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Common/ (BaseEntity, AuditableEntity)
│   │   └── Jomla.Domain.csproj
│   ├── Jomla.Application/
│   │   ├── Common/Interfaces/
│   │   ├── Common/Behaviours/
│   │   ├── Common/Exceptions/
│   │   ├── Common/Models/
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   ├── SellerOffers/
│   │   │   ├── Batches/
│   │   │   └── GroupRequests/
│   │   └── Jomla.Application.csproj
│   ├── Jomla.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── DateTimeService.cs
│   │   │   ├── JwtService.cs
│   │   │   └── MockStripeService.cs
│   │   ├── Hangfire/
│   │   └── Jomla.Infrastructure.csproj
│   └── Jomla.API/
│       ├── Controllers/
│       ├── Middleware/
│       ├── Extensions/
│       ├── appsettings.json
│       └── Jomla.API.csproj
```

**Acceptance Criteria:**

- [ ] Solution builds without errors
- [ ] All projects reference each other correctly
- [ ] Swagger endpoint accessible at `/swagger`
- [ ] Health check endpoint returns 200 OK

---

**TASK 1.2: Database Setup & EF Core Configuration**  
**Assigned to:** You + Mohammed  
**Priority:** 🔴 Critical  
**Estimated Time:** 8 hours

**Description:**
Implement EF Core DbContext, entity configurations, and initial migration.

**Techniques & Technologies:**

**1. Base Entity Classes:**

```csharp
// Jomla.Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

// Jomla.Domain/Common/AuditableEntity.cs
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
```

**2. Entity Example (User):**

```csharp
// Jomla.Domain/Entities/User.cs
public class User : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // Navigation properties
    public ICollection<SellerOffer> SellerOffers { get; set; } = new List<SellerOffer>();
    public ICollection<BatchParticipant> BatchParticipants { get; set; } = new List<BatchParticipant>();
    public ICollection<GroupRequest> GroupRequests { get; set; } = new List<GroupRequest>();
}

// Jomla.Domain/Enums/UserRole.cs
public enum UserRole
{
    Seller,
    Buyer
}
```

**3. EF Core Configuration:**

```csharp
// Jomla.Infrastructure/Persistence/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("NEWID()");

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(10)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
    }
}
```

**4. DbContext:**

```csharp
// Jomla.Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<SellerOffer> SellerOffers => Set<SellerOffer>();
    public DbSet<SellerBatch> SellerBatches => Set<SellerBatch>();
    public DbSet<BatchParticipant> BatchParticipants => Set<BatchParticipant>();
    public DbSet<GroupRequest> GroupRequests => Set<GroupRequest>();
    public DbSet<GroupRequestParticipant> GroupRequestParticipants => Set<GroupRequestParticipant>();
    public DbSet<Category> Categories => Set<Category>();
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters for soft delete if needed
        // modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = "system"; // TODO: Get from user context
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModified = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = "system";
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

**5. Migration Commands:**

```bash
# Add initial migration
dotnet ef migrations add InitialCreate --project Jomla.Infrastructure --startup-project Jomla.API

# Update database
dotnet ef database update --project Jomla.Infrastructure --startup-project Jomla.API

# Script migration for production
dotnet ef migrations script --project Jomla.Infrastructure --startup-project Jomla.API -o migrations.sql
```

**Acceptance Criteria:**

- [ ] All entities from schema are implemented
- [ ] Configurations use SQL Server naming conventions
- [ ] Initial migration created successfully
- [ ] Database created and tables match schema
- [ ] Seed data for categories and test users

---

**TASK 1.3: JWT Authentication Setup**  
**Assigned to:** You (Team Lead)  
**Priority:** 🔴 Critical  
**Estimated Time:** 6 hours

**Description:**
Implement JWT-based authentication with role-based authorization.

**Techniques & Technologies:**

**1. JWT Configuration:**

```csharp
// appsettings.json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "JomlaAPI",
    "Audience": "JomlaClient",
    "ExpirationInMinutes": 60
  }
}
```

**2. JWT Service:**

```csharp
// Jomla.Application/Common/Interfaces/IJwtService.cs
public interface IJwtService
{
    string GenerateToken(User user);
}

// Jomla.Infrastructure/Services/JwtService.cs
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**3. Auth Controller:**

```csharp
// Jomla.API/Controllers/AuthController.cs
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginQuery command)
    {
        return Ok(await _mediator.Send(command));
    }
}
```

**4. CQRS Commands:**

```csharp
// Jomla.Application/Features/Auth/Commands/RegisterCommand.cs
public record RegisterCommand(
    string Name,
    string Email,
    string Password,
    UserRole Role
) : IRequest<AuthResponse>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        // Check if user exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, ct))
            throw new ValidationException("Email already registered");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        var token = _jwtService.GenerateToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Token = token,
            Role = user.Role
        };
    }
}
```

**5. Program.cs Configuration:**

```csharp
// Jomla.API/Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])
        )
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller"));
    options.AddPolicy("BuyerOnly", policy => policy.RequireRole("Buyer"));
});
```

**Acceptance Criteria:**

- [ ] User can register with email/password
- [ ] User can login and receive JWT token
- [ ] Token includes user ID, email, and role
- [ ] Protected endpoints require valid token
- [ ] Role-based authorization works (Seller/Buyer)

---

**TASK 1.4: Seller Offer CQRS Implementation**  
**Assigned to:** Mohammed  
**Priority:** 🔴 Critical  
**Estimated Time:** 10 hours

**Description:**
Implement CreateSellerOffer, GetSellerOffers, UpdateSellerOffer commands/queries.

**Techniques & Technologies:**

**1. Entity Definition:**

```csharp
// Jomla.Domain/Entities/SellerOffer.cs
public class SellerOffer : AuditableEntity
{
    public Guid SellerId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrls { get; set; } // JSON array
    public decimal UnitPrice { get; set; }
    public int HubTargetQuantity { get; set; }
    public int TotalQuantityAvailable { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? ExpiryFallbackThreshold { get; set; }
    public string? VariantAttributes { get; set; } // JSON object
    public SellerOfferStatus Status { get; set; } = SellerOfferStatus.Active;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    public string? ModerationReason { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    public User Seller { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<SellerBatch> Batches { get; set; } = new List<SellerBatch>();
}
```

**2. DTOs:**

```csharp
// Jomla.Application/Features/SellerOffers/DTOs/SellerOfferDto.cs
public class SellerOfferDto
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public decimal UnitPrice { get; set; }
    public int HubTargetQuantity { get; set; }
    public int TotalQuantityAvailable { get; set; }
    public int CurrentAvailableQuantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? ExpiryFallbackThreshold { get; set; }
    public Dictionary<string, string>? VariantAttributes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveBatchNumber { get; set; }
}

// Jomla.Application/Features/SellerOffers/Commands/CreateSellerOfferCommand.cs
public record CreateSellerOfferCommand(
    Guid CategoryId,
    string Title,
    string? Description,
    List<string> ImageUrls,
    decimal UnitPrice,
    int HubTargetQuantity,
    int TotalQuantityAvailable,
    decimal DiscountPercent,
    int? ExpiryFallbackThreshold,
    Dictionary<string, string>? VariantAttributes,
    DateTime ExpiresAt
) : IRequest<Guid>;
```

**3. Validation:**

```csharp
// Jomla.Application/Features/SellerOffers/Commands/CreateSellerOfferCommandValidator.cs
public class CreateSellerOfferCommandValidator : AbstractValidator<CreateSellerOfferCommand>
{
    public CreateSellerOfferCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Unit price is too high");

        RuleFor(x => x.HubTargetQuantity)
            .InclusiveBetween(1, 10000).WithMessage("Target quantity must be between 1 and 10000");

        RuleFor(x => x.TotalQuantityAvailable)
            .InclusiveBetween(1, 100000).WithMessage("Total quantity must be between 1 and 100000");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount must be between 0 and 100");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiry date must be in the future");

        RuleFor(x => x)
            .Must(x => x.TotalQuantityAvailable >= x.HubTargetQuantity)
            .WithMessage("Total quantity must be at least the target quantity");
    }
}
```

**4. Command Handler:**

```csharp
// Jomla.Application/Features/SellerOffers/Commands/CreateSellerOfferCommandHandler.cs
public class CreateSellerOfferCommandHandler : IRequestHandler<CreateSellerOfferCommand, Guid>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public async Task<Guid> Handle(CreateSellerOfferCommand request, CancellationToken ct)
    {
        var sellerId = Guid.Parse(_currentUserService.UserId);

        // Verify seller exists and owns this account
        var seller = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == sellerId && u.Role == UserRole.Seller, ct);

        if (seller == null)
            throw new ForbiddenAccessException("Only sellers can create offers");

        // Verify category exists
        var category = await _context.Categories.FindAsync(new object[] { request.CategoryId }, ct);
        if (category == null)
            throw new NotFoundException(nameof(Category), request.CategoryId);

        var offer = new SellerOffer
        {
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            ImageUrls = JsonSerializer.Serialize(request.ImageUrls),
            UnitPrice = request.UnitPrice,
            HubTargetQuantity = request.HubTargetQuantity,
            TotalQuantityAvailable = request.TotalQuantityAvailable,
            DiscountPercent = request.DiscountPercent,
            ExpiryFallbackThreshold = request.ExpiryFallbackThreshold,
            VariantAttributes = request.VariantAttributes != null
                ? JsonSerializer.Serialize(request.VariantAttributes)
                : null,
            ExpiresAt = request.ExpiresAt,
            Status = SellerOfferStatus.Active,
            ModerationStatus = ModerationStatus.Pending // AI moderation will update this
        };

        _context.SellerOffers.Add(offer);
        await _context.SaveChangesAsync(ct);

        // TODO: Trigger AI Moderation Agent asynchronously

        return offer.Id;
    }
}
```

**5. AutoMapper Profile:**

```csharp
// Jomla.Application/Common/Mappings/MappingProfile.cs
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SellerOffer, SellerOfferDto>()
            .ForMember(d => d.SellerName, opt => opt.MapFrom(s => s.Seller.Name))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name))
            .ForMember(d => d.ImageUrls, opt => opt.MapFrom(
                s => string.IsNullOrEmpty(s.ImageUrls)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(s.ImageUrls)))
            .ForMember(d => d.VariantAttributes, opt => opt.MapFrom(
                s => string.IsNullOrEmpty(s.VariantAttributes)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(s.VariantAttributes)))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ModerationStatus, opt => opt.MapFrom(s => s.ModerationStatus.ToString()))
            .ForMember(d => d.CurrentAvailableQuantity, opt =>
                opt.MapFrom(s => CalculateAvailableQuantity(s)));
    }

    private static int CalculateAvailableQuantity(SellerOffer offer)
    {
        var completedBatchesQuantity = offer.Batches
            .Where(b => b.Status == SellerBatchStatus.Completed)
            .Sum(b => b.TargetQuantity);

        return offer.TotalQuantityAvailable - completedBatchesQuantity;
    }
}
```

**Acceptance Criteria:**

- [ ] Seller can create an offer with all required fields
- [ ] Validation rules enforced
- [ ] Offer saved with moderation_status = 'pending'
- [ ] Get offers returns paginated list with seller and category names
- [ ] Seller can only see/edit their own offers
- [ ] Unit tests for command handler

---

**TASK 1.5: Angular Project Setup**  
**Assigned to:** Sarah  
**Priority:** 🔴 Critical  
**Estimated Time:** 6 hours

**Description:**
Initialize Angular workspace with standalone components, routing, and core services.

**Techniques & Technologies:**

**1. Angular CLI Commands:**

```bash
# Create new Angular project
ng new jomla-frontend --standalone --routing --style=scss --ssr=false

cd jomla-frontend

# Install dependencies
npm install @angular/material @angular/cdk
npm install @microsoft/signalr
npm install ngx-toastr
npm install @ngrx/signals  # Optional for state management
npm install prettier eslint --save-dev

# Generate core structure
ng generate service core/services/auth
ng generate service core/services/api
ng generate service core/services/signalr
ng generate interceptor core/interceptors/auth
ng generate guard core/guards/auth
ng generate component features/auth/login
ng generate component features/auth/register
ng generate component features/seller/dashboard
ng generate component shared/components/navbar
ng generate component shared/components/footer
```

**2. Project Structure:**

```
jomla-frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── services/
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── api.service.ts
│   │   │   │   ├── signalr.service.ts
│   │   │   │   └── toast.service.ts
│   │   │   ├── interceptors/
│   │   │   │   └── auth.interceptor.ts
│   │   │   ├── guards/
│   │   │   │   ├── auth.guard.ts
│   │   │   │   └── role.guard.ts
│   │   │   └── core.module.ts
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   ├── login/
│   │   │   │   ├── register/
│   │   │   │   └── auth.routes.ts
│   │   │   ├── seller/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── create-offer/
│   │   │   │   └── seller.routes.ts
│   │   │   └── buyer/
│   │   │       ├── discover/
│   │   │       └── buyer.routes.ts
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   ├── directives/
│   │   │   ├── pipes/
│   │   │   └── models/
│   │   │       ├── user.model.ts
│   │   │       ├── offer.model.ts
│   │   │       └── api-response.model.ts
│   │   ├── app.component.ts
│   │   ├── app.routes.ts
│   │   └── app.config.ts
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.prod.ts
│   └── assets/
└── angular.json
```

**3. Core Services:**

```typescript
// src/app/core/services/auth.service.ts
import { Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { environment } from "../../../environments/environment";

export interface User {
  userId: string;
  email: string;
  name: string;
  role: "Seller" | "Buyer";
}

export interface AuthResponse {
  userId: string;
  email: string;
  token: string;
  role: string;
}

@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly tokenKey = "jomla_token";
  private readonly userKey = "jomla_user";

  // Signals for reactive state
  readonly currentUser = signal<User | null>(null);
  readonly isAuthenticated = signal(false);

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {
    this.loadUserFromStorage();
  }

  login(email: string, password: string) {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, { email, password })
      .pipe(tap((response) => this.setAuth(response)));
  }

  register(
    name: string,
    email: string,
    password: string,
    role: "Seller" | "Buyer",
  ) {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, {
        name,
        email,
        password,
        role,
      })
      .pipe(tap((response) => this.setAuth(response)));
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(["/auth/login"]);
  }

  private setAuth(response: AuthResponse) {
    localStorage.setItem(this.tokenKey, response.token);
    const user: User = {
      userId: response.userId,
      email: response.email,
      name: response.email.split("@")[0], // TODO: Get from response
      role: response.role as "Seller" | "Buyer",
    };
    localStorage.setItem(this.userKey, JSON.stringify(user));
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
  }

  private loadUserFromStorage() {
    const token = localStorage.getItem(this.tokenKey);
    const userJson = localStorage.getItem(this.userKey);

    if (token && userJson) {
      const user = JSON.parse(userJson);
      this.currentUser.set(user);
      this.isAuthenticated.set(true);
    }
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }
}
```

**4. API Service with Error Handling:**

```typescript
// src/app/core/services/api.service.ts
import { Injectable, inject } from "@angular/core";
import {
  HttpClient,
  HttpErrorResponse,
  HttpParams,
} from "@angular/common/http";
import { catchError, throwError } from "rxjs";
import { ToastService } from "./toast.service";

export interface PaginatedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: "root" })
export class ApiService {
  private http = inject(HttpClient);
  private toast = inject(ToastService);
  private baseUrl = environment.apiUrl;

  get<T>(endpoint: string, params?: any) {
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach((key) => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key]);
        }
      });
    }

    return this.http
      .get<T>(`${this.baseUrl}/${endpoint}`, { params })
      .pipe(catchError(this.handleError));
  }

  post<T>(endpoint: string, body: any) {
    return this.http
      .post<T>(`${this.baseUrl}/${endpoint}`, body)
      .pipe(catchError(this.handleError));
  }

  put<T>(endpoint: string, body: any) {
    return this.http
      .put<T>(`${this.baseUrl}/${endpoint}`, body)
      .pipe(catchError(this.handleError));
  }

  delete<T>(endpoint: string) {
    return this.http
      .delete<T>(`${this.baseUrl}/${endpoint}`)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = "An unknown error occurred";

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage =
        error.error?.title ||
        `Error Code: ${error.status}\nMessage: ${error.message}`;
    }

    this.toast.showError(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
```

**5. Environment Configuration:**

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: "https://localhost:7001/api",
  signalRUrl: "https://localhost:7001/hubs",
};

// src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiUrl: "https://api.jomla.com/api",
  signalRUrl: "https://api.jomla.com/hubs",
};
```

**6. App Configuration:**

```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideZoneChangeDetection } from "@angular/core";
import { provideRouter } from "@angular/router";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideAnimations } from "@angular/platform-browser/animations";
import { routes } from "./app.routes";
import { authInterceptor } from "./core/interceptors/auth.interceptor";

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations(),
  ],
};
```

**Acceptance Criteria:**

- [ ] Angular app builds and runs successfully
- [ ] Routing configured for auth and feature modules
- [ ] Auth service with login/register/logout
- [ ] HTTP interceptor adds JWT token to requests
- [ ] Error handling displays toast notifications
- [ ] Responsive layout with navbar

---

#### **Day 3-5 Tasks**

**TASK 1.6: Seller Batch Management**  
**Assigned to:** Mohammed  
**Priority:** 🟡 High  
**Estimated Time:** 8 hours

**Description:**
Implement batch creation, joining, and completion logic.

**Techniques & Technologies:**

**Key Business Logic:**

```csharp
// Jomla.Application/Features/Batches/Commands/JoinBatchCommandHandler.cs
public class JoinBatchCommandHandler : IRequestHandler<JoinBatchCommand>
{
    public async Task Handle(JoinBatchCommand request, CancellationToken ct)
    {
        var batch = await _context.SellerBatches
            .Include(b => b.Offer)
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == request.BatchId, ct);

        if (batch == null)
            throw new NotFoundException(nameof(SellerBatch), request.BatchId);

        if (batch.Status != SellerBatchStatus.Open)
            throw new BadRequestException("Batch is not open for joining");

        var remainingSlots = batch.TargetQuantity - batch.CurrentQuantity;

        if (request.Quantity > remainingSlots)
            throw new BadRequestException($"Only {remainingSlots} slots remaining");

        // Check if buyer already joined
        var existingParticipant = batch.Participants
            .FirstOrDefault(p => p.BuyerId == request.BuyerId);

        if (existingParticipant != null)
        {
            if (existingParticipant.Status == BatchParticipantStatus.Left)
            {
                // Rejoin - reuse existing row
                existingParticipant.Status = BatchParticipantStatus.Joined;
                existingParticipant.Quantity = request.Quantity;
                existingParticipant.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                throw new BadRequestException("Already joined this batch");
            }
        }
        else
        {
            // New participant
            var participant = new BatchParticipant
            {
                BatchId = batch.Id,
                BuyerId = request.BuyerId,
                Quantity = request.Quantity,
                Status = BatchParticipantStatus.Joined
            };
            batch.Participants.Add(participant);
        }

        batch.CurrentQuantity += request.Quantity;

        // Check if batch completed
        if (batch.CurrentQuantity >= batch.TargetQuantity)
        {
            batch.Status = SellerBatchStatus.Completed;
            batch.CompletedAt = DateTime.UtcNow;

            // Create orders for all participants
            await CreateOrdersForBatch(batch, ct);

            // Create new batch if offer has remaining stock
            await CreateNextBatchIfAvailable(batch.Offer, ct);
        }

        await _context.SaveChangesAsync(ct);

        // SignalR: Notify batch progress update
        await _signalRService.SendBatchUpdate(batch.Id, batch.CurrentQuantity, batch.TargetQuantity);
    }

    private async Task CreateOrdersForBatch(SellerBatch batch, CancellationToken ct)
    {
        var orders = batch.Participants
            .Where(p => p.Status == BatchParticipantStatus.Joined)
            .Select(p => new Order
            {
                BuyerId = p.BuyerId,
                BatchId = batch.Id,
                Quantity = p.Quantity,
                TotalAmount = batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercent / 100) * p.Quantity,
                Status = OrderStatus.Pending
            });

        _context.Orders.AddRange(orders);
    }

    private async Task CreateNextBatchIfAvailable(SellerOffer offer, CancellationToken ct)
    {
        var completedQuantity = offer.Batches
            .Where(b => b.Status == SellerBatchStatus.Completed)
            .Sum(b => b.TargetQuantity);

        var remainingStock = offer.TotalQuantityAvailable - completedQuantity;

        if (remainingStock > 0)
        {
            var newBatch = new SellerBatch
            {
                OfferId = offer.Id,
                BatchNumber = offer.Batches.Count + 1,
                TargetQuantity = Math.Min(offer.HubTargetQuantity, remainingStock),
                CurrentQuantity = 0,
                Status = SellerBatchStatus.Open
            };

            _context.SellerBatches.Add(newBatch);
        }
        else
        {
            offer.Status = SellerOfferStatus.Inactive;
        }
    }
}
```

**Acceptance Criteria:**

- [ ] Buyers can join open batches
- [ ] Validation prevents over-joining
- [ ] Batch auto-completes when target reached
- [ ] Orders created for all participants
- [ ] New batch opens automatically if stock remains
- [ ] SignalR notifies clients of progress

---

**TASK 1.7: Seller Dashboard UI**  
**Assigned to:** Sarah  
**Priority:** 🟡 High  
**Estimated Time:** 8 hours

**Description:**
Build seller dashboard with offer list, create offer form, and batch progress.

**Techniques & Technologies:**

**1. Create Offer Component:**

```typescript
// src/app/features/seller/create-offer/create-offer.component.ts
import { Component, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { Router } from "@angular/router";
import { ApiService } from "../../../core/services/api.service";
import { ToastService } from "../../../core/services/toast.service";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatNativeDateModule } from "@angular/material/core";

@Component({
  selector: "app-create-offer",
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
  ],
  templateUrl: "./create-offer.component.html",
  styleUrls: ["./create-offer.component.scss"],
})
export class CreateOfferComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);

  offerForm: FormGroup;
  isSubmitting = false;
  categories: any[] = [];

  constructor() {
    this.offerForm = this.fb.group({
      categoryId: ["", Validators.required],
      title: ["", [Validators.required, Validators.maxLength(255)]],
      description: [""],
      imageUrls: [[]],
      unitPrice: ["", [Validators.required, Validators.min(0.01)]],
      hubTargetQuantity: [
        "",
        [Validators.required, Validators.min(1), Validators.max(10000)],
      ],
      totalQuantityAvailable: ["", [Validators.required, Validators.min(1)]],
      discountPercent: [
        "",
        [Validators.required, Validators.min(0), Validators.max(100)],
      ],
      expiryFallbackThreshold: [null],
      expiresAt: ["", Validators.required],
    });

    this.loadCategories();
  }

  async loadCategories() {
    this.categories = await this.api.get<any[]>("categories").toPromise();
  }

  async onSubmit() {
    if (this.offerForm.invalid) {
      this.offerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    try {
      const offerId = await this.api
        .post<string>("seller-offers", this.offerForm.value)
        .toPromise();
      this.toast.showSuccess("Offer created successfully!");
      this.router.navigate(["/seller/offers", offerId]);
    } catch (error) {
      this.toast.showError("Failed to create offer");
    } finally {
      this.isSubmitting = false;
    }
  }
}
```

**2. Batch Progress Component with SignalR:**

```typescript
// src/app/shared/components/batch-progress/batch-progress.component.ts
import { Component, Input, OnInit, OnDestroy } from "@angular/core";
import { SignalRService } from "../../../core/services/signalr.service";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { Subject, takeUntil } from "rxjs";

@Component({
  selector: "app-batch-progress",
  standalone: true,
  imports: [MatProgressBarModule],
  template: `
    <div class="batch-progress">
      <mat-progress-bar
        mode="determinate"
        [value]="progressPercentage"
        color="accent"
      >
      </mat-progress-bar>
      <div class="progress-info">
        <span>{{ currentQuantity }} / {{ targetQuantity }} units</span>
        <span>{{ progressPercentage | number: "1.0-0" }}%</span>
      </div>
      <div *ngIf="remainingSlots > 0" class="remaining-slots">
        {{ remainingSlots }} slots remaining
      </div>
    </div>
  `,
})
export class BatchProgressComponent implements OnInit, OnDestroy {
  @Input() batchId!: string;
  @Input() currentQuantity!: number;
  @Input() targetQuantity!: number;

  progressPercentage = 0;
  remainingSlots = 0;

  private destroy$ = new Subject<void>();

  constructor(private signalR: SignalRService) {}

  ngOnInit() {
    this.calculateProgress();
    this.subscribeToBatchUpdates();
  }

  calculateProgress() {
    this.progressPercentage =
      (this.currentQuantity / this.targetQuantity) * 100;
    this.remainingSlots = this.targetQuantity - this.currentQuantity;
  }

  subscribeToBatchUpdates() {
    this.signalR
      .onBatchUpdated(this.batchId)
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => {
        this.currentQuantity = data.currentQuantity;
        this.calculateProgress();
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
```

**Acceptance Criteria:**

- [ ] Form validation works correctly
- [ ] Offer creation integrates with backend
- [ ] Batch progress updates in real-time via SignalR
- [ ] Responsive design for mobile/desktop
- [ ] Loading states and error handling

---

**SPRINT 1 DELIVERABLES CHECKLIST:**

- [ ] Backend API running with Swagger
- [ ] Database created with all tables
- [ ] JWT authentication working
- [ ] Seller can create offers (API + UI)
- [ ] Buyer can join batches (API)
- [ ] Angular app connected to backend
- [ ] Basic seller dashboard functional
- [ ] Code reviewed and merged to main

---

### **SPRINT 2: Buyer Flow & AI Integration**

**Duration:** Days 6-10 (June 15-19)  
**Goal:** Buyer-initiated Wish Hub flow working. AI categorization agent integrated.

---

#### **Day 6-7 Tasks**

**TASK 2.1: Group Request CQRS**  
**Assigned to:** Nour  
**Priority:** 🔴 Critical  
**Estimated Time:** 10 hours

**Description:**
Implement CreateGroupRequest, JoinGroupRequest commands with AI categorization trigger.

**Techniques & Technologies:**

**1. AI Categorization Agent:**

```csharp
// Jomla.Application/Features/GroupRequests/Services/AICategorizationAgent.cs
public class AICategorizationAgent : IAICategorizationAgent
{
    private readonly ILogger<AICategorizationAgent> _logger;
    private readonly IOpenAIClient _openAIClient; // Or Semantic Kernel

    public async Task<CategoryMatchResult> CategorizeAsync(string itemTitle, CancellationToken ct)
    {
        // Using OpenAI API directly
        var prompt = $@"
You are a product categorization expert. Given a product title, determine the most appropriate category from the list below.

Product Title: ""{itemTitle}""

Available Categories:
- Electronics > Computers > Laptops
- Electronics > Computers > Desktops
- Electronics > Phones > Smartphones
- Home & Kitchen > Furniture > Chairs
- Home & Kitchen > Appliances > Refrigerators
- Fashion > Men > Shoes
- Fashion > Women > Dresses
[... more categories ...]

Return ONLY the category path and confidence score (0-100) in JSON format:
{{
  ""categoryPath"": ""Electronics > Computers > Laptops"",
  ""confidence"": 85,
  ""alternativeCategories"": [
    {{ ""categoryPath"": ""Electronics > Computers > Desktops"", ""confidence"": 45 }}
  ]
}}";

        var response = await _openAIClient.Chat.CompleteAsync(
            messages: new[] { new ChatMessage("user", prompt) },
            options: new ChatCompletionOptions { Temperature = 0.1 },
            cancellationToken: ct
        );

        var result = JsonSerializer.Deserialize<CategoryMatchResult>(
            response.Choices[0].Message.Content);

        return result;
    }
}
```

**2. Create Group Request Handler:**

```csharp
// Jomla.Application/Features/GroupRequests/Commands/CreateGroupRequestCommandHandler.cs
public class CreateGroupRequestCommandHandler : IRequestHandler<CreateGroupRequestCommand, Guid>
{
    private readonly IAICategorizationAgent _aiAgent;
    private readonly ApplicationDbContext _context;

    public async Task<Guid> Handle(CreateGroupRequestCommand request, CancellationToken ct)
    {
        // Step 1: AI categorization
        var categorization = await _aiAgent.CategorizeAsync(request.ItemTitle, ct);

        Category? category = null;
        if (categorization.Confidence >= 70)
        {
            category = await FindCategoryByPath(categorization.CategoryPath, ct);
        }

        // If low confidence, use default "Uncategorized" or ask buyer to select
        if (category == null)
        {
            category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == "Uncategorized", ct);
        }

        // Step 2: Create group request
        var groupRequest = new GroupRequest
        {
            InitiatorBuyerId = request.BuyerId,
            CategoryId = category.Id,
            ItemTitle = request.ItemTitle,
            CurrentQuantity = request.Quantity, // Starts with initiator's quantity
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Pending
        };

        _context.GroupRequests.Add(groupRequest);
        await _context.SaveChangesAsync(ct);

        // Step 3: Create initiator's participant record
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = request.BuyerId,
            Quantity = request.Quantity,
            Status = GroupRequestParticipantStatus.Active
        };

        _context.GroupRequestParticipants.Add(participant);
        await _context.SaveChangesAsync(ct);

        // Step 4: Trigger AI moderation asynchronously
        _ = Task.Run(async () => await ModerateGroupRequest(groupRequest.Id));

        return groupRequest.Id;
    }
}
```

**Acceptance Criteria:**

- [ ] Buyer can create group request with free-text title
- [ ] AI categorizes the item automatically
- [ ] Participant record created for initiator
- [ ] Current quantity starts at initiator's quantity
- [ ] Moderation triggered asynchronously

---

**TASK 2.2: Seller Notification Matching**  
**Assigned to:** Nour  
**Priority:** 🟡 High  
**Estimated Time:** 6 hours

**Description:**
Implement notification matching query when group request quantity grows.

**Techniques & Technologies:**

```csharp
// Jomla.Application/Features/Notifications/Services/NotificationMatchingService.cs
public class NotificationMatchingService : INotificationMatchingService
{
    public async Task MatchAndNotifySellers(Guid groupRequestId, CancellationToken ct)
    {
        var groupRequest = await _context.GroupRequests
            .Include(gr => gr.Category)
            .FirstOrDefaultAsync(gr => gr.Id == groupRequestId, ct);

        if (groupRequest == null) return;

        // Find sellers who:
        // 1. Have this category in their preferences
        // 2. Have min_quantity <= current_quantity
        // 3. Haven't been notified for this group request yet

        var matchingSellers = await (from scp in _context.SellerCategoryPreferences
                                     join u in _context.Users on scp.SellerId equals u.Id
                                     where scp.CategoryId == groupRequest.CategoryId
                                           && scp.MinQuantity <= groupRequest.CurrentQuantity
                                           && scp.IsActive
                                           && u.Role == UserRole.Seller
                                           && !_context.SellerNotifications.Any(sn =>
                                               sn.GroupRequestId == groupRequest.Id &&
                                               sn.SellerId == scp.SellerId)
                                     select new
                                     {
                                         SellerId = scp.SellerId,
                                         SellerName = u.Name,
                                         SellerEmail = u.Email
                                     })
                                     .ToListAsync(ct);

        // Create notification records
        var notifications = matchingSellers.Select(s => new SellerNotification
        {
            GroupRequestId = groupRequest.Id,
            SellerId = s.SellerId,
            Status = SellerNotificationStatus.Pending
        });

        _context.SellerNotifications.AddRange(notifications);
        await _context.SaveChangesAsync(ct);

        // TODO: Send email/push notification to sellers
        // TODO: SignalR notification to seller dashboard
    }
}
```

**Acceptance Criteria:**

- [ ] Sellers notified when quantity reaches their min_quantity threshold
- [ ] No duplicate notifications for same seller + group request
- [ ] Notification created in database
- [ ] Real-time notification via SignalR

---

#### **Day 8-10 Tasks**

**TASK 2.3: Group Request Offers**  
**Assigned to:** Defrawy  
**Priority:** 🟡 High  
**Estimated Time:** 8 hours

**Description:**
Implement seller creating offers for group requests.

**Key Features:**

- Seller submits price, quantity available, min_unit_price (private)
- Offer visible to all buyers in the group request
- Buyers can accept/reject

**Acceptance Criteria:**

- [ ] Seller can create offer for group request
- [ ] min_unit_price stored but never exposed to buyers
- [ ] Buyers see current_unit_price
- [ ] Offer has expiry date

---

**TASK 2.4: Buyer Discover Page**  
**Assigned to:** Defrawy + Sarah  
**Priority:** 🟡 High  
**Estimated Time:** 8 hours

**Description:**
Angular page showing active group requests and seller offers.

**Acceptance Criteria:**

- [ ] List view of active group requests
- [ ] Filter by category
- [ ] Show current quantity and participant count
- [ ] Click to view details and join

---

**SPRINT 2 DELIVERABLES CHECKLIST:**

- [ ] Group request creation with AI categorization
- [ ] Seller notification matching working
- [ ] Sellers can submit offers to group requests
- [ ] Buyer discover page functional
- [ ] AI agent logs categorization decisions

---

### **SPRINT 3: Advanced Logic & Real-time**

**Duration:** Days 11-15 (June 20-24)  
**Goal:** AI negotiation, Hangfire jobs, real-time updates, payment flow.

---

#### **Day 11-13 Tasks**

**TASK 3.1: AI Negotiation Agent**  
**Assigned to:** Defrawy  
**Priority:** 🔴 Critical  
**Estimated Time:** 12 hours

**Description:**
Implement AI agent that lowers price based on buyer rejections.

**Techniques & Technologies:**

```csharp
// Jomla.Application/Features/Offers/Services/AINEgotiationAgent.cs
public class AINEgotiationAgent : IAINegotiationAgent
{
    public async Task NegotiateAsync(Guid offerId, CancellationToken ct)
    {
        var offer = await _context.GroupRequestOffers
            .Include(o => o.GroupRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);

        if (offer == null || offer.Status != GroupRequestOfferStatus.Open)
            return;

        // Get rejection count and acceptance rate
        var responses = await _context.BuyerOfferResponses
            .Where(r => r.OfferId == offerId)
            .ToListAsync(ct);

        var rejectionCount = responses.Count(r => r.Response == BuyerOfferResponse.Rejected);
        var acceptanceCount = responses.Count(r => r.Response == BuyerOfferResponse.Accepted);

        // Trigger negotiation if rejection threshold met (e.g., 3 rejections)
        if (rejectionCount < 3) return;

        var currentPrice = offer.CurrentUnitPrice;
        var minPrice = offer.MinUnitPrice ?? currentPrice;

        if (currentPrice <= minPrice) return; // Already at floor

        // Calculate new price using AI
        var newPrice = await CalculateOptimalPrice(offer, responses, ct);

        if (newPrice < minPrice) newPrice = minPrice;
        if (newPrice >= currentPrice) return; // No improvement

        // Create counteroffer (new row, old marked as countered)
        var counterOffer = new GroupRequestOffer
        {
            GroupRequestId = offer.GroupRequestId,
            SellerId = offer.SellerId,
            UnitPrice = offer.UnitPrice,
            MinUnitPrice = offer.MinUnitPrice,
            CurrentUnitPrice = newPrice,
            QuantityAvailable = offer.QuantityAvailable,
            MinFallbackQuantity = offer.MinFallbackQuantity,
            Status = GroupRequestOfferStatus.Open,
            ExpiresAt = offer.ExpiresAt
        };

        _context.GroupRequestOffers.Add(counterOffer);

        offer.Status = GroupRequestOfferStatus.Countered;

        // Log negotiation
        _context.NegotiationLog.Add(new NegotiationLog
        {
            OfferId = offer.Id,
            PreviousPrice = currentPrice,
            NewPrice = newPrice,
            ReasoningSummary = $"Lowered price due to {rejectionCount} buyer rejections. Acceptance rate: {acceptanceCount}/{responses.Count}"
        });

        await _context.SaveChangesAsync(ct);

        // Notify buyers of new price via SignalR
        await _signalRService.SendOfferUpdate(counterOffer.Id);
    }

    private async Task<decimal> CalculateOptimalPrice(GroupRequestOffer offer, List<BuyerOfferResponse> responses, CancellationToken ct)
    {
        // Use Semantic Kernel with RAG to analyze:
        // - Historical accepted prices in this category
        // - Current quantity in group request
        // - Market price trends
        // - Rejection reasons if available

        var prompt = $@"
Analyze the following offer and suggest an optimal price reduction:

Current Price: ${offer.CurrentUnitPrice}
Minimum Price: ${offer.MinUnitPrice}
Group Request Quantity: {offer.GroupRequest.CurrentQuantity}
Rejections: {responses.Count(r => r.Response == BuyerOfferResponse.Rejected)}
Acceptances: {responses.Count(r => r.Response == BuyerOfferResponse.Accepted)}

Suggest a new price that balances seller profit with buyer acceptance likelihood.
Return ONLY the price as a decimal number.";

        var response = await _openAIClient.Chat.CompleteAsync(prompt, ct);
        return decimal.Parse(response.Choices[0].Message.Content);
    }
}
```

**Acceptance Criteria:**

- [ ] Agent triggers after rejection threshold
- [ ] New offer created with lowered price
- [ ] Old offer marked as countered
- [ ] Negotiation logged with reasoning
- [ ] Price never drops below min_unit_price
- [ ] Buyers notified of new offer

---

**TASK 3.2: Hangfire Background Jobs**  
**Assigned to:** Mohammed + Nour  
**Priority:** 🔴 Critical  
**Estimated Time:** 10 hours

**Description:**
Implement scheduled jobs for expiry checks, batch completion, and hub closure.

**Techniques & Technologies:**

```csharp
// Jomla.Infrastructure/Hangfire/Jobs/OfferExpiryJob.cs
public class OfferExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OfferExpiryJob> _logger;

    public async Task ProcessExpiredOffers()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredOffers = await context.SellerOffers
            .Include(o => o.Batches)
            .Where(o => o.Status == SellerOfferStatus.Active
                     && o.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var offer in expiredOffers)
        {
            var lastOpenBatch = offer.Batches
                .OrderByDescending(b => b.BatchNumber)
                .FirstOrDefault(b => b.Status == SellerBatchStatus.Open);

            if (lastOpenBatch != null)
            {
                if (offer.ExpiryFallbackThreshold.HasValue &&
                    lastOpenBatch.CurrentQuantity >= offer.ExpiryFallbackThreshold)
                {
                    // Complete the batch
                    lastOpenBatch.Status = SellerBatchStatus.Completed;
                    lastOpenBatch.CompletedAt = DateTime.UtcNow;

                    // Create orders
                    await CreateOrdersForBatch(lastOpenBatch);

                    _logger.LogInformation(
                        "Batch {BatchId} completed on expiry with {Quantity} units (threshold: {Threshold})",
                        lastOpenBatch.Id, lastOpenBatch.CurrentQuantity, offer.ExpiryFallbackThreshold);
                }
                else
                {
                    // Fail the batch
                    lastOpenBatch.Status = SellerBatchStatus.Failed;

                    _logger.LogWarning(
                        "Batch {BatchId} failed on expiry with {Quantity} units (threshold: {Threshold})",
                        lastOpenBatch.Id, lastOpenBatch.CurrentQuantity, offer.ExpiryFallbackThreshold ?? 0);
                }
            }

            offer.Status = SellerOfferStatus.Expired;
        }

        await context.SaveChangesAsync();
    }
}

// Jomla.Infrastructure/Hangfire/Jobs/GroupRequestClosureJob.cs
public class GroupRequestClosureJob
{
    public async Task CloseInactiveHubs()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var inactiveHubs = await context.GroupRequests
            .Where(gr => gr.Status == GroupRequestStatus.Inactive
                      && gr.InactiveSince.HasValue
                      && gr.InactiveSince.Value.AddHours(24) <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var hub in inactiveHubs)
        {
            hub.Status = GroupRequestStatus.Closed;
            _logger.LogInformation("Closed inactive group request {GroupId} after 24h", hub.Id);
        }

        await context.SaveChangesAsync();
    }
}

// Program.cs configuration
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Register recurring jobs
RecurringJob.AddOrUpdate<OfferExpiryJob>(
    "process-expired-offers",
    job => job.ProcessExpiredOffers(),
    "*/15 * * * *"); // Every 15 minutes

RecurringJob.AddOrUpdate<GroupRequestClosureJob>(
    "close-inactive-hubs",
    job => job.CloseInactiveHubs(),
    "0 * * * *"); // Every hour
```

**Acceptance Criteria:**

- [ ] Expired offers processed correctly
- [ ] Batches completed/failed based on threshold
- [ ] Inactive hubs closed after 24h
- [ ] Hangfire dashboard accessible at `/hangfire`
- [ ] Jobs logged and monitored

---

**TASK 3.3: Mock Stripe Payment**  
**Assigned to:** You (Team Lead)  
**Priority:** 🔴 Critical  
**Estimated Time:** 6 hours

**Description:**
Implement mock payment service for MVP (no real Stripe integration needed).

**Techniques & Technologies:**

```csharp
// Jomla.Application/Common/Interfaces/IPaymentService.cs
public interface IPaymentService
{
    Task<PaymentResult> CreatePaymentIntentAsync(Order order, CancellationToken ct);
    Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId, CancellationToken ct);
}

// Jomla.Infrastructure/Services/MockStripeService.cs
public class MockStripeService : IPaymentService
{
    private readonly ILogger<MockStripeService> _logger;

    public async Task<PaymentResult> CreatePaymentIntentAsync(Order order, CancellationToken ct)
    {
        // Simulate Stripe API call
        await Task.Delay(500, ct); // Simulate network latency

        var paymentIntentId = $"pi_mock_{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Created mock payment intent {PaymentIntentId} for order {OrderId} amount {Amount}",
            paymentIntentId, order.Id, order.TotalAmount);

        return new PaymentResult
        {
            Success = true,
            PaymentIntentId = paymentIntentId,
            ClientSecret = "mock_client_secret",
            Message = "Mock payment intent created"
        };
    }

    public async Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId, CancellationToken ct)
    {
        await Task.Delay(300, ct);

        // Simulate 90% success rate for demo
        var success = new Random().Next(0, 10) < 9;

        if (success)
        {
            // Update order status
            var order = await _context.Orders.FindAsync(new object[] { Guid.Parse(paymentIntentId.Split('_').Last()) }, ct);
            if (order != null)
            {
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            return new PaymentResult
            {
                Success = true,
                Message = "Payment confirmed successfully"
            };
        }
        else
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Payment failed (simulated)"
            };
        }
    }
}
```

**Acceptance Criteria:**

- [ ] Payment intent created when order created
- [ ] Mock confirmation updates order status to paid
- [ ] Simulated failures for testing error handling
- [ ] Payment flow documented for demo

---

**TASK 3.4: SignalR Real-time Updates**  
**Assigned to:** Sarah + You  
**Priority:** 🟡 High  
**Estimated Time:** 8 hours

**Description:**
Implement SignalR hubs for batch progress, offer updates, and notifications.

**Techniques & Technologies:**

```csharp
// Jomla.API/Hubs/NotificationHub.cs
public class NotificationHub : Hub
{
    private readonly ICurrentUserService _currentUserService;

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;

        // Add user to role-based group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");

        // Add user to personal group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnConnectedAsync();
    }

    public async Task JoinBatchGroup(Guid batchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchId}");
    }

    public async Task JoinGroupRequestGroup(Guid groupRequestId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"grouprequest-{groupRequestId}");
    }
}

// Broadcasting updates
public class SignalRBroadcastService : ISignalRBroadcastService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public async Task SendBatchUpdate(Guid batchId, int currentQuantity, int targetQuantity)
    {
        await _hubContext.Clients.Group($"batch-{batchId}")
            .SendAsync("ReceiveBatchUpdate", new
            {
                batchId,
                currentQuantity,
                targetQuantity,
                percentage = (currentQuantity / (decimal)targetQuantity) * 100
            });
    }

    public async Task SendOfferUpdate(Guid offerId)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveOfferUpdate", new
        {
            offerId,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendNotificationToUser(string userId, NotificationDto notification)
    {
        await _hubContext.Clients.Group($"user-{userId}")
            .SendAsync("ReceiveNotification", notification);
    }
}
```

**Acceptance Criteria:**

- [ ] Batch progress updates in real-time
- [ ] New offers appear without page refresh
- [ ] Notifications pushed to dashboard
- [ ] Connection handling and reconnection logic

---

**SPRINT 3 DELIVERABLES CHECKLIST:**

- [ ] AI negotiation agent working end-to-end
- [ ] Hangfire jobs running on schedule
- [ ] Payment flow functional (mock)
- [ ] Real-time updates via SignalR
- [ ] Orders created with correct status

---

### **SPRINT 4: Polish & Demo Prep**

**Duration:** Days 16-20 (June 25-30)  
**Goal:** Feature freeze, bug fixes, UI polish, demo preparation.

---

#### **Day 16-17 Tasks**

**TASK 4.1: Feature Freeze & Bug Fixes**  
**Assigned to:** All Team Members  
**Priority:** 🔴 Critical  
**Estimated Time:** 16 hours

**Activities:**

- No new features after Day 16 EOD
- Triage and fix critical bugs
- Performance optimization
- Security review

**Bug Tracking:**

- Use GitHub Issues for bug tracking
- Priority labels: P0 (Critical), P1 (High), P2 (Medium), P3 (Low)
- Only fix P0 and P1 bugs

---

**TASK 4.2: UI/UX Polish**  
**Assigned to:** Sarah + Defrawy  
**Priority:** 🟡 High  
**Estimated Time:** 12 hours

**Checklist:**

- [ ] Consistent color scheme (buyer light / seller dark)
- [ ] Loading skeletons for all async operations
- [ ] Error messages user-friendly
- [ ] Empty states designed
- [ ] Mobile responsive testing
- [ ] Cross-browser testing (Chrome, Firefox, Edge)
- [ ] Accessibility (ARIA labels, keyboard navigation)

---

#### **Day 18-19 Tasks**

**TASK 4.3: Database Seed Scripts**  
**Assigned to:** Mohammed + Nour  
**Priority:** 🟡 High  
**Estimated Time:** 6 hours

**Description:**
Create realistic demo data for presentation.

```csharp
// Jomla.Infrastructure/Persistence/SeedData.cs
public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed categories
        if (!context.Categories.Any())
        {
            var electronics = new Category { Name = "Electronics", ParentId = null };
            var laptops = new Category { Name = "Laptops", ParentId = electronics.Id };
            var smartphones = new Category { Name = "Smartphones", ParentId = electronics.Id };

            context.Categories.AddRange(electronics, laptops, smartphones);
            await context.SaveChangesAsync();
        }

        // Seed test sellers
        if (!context.Users.Any(u => u.Role == UserRole.Seller))
        {
            var seller1 = new User
            {
                Name = "TechDeals Store",
                Email = "seller1@jomla.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.Seller
            };

            context.Users.Add(seller1);
            await context.SaveChangesAsync();
        }

        // Seed test buyers
        // ... similar pattern

        // Seed active offers with batches
        // Seed active group requests
        // Seed seller category preferences
    }
}
```

---

**TASK 4.4: Demo Script & Backup Video**  
**Assigned to:** All Team  
**Priority:** 🔴 Critical  
**Estimated Time:** 8 hours

**Demo Script Outline:**

**Introduction (2 minutes)**

- Welcome and team introduction
- Problem statement: Group buying benefits
- Jomla solution overview

**Flow 1: Seller-Initiated Deal (5 minutes)**

1. Seller login
2. Create new offer (show form validation)
3. Show offer in dashboard with "pending moderation"
4. Switch to buyer view
5. Buyer browses offers
6. Buyer joins batch (show real-time progress bar)
7. Multiple buyers join (show SignalR updates)
8. Batch completes automatically
9. Orders created, payment flow shown

**Flow 2: Buyer-Initiated Wish Hub (5 minutes)**

1. Buyer creates group request: "Gaming Laptop RTX 4070"
2. Show AI categorization result
3. Other buyers join the hub
4. Show quantity growing
5. Seller receives notification
6. Seller submits offer with price
7. Buyers vote accept/reject
8. Show AI negotiation (price drops)
9. Enough acceptances → order created

**Technical Highlights (3 minutes)**

- Clean Architecture diagram
- CQRS pattern explanation
- AI Agents (show negotiation log)
- Hangfire dashboard (show scheduled jobs)
- SignalR real-time updates
- Database schema overview

**Q&A (5 minutes)**

**Backup Video Recording:**

- Record full demo flow on Day 18
- Store on Google Drive + YouTube (unlisted)
- Test video quality and audio

---

**TASK 4.5: Final Testing & Documentation**  
**Assigned to:** All Team  
**Priority:** 🔴 Critical  
**Estimated Time:** 10 hours

**Testing Checklist:**

- [ ] All user stories tested end-to-end
- [ ] API endpoints documented in Swagger
- [ ] README.md updated with setup instructions
- [ ] Deployment guide written
- [ ] Known issues documented

**Documentation Files:**

```
/docs
  ├── ARCHITECTURE.md
  ├── API_DOCUMENTATION.md
  ├── DEPLOYMENT.md
  ├── USER_GUIDE.md
  └── DEMO_SCRIPT.md
```

---

#### **Day 20: Final Day**

**TASK 4.6: Final Deployment & Demo Rehearsal**  
**Assigned to:** All Team  
**Priority:** 🔴 Critical

**Schedule:**

- 9:00 AM - Final code review and merge
- 10:00 AM - Deploy to production/staging server
- 11:00 AM - Smoke test all features
- 12:00 PM - Lunch break
- 1:00 PM - Demo rehearsal (full run-through)
- 2:00 PM - Fix any critical issues
- 3:00 PM - Final rehearsal
- 4:00 PM - Prepare presentation slides
- 5:00 PM - Team celebration! 🎉

---

## **6. Daily Standup Template**

**Time:** 10:00 AM daily (15 minutes max)

**Format:**

```
1. What did I accomplish yesterday?
2. What will I work on today?
3. Any blockers or impediments?
```

**Example:**

```
Mohammed:
- Yesterday: Completed Seller Offer CQRS, wrote 8 unit tests
- Today: Starting batch management and Hangfire jobs
- Blockers: Need API contract for batch join endpoint confirmed

Sarah:
- Yesterday: Angular auth service and login page done
- Today: Building seller dashboard components
- Blockers: None

You:
- Yesterday: JWT auth implemented, code reviewed Mohammed's PRs
- Today: Setting up order creation pipeline
- Blockers: Waiting for Stripe mock service design
```

**Action Items:**

- Assign owner and due date for each blocker
- Update GitHub Projects board
- Note any scope changes

---

## **7. Risk Management**

### **High Risks**

| Risk                                | Probability | Impact | Mitigation Strategy                                                           |
| ----------------------------------- | ----------- | ------ | ----------------------------------------------------------------------------- |
| AI Agent integration takes too long | High        | High   | Use mock AI service for Days 1-10, integrate real AI in Sprint 3              |
| Stripe integration complexity       | Medium      | High   | Use MockStripeService throughout MVP, mention real integration as future work |
| Team member unavailable             | Medium      | High   | Cross-train on critical modules, maintain documentation                       |
| Scope creep                         | High        | Medium | Strict feature freeze on Day 16, say "no" to new features                     |
| SignalR connection issues           | Medium      | Medium | Implement reconnection logic, graceful degradation to polling                 |
| Database performance                | Low         | Medium | Add indexes on foreign keys, use AsNoTracking() for reads                     |

### **Contingency Plans**

**If AI Agents Don't Work:**

- Hardcode categorization rules (keyword matching)
- Simulate negotiation with fixed price reduction (10% per 3 rejections)
- Document that AI is "simulated for demo"

**If Real-time Updates Fail:**

- Fallback to polling every 30 seconds
- Show "Last updated" timestamp
- Manual refresh button

**If Payment Flow Breaks:**

- Skip payment in demo, show "Order created" success message
- Pre-record payment flow video as backup

---

## **8. Definition of Done**

A task/story is considered **Done** when:

✅ Code written and follows Clean Architecture principles  
✅ Unit tests passing (minimum 80% coverage for core logic)  
✅ Integration tests passing for API endpoints  
✅ Code reviewed by at least one team member  
✅ Merged to main branch without conflicts  
✅ Swagger documentation updated  
✅ Frontend integrated and working (if applicable)  
✅ No console errors in browser DevTools  
✅ Works on Chrome and Firefox  
✅ Mobile responsive (if UI component)

---

## **9. Demo Script Outline**

**Slide 1: Title Slide**

- Jomla - B2B Group Buying Platform
- Team members names
- Graduation Project 2026

**Slide 2: Problem Statement**

- Small buyers can't access wholesale prices
- Sellers struggle with bulk sales
- Solution: Group buying marketplace

**Slide 3: Platform Overview**

- Two-sided marketplace diagram
- Seller-initiated vs Buyer-initiated flows
- Key features list

**Slide 4: Technical Architecture**

- Clean Architecture diagram
- Tech stack logos
- AI Agents overview

**Slide 5: Live Demo - Flow 1**

- Seller creates offer
- Buyers join batch
- Real-time progress
- Batch completion

**Slide 6: Live Demo - Flow 2**

- Buyer creates wish hub
- AI categorization
- Seller offer + AI negotiation
- Order creation

**Slide 7: Database Schema**

- ERD diagram
- Key relationships
- Design decisions

**Slide 8: Challenges & Solutions**

- AI integration complexity
- Real-time synchronization
- Background job orchestration

**Slide 9: Future Enhancements**

- Real Stripe integration
- Mobile app (React Native)
- Advanced analytics dashboard
- Multi-language support

**Slide 10: Q&A**

- Thank you
- Questions?

---

## **Appendices**

### **A. GitHub Repository Structure**

```
jomla/
├── .github/
│   └── workflows/
│       └── ci-cd.yml
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API.md
│   └── DEMO.md
├── src/
│   ├── Jomla.API/
│   ├── Jomla.Application/
│   ├── Jomla.Domain/
│   └── Jomla.Infrastructure/
├── tests/
│   ├── Jomla.Application.Tests/
│   └── Jomla.API.IntegrationTests/
├── jomla-frontend/
│   ├── src/
│   ├── e2e/
│   └── angular.json
├── README.md
├── docker-compose.yml (optional)
└── Jomla.sln
```

### **B. Environment Variables**

**Backend (.env or appsettings.json):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=JomlaDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "JomlaAPI",
    "Audience": "JomlaClient",
    "ExpirationInMinutes": 60
  },
  "OpenAISettings": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o-mini"
  },
  "StripeSettings": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_..."
  }
}
```

**Frontend (environment.ts):**

```typescript
export const environment = {
  production: false,
  apiUrl: "https://localhost:7001/api",
  signalRUrl: "https://localhost:7001/hubs",
};
```

### **C. Useful Commands Cheat Sheet**

**Backend:**

```bash
# Build solution
dotnet build Jomla.sln

# Run API
dotnet run --project Jomla.API

# Add migration
dotnet ef migrations add MigrationName --project Jomla.Infrastructure --startup-project Jomla.API

# Update database
dotnet ef database update --project Jomla.Infrastructure --startup-project Jomla.API

# Run tests
dotnet test

# Watch for changes
dotnet watch run --project Jomla.API
```

**Frontend:**

```bash
# Install dependencies
npm install

# Run development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test

# Lint
ng lint

# Format
npx prettier --write "src/**/*.ts"
```

**Git:**

```bash
# Create feature branch
git checkout -b feature/feature-name

# Commit changes
git add .
git commit -m "feat: add seller offer creation"

# Push to remote
git push origin feature/feature-name

# Create PR via GitHub UI

# Pull latest main
git checkout main
git pull origin main

# Rebase feature branch
git checkout feature/feature-name
git rebase main
```

---

## **Final Notes**

**Success Metrics:**

- ✅ All core features working (seller offers, group requests, orders)
- ✅ At least 1 AI agent fully functional
- ✅ Real-time updates working
- ✅ Clean, professional UI
- ✅ Successful demo presentation

**Team Principles:**

1. **Communicate early, communicate often** - Don't suffer in silence
2. **Help each other** - If you finish early, help teammates
3. **Quality over perfection** - Working MVP > Perfect incomplete features
4. **Document as you go** - Future you will thank present you
5. **Have fun!** - This is your graduation project, enjoy the journey

---

**Good luck, team! You've got this! 🚀**

_Document Version: 1.0_  
_Created: June 10, 2026_  
_Last Updated: June 10, 2026_

---

Now you have a comprehensive plan! To convert this to PDF:

**Option 1: VS Code**

1. Install "Markdown PDF" extension
2. Right-click this file
3. Select "Markdown PDF: Export (pdf)"

**Option 2: Online**

1. Copy content to https://markdown-pdf.com
2. Download PDF

**Option 3: Pandoc (if installed)**

```bash
pandoc plan.md -o jomla-project-plan.pdf --pdf-engine=xelatex
```

Would you like me to create any specific diagrams (architecture, ERD, sequence diagrams) to include in the PDF?
