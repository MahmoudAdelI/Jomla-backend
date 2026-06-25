using Bogus;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Infrastructure.Persistance.Seeders
{
    public class DataSeeder(AppDbContext _db, UserManager<AppUser> _userManager, RoleManager<IdentityRole<Guid>> _roleManager)
    {
        private static readonly Dictionary<string, string[]> ProductTitlesByCategory = new()
        {
            ["Refrigerators"] = new[] { "Double-Door Refrigerator 450L", "Compact Mini Fridge 90L", "Side-by-Side Refrigerator 600L" },
            ["Freezers"] = new[] { "Chest Freezer 200L", "Upright Freezer 300L" },
            ["Washing Machines"] = new[] { "Front-Load Washing Machine 8kg", "Top-Load Washing Machine 10kg" },
            ["Dishwashers"] = new[] { "Built-In Dishwasher 12 Place Settings", "Freestanding Dishwasher 14 Place Settings" },
            ["Microwaves"] = new[] { "Digital Microwave Oven 25L", "Convection Microwave Oven 30L" },
            ["Coffee Makers"] = new[] { "Automatic Drip Coffee Maker", "Espresso Machine with Grinder" },
            ["Blenders"] = new[] { "Heavy-Duty Countertop Blender", "Commercial Smoothie Blender" },
            ["Air Conditioners"] = new[] { "Split Air Conditioner 1.5 Ton", "Window Air Conditioner 1 Ton" },
            ["Laptops"] = new[] { "Business Laptop 14-inch Core i5", "Ultrabook 13-inch Core i7" },
            ["Laptop Bags"] = new[] { "Padded Laptop Backpack 15.6-inch", "Laptop Sleeve Case 14-inch" },
            ["Computer Accessories"] = new[] { "Wireless Mouse and Keyboard Combo", "USB-C Docking Station" },
            ["Smartphones"] = new[] { "Android Smartphone 128GB", "Smartphone 256GB Dual SIM" },
            ["Tablets"] = new[] { "10-inch Android Tablet", "Tablet with Stylus 11-inch" },
            ["Cameras"] = new[] { "Digital DSLR Camera Kit", "4K Action Camera" },
            ["Mobile & Tablet Accessories"] = new[] { "Tempered Glass Screen Protector Pack", "Fast Charging Power Bank 20000mAh" },
            ["Paper Products"] = new[] { "A4 Copy Paper Ream Pack (5x500)", "Sticky Notes Bulk Pack" },
            ["Writing Instruments"] = new[] { "Ballpoint Pens Box of 50", "Whiteboard Markers Set of 12" },
            ["Office Furniture"] = new[] { "Ergonomic Office Chair", "Adjustable Standing Desk" },
            ["Printing Supplies"] = new[] { "Toner Cartridge Multipack", "Inkjet Printer Ink Set" },
            ["Furniture"] = new[] { "3-Seater Fabric Sofa", "Dining Table Set (6-Seater)" },
            ["Bedding"] = new[] { "Queen Size Bedsheet Set", "Microfiber Comforter Set" },
            ["Home Decor"] = new[] { "Wall Art Canvas Set of 3", "Decorative Throw Pillow Set" },
        };
        private static readonly Dictionary<string, string[]> GroupRequestTitlesByCategory = new()
        {
            ["Refrigerators"] = new[] { "Mini fridges for office break rooms", "Double-door refrigerators for branch offices" },
            ["Freezers"] = new[] { "Chest freezers for restaurant supply" },
            ["Washing Machines"] = new[] { "Front-load washing machines for staff housing" },
            ["Dishwashers"] = new[] { "Commercial dishwashers for cafeteria" },
            ["Microwaves"] = new[] { "Microwaves for office pantry" },
            ["Coffee Makers"] = new[] { "Coffee machines for office branches" },
            ["Blenders"] = new[] { "Blenders for juice bar chain" },
            ["Air Conditioners"] = new[] { "Split ACs for new office building" },
            ["Laptops"] = new[] { "Business laptops for new hires", "Laptops for training center" },
            ["Laptop Bags"] = new[] { "Laptop backpacks for onboarding kits" },
            ["Computer Accessories"] = new[] { "Wireless mouse and keyboard sets for office" },
            ["Smartphones"] = new[] { "Company phones for sales team" },
            ["Tablets"] = new[] { "Tablets for retail checkout stations" },
            ["Cameras"] = new[] { "Security cameras for warehouse" },
            ["Mobile & Tablet Accessories"] = new[] { "Power banks for field staff" },
            ["Paper Products"] = new[] { "A4 paper reams for office restock" },
            ["Writing Instruments"] = new[] { "Pens and markers for branches" },
            ["Office Furniture"] = new[] { "Office chairs for new branch", "Standing desks for HQ renovation" },
            ["Printing Supplies"] = new[] { "Toner cartridges for office printers" },
            ["Furniture"] = new[] { "Sofas for office lounge" },
            ["Bedding"] = new[] { "Bedsheets for staff dormitory" },
            ["Home Decor"] = new[] { "Wall art for office decoration" },
        };
        private static readonly Dictionary<string, Func<Random, Dictionary<string, string>>> VariantGeneratorsByCategory = new()
        {
            ["Refrigerators"] = r => new() { ["color"] = Pick(r, "White", "Silver", "Black"), ["capacity"] = Pick(r, "350L", "450L", "600L") },
            ["Freezers"] = r => new() { ["capacity"] = Pick(r, "150L", "200L", "300L") },
            ["Washing Machines"] = r => new() { ["color"] = Pick(r, "White", "Silver"), ["capacity"] = Pick(r, "7kg", "8kg", "10kg") },
            ["Dishwashers"] = r => new() { ["color"] = Pick(r, "White", "Silver", "Black") },
            ["Microwaves"] = r => new() { ["color"] = Pick(r, "Black", "White", "Silver") },
            ["Coffee Makers"] = r => new() { ["color"] = Pick(r, "Black", "Silver", "Red") },
            ["Blenders"] = r => new() { ["color"] = Pick(r, "Black", "Red", "White") },
            ["Air Conditioners"] = r => new() { ["capacity"] = Pick(r, "1 Ton", "1.5 Ton", "2 Ton") },
            ["Laptops"] = r => new() { ["color"] = Pick(r, "Silver", "Black", "Gray"), ["storage"] = Pick(r, "256GB", "512GB", "1TB"), ["ram"] = Pick(r, "8GB", "16GB") },
            ["Laptop Bags"] = r => new() { ["color"] = Pick(r, "Black", "Gray", "Navy") },
            ["Computer Accessories"] = r => new() { ["color"] = Pick(r, "Black", "White") },
            ["Smartphones"] = r => new() { ["color"] = Pick(r, "Black", "White", "Blue", "Gold"), ["storage"] = Pick(r, "64GB", "128GB", "256GB") },
            ["Tablets"] = r => new() { ["color"] = Pick(r, "Black", "Silver", "Gray"), ["storage"] = Pick(r, "64GB", "128GB") },
            ["Cameras"] = r => new() { ["color"] = Pick(r, "Black", "Silver") },
            ["Mobile & Tablet Accessories"] = r => new() { ["color"] = Pick(r, "Black", "White", "Blue") },
            ["Office Furniture"] = r => new() { ["color"] = Pick(r, "Black", "Gray", "Brown"), ["material"] = Pick(r, "Mesh", "Leather", "Fabric") },
            ["Furniture"] = r => new() { ["color"] = Pick(r, "Gray", "Beige", "Brown"), ["material"] = Pick(r, "Fabric", "Leather") },
            ["Bedding"] = r => new() { ["color"] = Pick(r, "White", "Gray", "Blue"), ["size"] = Pick(r, "Twin", "Queen", "King") },
            ["Home Decor"] = r => new() { ["color"] = Pick(r, "Multicolor", "Beige", "Gray") },
        };
        public async Task SeedAsync()
        {
            if (await _db.Users.AnyAsync())
                return; // DB has been seeded
            await SeedRolesAsync();
            var categories = SeedCategories();
            await _db.SaveChangesAsync();

            var (suppliers, buyers) = await SeedUsersAsync();
            var preferences = SeedSupplierCategoryPreferences(suppliers, categories);
            await _db.SaveChangesAsync();

            var offers = SeedSupplierOffers(suppliers, categories);
            await _db.SaveChangesAsync();

            var (batches, batchParticipants) = SeedSupplierBatchesAndParticipants(offers, buyers);
            await _db.SaveChangesAsync();

            var (groupRequests, groupRequestParticipants) = SeedGroupRequestsAndParticipants(buyers, categories);
            await _db.SaveChangesAsync();

            SeedGroupRequestAlerts(groupRequests, preferences);
            await _db.SaveChangesAsync();

            var groupRequestOffers = SeedGroupRequestOffers(groupRequests, preferences, categories);
            await _db.SaveChangesAsync();

            var responses = SeedBuyerOfferResponses(groupRequestOffers, groupRequestParticipants);
            await _db.SaveChangesAsync();

            SeedOrders(batches, batchParticipants, groupRequestOffers, responses, groupRequestParticipants);
            await _db.SaveChangesAsync();
        }

        private List<Category> SeedCategories()
        {
            #region Electronics
            var electronicsAndAppliances = new Category { Id = Guid.NewGuid(), Name = "Electronics & Appliances" };

            // Large Appliances
            var largeAppliances = new Category { Id = Guid.NewGuid(), Name = "Large Appliances", ParentId = electronicsAndAppliances.Id };
            var refrigerators = new Category { Id = Guid.NewGuid(), Name = "Refrigerators", ParentId = largeAppliances.Id };
            var freezers = new Category { Id = Guid.NewGuid(), Name = "Freezers", ParentId = largeAppliances.Id };
            var washingMachines = new Category { Id = Guid.NewGuid(), Name = "Washing Machines", ParentId = largeAppliances.Id };
            var dishwashers = new Category { Id = Guid.NewGuid(), Name = "Dishwashers", ParentId = largeAppliances.Id };

            // Small Appliances
            var smallAppliances = new Category { Id = Guid.NewGuid(), Name = "Small Appliances", ParentId = electronicsAndAppliances.Id };
            var microwaves = new Category { Id = Guid.NewGuid(), Name = "Microwaves", ParentId = smallAppliances.Id };
            var coffeeMakers = new Category { Id = Guid.NewGuid(), Name = "Coffee Makers", ParentId = smallAppliances.Id };
            var blenders = new Category { Id = Guid.NewGuid(), Name = "Blenders", ParentId = smallAppliances.Id };
            var airConditioners = new Category { Id = Guid.NewGuid(), Name = "Air Conditioners", ParentId = smallAppliances.Id };

            // Laptops & PCs
            var laptopsAndPCs = new Category { Id = Guid.NewGuid(), Name = "Laptops & PCs", ParentId = electronicsAndAppliances.Id };
            var laptops = new Category { Id = Guid.NewGuid(), Name = "Laptops", ParentId = laptopsAndPCs.Id };
            var laptopBags = new Category { Id = Guid.NewGuid(), Name = "Laptop Bags", ParentId = laptopsAndPCs.Id };
            var computerAccessories = new Category { Id = Guid.NewGuid(), Name = "Computer Accessories", ParentId = laptopsAndPCs.Id };

            // Other Electronics
            var smartphones = new Category { Id = Guid.NewGuid(), Name = "Smartphones", ParentId = electronicsAndAppliances.Id };
            var tablets = new Category { Id = Guid.NewGuid(), Name = "Tablets", ParentId = electronicsAndAppliances.Id };
            var cameras = new Category { Id = Guid.NewGuid(), Name = "Cameras", ParentId = electronicsAndAppliances.Id };
            var mobileAndTabletAccessories = new Category { Id = Guid.NewGuid(), Name = "Mobile & Tablet Accessories", ParentId = electronicsAndAppliances.Id };
            #endregion

            #region Office Supplies
            var officeSupplies = new Category { Id = Guid.NewGuid(), Name = "Office Supplies" };
            var paperProducts = new Category { Id = Guid.NewGuid(), Name = "Paper Products", ParentId = officeSupplies.Id };
            var writingInstruments = new Category { Id = Guid.NewGuid(), Name = "Writing Instruments", ParentId = officeSupplies.Id };
            var officeFurniture = new Category { Id = Guid.NewGuid(), Name = "Office Furniture", ParentId = officeSupplies.Id };
            var printingSupplies = new Category { Id = Guid.NewGuid(), Name = "Printing Supplies", ParentId = officeSupplies.Id };
            #endregion

            #region Home Supplies
            var homeSupplies = new Category { Id = Guid.NewGuid(), Name = "Home Supplies" };
            var furniture = new Category { Id = Guid.NewGuid(), Name = "Furniture", ParentId = homeSupplies.Id };
            var bedding = new Category { Id = Guid.NewGuid(), Name = "Bedding", ParentId = homeSupplies.Id };
            var homeDecor = new Category { Id = Guid.NewGuid(), Name = "Home Decor", ParentId = homeSupplies.Id };
            #endregion

            var categories = new List<Category>
                {
                    electronicsAndAppliances, largeAppliances, refrigerators, freezers, washingMachines, dishwashers,
                    smallAppliances, microwaves, coffeeMakers, blenders, airConditioners,
                    laptopsAndPCs, laptops, laptopBags, computerAccessories,
                    smartphones, tablets, cameras, mobileAndTabletAccessories,
                    officeSupplies, paperProducts, writingInstruments, officeFurniture, printingSupplies,
                    homeSupplies, furniture, bedding, homeDecor
                };

            _db.Categories.AddRange(categories);
            return categories;
        }

        private async Task SeedRolesAsync()
        {
            foreach (var role in Enum.GetValues<UserRole>())
            {
                var roleName = role.ToString();
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant()
                    });
                }
            }
        }

        private async Task<(List<AppUser> suppliers, List<AppUser> buyers)> SeedUsersAsync()
        {
            var suppliers = new List<AppUser>();
            var buyers = new List<AppUser>();

            var testAccounts = new[]
            {
            ("seller1@jomla.test", "Seller", "One", UserRole.Supplier),
            ("seller2@jomla.test", "Seller", "Two", UserRole.Supplier),
            ("buyer1@jomla.test", "Buyer", "One", UserRole.Buyer),
            ("buyer2@jomla.test", "Buyer", "Two", UserRole.Buyer),
        };

            foreach (var (email, first, last, role) in testAccounts)
            {
                var user = await CreateUserAsync(email, first, last, DateTime.UtcNow, role);
                (role == UserRole.Supplier ? suppliers : buyers).Add(user);
            }

            var faker = new Faker();

            for (int i = 0; i < 15; i++)
            {
                var first = faker.Name.FirstName();
                var last = faker.Name.LastName();
                var email = $"{SanitizeForEmail(first)}.{SanitizeForEmail(last)}{i}@jomla.test".ToLowerInvariant();
                var user = await CreateUserAsync(email, first, last, faker.Date.Past(1), UserRole.Supplier);
                suppliers.Add(user);
            }

            for (int i = 0; i < 50; i++)
            {
                var first = faker.Name.FirstName();
                var last = faker.Name.LastName();
                var email = $"{SanitizeForEmail(first)}.{SanitizeForEmail(last)}{i}@jomla.test".ToLowerInvariant();
                var user = await CreateUserAsync(email, first, last, faker.Date.Past(1), UserRole.Buyer);
                buyers.Add(user);
            }

            return (suppliers, buyers);
        }

        private async Task<AppUser> CreateUserAsync(string email, string first, string last, DateTime createdAt, UserRole role)
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = first,
                LastName = last,
                CreatedAt = createdAt
            };

            var result = await _userManager.CreateAsync(user, "Password123!");
            if (!result.Succeeded)
                throw new Exception($"Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            await _userManager.AddToRoleAsync(user, role.ToString());

            return user;
        }

        private List<SupplierCategoryPreference> SeedSupplierCategoryPreferences(List<AppUser> suppliers, List<Category> categories)
        {
            var random = new Random(44);
            var prefs = new List<SupplierCategoryPreference>();

            foreach(var supplier in suppliers)
            {
                var pickCount = random.Next(1, 5); // 1 to 4 categories per seller
                var pickedCategories = categories
                    .OrderBy(_ => random.Next())
                    .Take(pickCount);

                foreach (var category in pickedCategories)
                {
                    prefs.Add(new SupplierCategoryPreference
                    {
                        SupplierId = supplier.Id,
                        CategoryId = category.Id,
                        MinQuantity = random.Next(5, 50)
                    });
                }
            }

            _db.SupplierCategoryPreferences.AddRange(prefs);
            return prefs;
        }

        private List<SupplierOffer> SeedSupplierOffers(List<AppUser> suppliers, List<Category> categories)
        {
            var leafCategories = categories
                .Where(c => !categories.Any(child => child.ParentId == c.Id))
                .ToList();

            var random = new Random(45);
            var offers = new List<SupplierOffer>();

            foreach (var supplier in suppliers)
            {
                var offerCount = random.Next(1, 4); // 1-3 offers per supplier

                for (int i = 0; i < offerCount; i++)
                {
                    var category = leafCategories[random.Next(leafCategories.Count)];
                    var titlePool = ProductTitlesByCategory[category.Name];
                    var title = titlePool[random.Next(titlePool.Length)];

                    var batchTarget = random.Next(2, 11) * 10; // 20-100, round numbers
                    var unitPrice = Math.Round((decimal)(random.NextDouble() * 480 + 20), 2);
                    var discount = Math.Round((decimal)(random.NextDouble() * 25 + 5), 2);
                    var createdAt = DateTime.UtcNow.AddDays(-random.Next(5, 60));

                    var offer = new SupplierOffer
                    {
                        Id = Guid.NewGuid(),
                        SupplierId = supplier.Id,
                        CategoryId = category.Id,
                        Title = title,
                        Description = $"Bulk order of {title}. Join the group buy to unlock wholesale pricing.",
                        UnitPrice = unitPrice,
                        DiscountPercentage = discount,
                        BatchTargetQuantity = batchTarget,
                        TotalQuantityAvailable = batchTarget * random.Next(2, 4), // enough for 2-3 batches
                        MinFallbackQuantity = random.Next(0, 2) == 1 ? (int)(batchTarget * 0.6) : null,
                        VariantAttributes = GenerateVariantAttributes(category.Name, random),
                        ImageUrls = null,
                        Status = SupplierOfferStatus.Active,
                        ModerationStatus = ModerationStatus.Approved,
                        CreatedAt = createdAt,
                        ExpiresAt = createdAt.AddDays(random.Next(14, 45))
                    };

                    offers.Add(offer);
                }
            }

            _db.SupplierOffers.AddRange(offers);
            return offers;
        }

        private (List<SupplierBatch> batches, List<BatchParticipant> participants) SeedSupplierBatchesAndParticipants(List<SupplierOffer> offers, List<AppUser> buyers)
        {
            var random = new Random(46);
            var batches = new List<SupplierBatch>();
            var participants = new List<BatchParticipant>();

            foreach (var offer in offers)
            {
                var remaining = offer.TotalQuantityAvailable;
                var numBatches = Math.Max(1, (int)Math.Ceiling((double)remaining / offer.BatchTargetQuantity));

                for (int b = 0; b < numBatches; b++)
                {
                    var isLastBatch = b == numBatches - 1;
                    var targetQuantity = isLastBatch && remaining < offer.BatchTargetQuantity
                        ? remaining
                        : offer.BatchTargetQuantity;

                    BatchStatus status;
                    int currentQuantity;
                    DateTime? completedAt = null;
                    var batchCreatedAt = offer.CreatedAt.AddDays(b * 5);

                    if (!isLastBatch)
                    {
                        // Earlier batches are already completed and full
                        status = BatchStatus.Completed;
                        currentQuantity = targetQuantity;
                        completedAt = batchCreatedAt.AddDays(random.Next(1, 5));
                    }
                    else
                    {
                        var roll = random.Next(0, 3);
                        if (roll == 0)
                        {
                            status = BatchStatus.Open;
                            currentQuantity = random.Next(0, targetQuantity); // partially filled, still open
                        }
                        else if (roll == 1)
                        {
                            status = BatchStatus.Completed;
                            currentQuantity = targetQuantity;
                            completedAt = batchCreatedAt.AddDays(random.Next(1, 5));
                        }
                        else
                        {
                            status = BatchStatus.Failed;
                            currentQuantity = random.Next(0, Math.Max(1, targetQuantity / 2)); // never reached target
                            completedAt = batchCreatedAt.AddDays(random.Next(1, 5));
                        }
                    }

                    var batch = new SupplierBatch
                    {
                        Id = Guid.NewGuid(),
                        OfferId = offer.Id,
                        Offer = offer,
                        BatchNumber = b + 1,
                        TargetQuantity = targetQuantity,
                        Status = status,
                        CreatedAt = batchCreatedAt,
                        CompletedAt = completedAt
                    };

                    // Generate participants whose quantities sum toward currentQuantity
                    var qtyToAllocate = currentQuantity;
                    var batchParticipants = new List<BatchParticipant>();
                    
                    if (qtyToAllocate > 0)
                    {
                        var shuffledBuyers = buyers.OrderBy(_ => random.Next()).ToList();
                        int buyerIndex = 0;

                        while (qtyToAllocate > 0 && buyerIndex < shuffledBuyers.Count)
                        {
                            var buyer = shuffledBuyers[buyerIndex++];
                            var qty = Math.Min(qtyToAllocate, random.Next(1, 11));
                            
                            // If we are at the last available buyer, take all remaining to satisfy the target
                            if (buyerIndex == shuffledBuyers.Count)
                            {
                                qty = qtyToAllocate;
                            }
                            qtyToAllocate -= qty;

                            BatchParticipantStatus participantStatus = status switch
                            {
                                BatchStatus.Completed => BatchParticipantStatus.Active,
                                BatchStatus.Failed => BatchParticipantStatus.Left,
                                BatchStatus.Open => random.Next(0, 10) == 0 ? BatchParticipantStatus.Left : BatchParticipantStatus.Active,
                                _ => BatchParticipantStatus.Active
                            };

                            batchParticipants.Add(new BatchParticipant
                            {
                                BatchId = batch.Id,
                                BuyerId = buyer.Id,
                                Quantity = qty,
                                Status = participantStatus,
                                StripePaymentIntentId = $"pi_{Guid.NewGuid():N}"[..27],
                                JoinedAt = batchCreatedAt.AddHours(random.Next(1, 96))
                            });
                        }
                    }

                    // CurrentQuantity reflects only participants whose hold is still active
                    batch.CurrentQuantity = batchParticipants
                        .Where(p => p.Status != BatchParticipantStatus.Left)
                        .Sum(p => p.Quantity);

                    batches.Add(batch);
                    participants.AddRange(batchParticipants);

                    // Decrement available quantity and mark inactive if stock is depleted
                    if (status == BatchStatus.Completed)
                    {
                        offer.TotalQuantityAvailable = Math.Max(0, offer.TotalQuantityAvailable - batch.CurrentQuantity);
                        if (offer.TotalQuantityAvailable <= 0)
                        {
                            offer.Status = SupplierOfferStatus.Inactive;
                        }
                    }

                    remaining -= targetQuantity;
                }
            }

            _db.SupplierBatches.AddRange(batches);
            _db.BatchParticipants.AddRange(participants);

            return (batches, participants);
        }

        private (List<GroupRequest> groupRequests, List<GroupRequestParticipant> participants) SeedGroupRequestsAndParticipants(
        List<AppUser> buyers, List<Category> categories)
        {
            var leafCategories = categories
                .Where(c => !categories.Any(child => child.ParentId == c.Id))
                .ToList();

            var random = new Random(47);
            var groupRequests = new List<GroupRequest>();
            var allParticipants = new List<GroupRequestParticipant>();

            const int requestCount = 20;

            for (int i = 0; i < requestCount; i++)
            {
                var initiator = buyers[random.Next(buyers.Count)];
                var category = leafCategories[random.Next(leafCategories.Count)];
                var titlePool = GroupRequestTitlesByCategory[category.Name];
                var title = titlePool[random.Next(titlePool.Length)];
                var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));

                var statusRoll = random.Next(0, 10);
                var status = statusRoll switch
                {
                    < 7 => GroupRequestStatus.Active,
                    < 9 => GroupRequestStatus.Closed,
                    _ => GroupRequestStatus.Inactive
                };

                var groupRequestId = Guid.NewGuid();
                var participants = new List<GroupRequestParticipant>();

                // Initiator is always the first participant
                participants.Add(new GroupRequestParticipant
                {
                    GroupRequestId = groupRequestId,
                    BuyerId = initiator.Id,
                    Quantity = random.Next(5, 31),
                    Status = GroupRequestParticipantStatus.Active,
                    JoinedAt = createdAt
                });

                // 0-4 additional buyers join the demand pool
                var additionalCount = random.Next(0, 5);
                var otherBuyers = buyers
                    .Where(b => b.Id != initiator.Id)
                    .OrderBy(_ => random.Next())
                    .Take(additionalCount);

                foreach (var buyer in otherBuyers)
                {
                    participants.Add(new GroupRequestParticipant
                    {
                        GroupRequestId = groupRequestId,
                        BuyerId = buyer.Id,
                        Quantity = random.Next(1, 21),
                        Status = GroupRequestParticipantStatus.Active,
                        JoinedAt = createdAt.AddHours(random.Next(1, 72))
                    });
                }

                DateTime? inactiveSince = null;
                if (status == GroupRequestStatus.Inactive)
                {
                    // Demand pool emptied out - everyone left, current_quantity hits 0
                    foreach (var p in participants)
                        p.Status = GroupRequestParticipantStatus.Left;

                    inactiveSince = createdAt.AddDays(random.Next(1, 10));
                }
                else
                {
                    // Occasionally one of the non-initiator participants backs out
                    foreach (var p in participants.Skip(1))
                    {
                        if (random.Next(0, 10) == 0)
                            p.Status = GroupRequestParticipantStatus.Left;
                    }
                }

                var currentQuantity = participants
                    .Where(p => p.Status == GroupRequestParticipantStatus.Active)
                    .Sum(p => p.Quantity);

                groupRequests.Add(new GroupRequest
                {
                    Id = groupRequestId,
                    InitiatorId = initiator.Id,
                    CategoryId = category.Id,
                    Title = title,
                    CurrentQuantity = currentQuantity,
                    Status = status,
                    ModerationStatus = ModerationStatus.Approved,
                    InactiveSince = inactiveSince,
                    CreatedAt = createdAt
                });

                allParticipants.AddRange(participants);
            }

            _db.GroupRequests.AddRange(groupRequests);
            _db.GroupRequestParticipants.AddRange(allParticipants);

            return (groupRequests, allParticipants);
        }

        private void SeedGroupRequestAlerts(List<GroupRequest> groupRequests, List<SupplierCategoryPreference> preferences)
        {
            var random = new Random(48);
            var alerts = new List<GroupRequestAlert>();

            foreach (var request in groupRequests)
            {
                var matchingSupplierIds = preferences
                    .Where(p => p.CategoryId == request.CategoryId && p.MinQuantity <= request.CurrentQuantity)
                    .Select(p => p.SupplierId)
                    .Distinct();

                foreach (var supplierId in matchingSupplierIds)
                {
                    var statusRoll = random.Next(0, 10);
                    var status = statusRoll switch
                    {
                        < 7 => GroupRequestAlertStatus.Pending,
                        < 9 => GroupRequestAlertStatus.Responded,
                        _ => GroupRequestAlertStatus.Ignored
                    };

                    alerts.Add(new GroupRequestAlert
                    {
                        GroupRequestId = request.Id,
                        SupplierId = supplierId,
                        Status = status,
                        NotifiedAt = request.CreatedAt.AddHours(random.Next(1, 24))
                    });
                }
            }

            _db.GroupRequestAlerts.AddRange(alerts);
        }

        private List<GroupRequestOffer> SeedGroupRequestOffers(
        List<GroupRequest> groupRequests, List<SupplierCategoryPreference> preferences, List<Category> categories)
        {
            var random = new Random(49);
            var offers = new List<GroupRequestOffer>();
            var categoryById = categories.ToDictionary(c => c.Id);

            var candidates = groupRequests
                .Where(gr => gr.Status != GroupRequestStatus.Inactive)
                .OrderBy(_ => random.Next())
                .Take(10);

            foreach (var request in candidates)
            {
                var matchingSupplierIds = preferences
                    .Where(p => p.CategoryId == request.CategoryId && p.MinQuantity <= request.CurrentQuantity)
                    .Select(p => p.SupplierId)
                    .Distinct()
                    .ToList();

                if (matchingSupplierIds.Count == 0)
                    continue; // no supplier interested in this category/quantity

                var supplierId = matchingSupplierIds[random.Next(matchingSupplierIds.Count)];

                var unitPrice = Math.Round((decimal)(random.NextDouble() * 480 + 20), 2);
                var hasNegotiationAgent = random.Next(0, 2) == 0; // 50% chance
                decimal? minUnitPrice = hasNegotiationAgent
                    ? Math.Round(unitPrice * 0.85m, 2) // floor is 15% below opening price
                    : null;

                var createdAt = request.CreatedAt.AddDays(random.Next(1, 5));

                var status = request.Status == GroupRequestStatus.Closed
                    ? GroupRequestOfferStatus.Accepted
                    : GroupRequestOfferStatus.Open;

                var categoryName = categoryById[request.CategoryId].Name;

                offers.Add(new GroupRequestOffer
                {
                    Id = Guid.NewGuid(),
                    GroupRequestId = request.Id,
                    SupplierId = supplierId,
                    UnitPrice = unitPrice,
                    MinUnitPrice = minUnitPrice,
                    CurrentUnitPrice = unitPrice, // no negotiation yet, so current == opening
                    QuantityAvailable = request.CurrentQuantity + random.Next(0, 11),
                    MinFallbackQuantity = random.Next(0, 2) == 1 ? (int)(request.CurrentQuantity * 0.6) : null,
                    VariantAttributes = GenerateVariantAttributes(categoryName, random),
                    RoundNumber = 1,
                    ParentId = null,
                    Status = status,
                    CreatedAt = createdAt,
                    ExpiresAt = createdAt.AddDays(random.Next(5, 15))
                });
            }

            _db.GroupRequestOffers.AddRange(offers);
            return offers;
        }

        private List<BuyerOfferResponse> SeedBuyerOfferResponses(List<GroupRequestOffer> offers, List<GroupRequestParticipant> allParticipants)
        {
            var random = new Random(50);
            var responses = new List<BuyerOfferResponse>();

            foreach (var offer in offers)
            {
                var activeParticipants = allParticipants
                    .Where(p => p.GroupRequestId == offer.GroupRequestId && p.Status == GroupRequestParticipantStatus.Active)
                    .ToList();

                foreach (var participant in activeParticipants)
                {
                    BuyerOfferResponseType response;

                    if (offer.Status == GroupRequestOfferStatus.Accepted)
                    {
                        response = BuyerOfferResponseType.Accepted;
                    }
                    else
                    {
                        var roll = random.Next(0, 10);
                        if (roll < 4) response = BuyerOfferResponseType.Accepted;
                        else if (roll < 7) response = BuyerOfferResponseType.Rejected;
                        else continue; // no response yet
                    }

                    responses.Add(new BuyerOfferResponse
                    {
                        OfferId = offer.Id,
                        BuyerId = participant.BuyerId,
                        Response = response,
                        StripePaymentIntentId = response == BuyerOfferResponseType.Accepted ? $"pi_{Guid.NewGuid():N}"[..27] : null,
                        RespondedAt = offer.CreatedAt.AddHours(random.Next(1, 48))
                    });
                }
            }

            _db.BuyerOfferResponses.AddRange(responses);
            return responses;
        }

        private void SeedOrders(
            List<SupplierBatch> batches,
            List<BatchParticipant> batchParticipants,
            List<GroupRequestOffer> groupRequestOffers,
            List<BuyerOfferResponse> responses,
            List<GroupRequestParticipant> groupRequestParticipants)
        {
            var orders = new List<Order>();

            // 1. Orders for completed batches
            var completedBatches = batches.Where(b => b.Status == BatchStatus.Completed).ToList();
            foreach (var batch in completedBatches)
            {
                var activeParticipants = batchParticipants
                    .Where(p => p.BatchId == batch.Id && p.Status == BatchParticipantStatus.Active)
                    .ToList();

                foreach (var participant in activeParticipants)
                {
                    var offer = batch.Offer;
                    if (offer == null) continue;

                    var unitPrice = offer.UnitPrice;
                    var discount = offer.DiscountPercentage;
                    var totalAmount = participant.Quantity * unitPrice * (1 - discount / 100m);

                    orders.Add(new Order
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = participant.BuyerId,
                        BatchId = batch.Id,
                        OfferId = null,
                        Quantity = participant.Quantity,
                        TotalAmount = Math.Round(totalAmount, 2),
                        Status = OrderStatus.Paid,
                        PaidAt = batch.CompletedAt ?? batch.CreatedAt.AddDays(2),
                        CreatedAt = batch.CompletedAt ?? batch.CreatedAt.AddDays(2)
                    });
                }
            }

            // 2. Orders for accepted group request offers
            var acceptedOffers = groupRequestOffers.Where(o => o.Status == GroupRequestOfferStatus.Accepted).ToList();
            foreach (var offer in acceptedOffers)
            {
                var acceptedResponses = responses
                    .Where(r => r.OfferId == offer.Id && r.Response == BuyerOfferResponseType.Accepted)
                    .ToList();

                foreach (var response in acceptedResponses)
                {
                    var participant = groupRequestParticipants
                        .FirstOrDefault(p => p.GroupRequestId == offer.GroupRequestId && p.BuyerId == response.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

                    if (participant != null)
                    {
                        var totalAmount = participant.Quantity * offer.UnitPrice;

                        orders.Add(new Order
                        {
                            Id = Guid.NewGuid(),
                            BuyerId = response.BuyerId,
                            BatchId = null,
                            OfferId = offer.Id,
                            Quantity = participant.Quantity,
                            TotalAmount = Math.Round(totalAmount, 2),
                            Status = OrderStatus.Paid,
                            PaidAt = response.RespondedAt,
                            CreatedAt = response.RespondedAt
                        });
                    }
                }
            }

            _db.Orders.AddRange(orders);
        }

        private static string? GenerateVariantAttributes(string categoryName, Random random, double chance = 0.7)
        {
            if (!VariantGeneratorsByCategory.TryGetValue(categoryName, out var generator))
                return null; // category has no meaningful variants (e.g. Paper Products, Printing Supplies)

            if (random.NextDouble() > chance)
                return null; // this particular offer happens to not specify variants

            return JsonSerializer.Serialize(generator(random));
        }
        private static string SanitizeForEmail(string input) =>
            new string(input.Where(char.IsLetterOrDigit).ToArray());
        private static string Pick(Random r, params string[] options) => options[r.Next(options.Length)];

    }
}
