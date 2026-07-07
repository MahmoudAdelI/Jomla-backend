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
        // Static dictionaries moved to embedded JSON resource files

        public async Task<bool> SeedAsync()
        {
            if (await _db.Users.AnyAsync())
            {
                // Backfill existing users with generated Address and Phone if missing
                var usersToFix = await _db.Users
                    .Where(u => string.IsNullOrEmpty(u.ShippingAddress) || string.IsNullOrEmpty(u.PhoneNumber))
                    .ToListAsync();

                if (usersToFix.Any())
                {
                    var faker = new Faker();
                    foreach (var user in usersToFix)
                    {
                        if (string.IsNullOrEmpty(user.ShippingAddress))
                        {
                            user.ShippingAddress = faker.Address.FullAddress();
                        }
                        if (string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            user.PhoneNumber = faker.Phone.PhoneNumber();
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                // Backfill existing batch participants with address/phone if missing
                var participantsToFix = await _db.BatchParticipants
                    .Include(p => p.Buyer)
                    .Where(p => string.IsNullOrEmpty(p.ShippingAddress) || string.IsNullOrEmpty(p.PhoneNumber))
                    .ToListAsync();

                if (participantsToFix.Any())
                {
                    var faker = new Faker();
                    foreach (var part in participantsToFix)
                    {
                        if (string.IsNullOrEmpty(part.ShippingAddress))
                        {
                            part.ShippingAddress = part.Buyer.ShippingAddress ?? faker.Address.FullAddress();
                        }
                        if (string.IsNullOrEmpty(part.PhoneNumber))
                        {
                            part.PhoneNumber = part.Buyer.PhoneNumber ?? faker.Phone.PhoneNumber();
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                return false; // DB has been seeded
            }
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
            
            // Calculate and assign AcceptedQuantity for each group request offer
            foreach (var offer in groupRequestOffers)
            {
                var acceptedBuyerIds = responses
                    .Where(r => r.OfferId == offer.Id && r.Response == BuyerOfferResponseType.Accepted)
                    .Select(r => r.BuyerId)
                    .ToHashSet();

                var acceptedQuantity = groupRequestParticipants
                    .Where(p => p.GroupRequestId == offer.GroupRequestId &&
                                acceptedBuyerIds.Contains(p.BuyerId) &&
                                p.Status == GroupRequestParticipantStatus.Active)
                    .Sum(p => p.Quantity);

                offer.AcceptedQuantity = acceptedQuantity;
            }

            await _db.SaveChangesAsync();

            SeedOrders(batches, batchParticipants, groupRequestOffers, responses, groupRequestParticipants);
            await _db.SaveChangesAsync();

            return true;
        }

        private List<Category> SeedCategories()
        {
            // Level 1 Departments
            var electronics = new Category { Id = Guid.NewGuid(), Name = "Electronics & Appliances" };
            var homeKitchen = new Category { Id = Guid.NewGuid(), Name = "Home & Kitchen" };
            var officeSupplies = new Category { Id = Guid.NewGuid(), Name = "Office Supplies" };
            var fashion = new Category { Id = Guid.NewGuid(), Name = "Fashion & Apparel" };
            var other = new Category { Id = Guid.NewGuid(), Name = "Other" };

            // Level 2 Subcategories under Electronics & Appliances
            var laptops = new Category { Id = Guid.NewGuid(), Name = "Laptops", ParentId = electronics.Id };
            var smartphones = new Category { Id = Guid.NewGuid(), Name = "Smartphones", ParentId = electronics.Id };
            var largeAppliances = new Category { Id = Guid.NewGuid(), Name = "Large Appliances", ParentId = electronics.Id };
            var smallAppliances = new Category { Id = Guid.NewGuid(), Name = "Small Appliances", ParentId = electronics.Id };
            var computerAccessories = new Category { Id = Guid.NewGuid(), Name = "Computer Accessories", ParentId = electronics.Id };
            var cameras = new Category { Id = Guid.NewGuid(), Name = "Cameras", ParentId = electronics.Id };

            // Level 2 Subcategories under Home & Kitchen
            var furniture = new Category { Id = Guid.NewGuid(), Name = "Furniture", ParentId = homeKitchen.Id };
            var bedding = new Category { Id = Guid.NewGuid(), Name = "Bedding", ParentId = homeKitchen.Id };
            var homeDecor = new Category { Id = Guid.NewGuid(), Name = "Home Decor", ParentId = homeKitchen.Id };
            var cookwareDining = new Category { Id = Guid.NewGuid(), Name = "Cookware & Dining", ParentId = homeKitchen.Id };

            // Level 2 Subcategories under Office Supplies
            var paperProducts = new Category { Id = Guid.NewGuid(), Name = "Paper Products", ParentId = officeSupplies.Id };
            var writingInstruments = new Category { Id = Guid.NewGuid(), Name = "Writing Instruments", ParentId = officeSupplies.Id };
            var officeFurniture = new Category { Id = Guid.NewGuid(), Name = "Office Furniture", ParentId = officeSupplies.Id };
            var printingSupplies = new Category { Id = Guid.NewGuid(), Name = "Printing Supplies", ParentId = officeSupplies.Id };

            // Level 2 Subcategories under Fashion & Apparel
            var clothing = new Category { Id = Guid.NewGuid(), Name = "Clothing", ParentId = fashion.Id };
            var footwear = new Category { Id = Guid.NewGuid(), Name = "Footwear", ParentId = fashion.Id };
            var bagsAccessories = new Category { Id = Guid.NewGuid(), Name = "Bags & Accessories", ParentId = fashion.Id };

            var categories = new List<Category>
            {
                electronics, homeKitchen, officeSupplies, fashion, other,
                laptops, smartphones, largeAppliances, smallAppliances, computerAccessories, cameras,
                furniture, bedding, homeDecor, cookwareDining,
                paperProducts, writingInstruments, officeFurniture, printingSupplies,
                clothing, footwear, bagsAccessories
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
            var faker = new Faker();
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = first,
                LastName = last,
                CreatedAt = createdAt,
                ShippingAddress = faker.Address.FullAddress(),
                PhoneNumber = faker.Phone.PhoneNumber()
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

            var leafCategories = categories
                .Where(c => !categories.Any(child => child.ParentId == c.Id) && c.Name != "Other")
                .ToList();

            var appliances = leafCategories
                .Where(c => c.Name == "Large Appliances" || c.Name == "Small Appliances")
                .ToList();

            foreach(var supplier in suppliers)
            {
                var pickCount = random.Next(1, 5); // 1 to 4 categories per seller
                var pickedCategories = new List<Category>();

                // 80% chance to prefer at least one appliance category
                if (random.NextDouble() < 0.8 && appliances.Count > 0)
                {
                    pickedCategories.Add(appliances[random.Next(appliances.Count)]);
                }

                var remainingLeafs = leafCategories
                    .Except(pickedCategories)
                    .OrderBy(_ => random.Next())
                    .ToList();

                pickedCategories.AddRange(remainingLeafs.Take(Math.Max(0, pickCount - pickedCategories.Count)));

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
                .Where(c => !categories.Any(child => child.ParentId == c.Id) && c.Name != "Other")
                .ToList();

            var random = new Random(45);
            var offers = new List<SupplierOffer>();

            foreach (var supplier in suppliers)
            {
                if (offers.Count >= 20)
                    break;

                var offerCount = random.Next(1, 4); // 1-3 offers per supplier

                for (int i = 0; i < offerCount; i++)
                {
                    if (offers.Count >= 20)
                        break;

                    Category category;
                    var applianceCategories = leafCategories
                        .Where(c => c.Name == "Large Appliances" || c.Name == "Small Appliances")
                        .ToList();

                    if (random.NextDouble() < 0.75 && applianceCategories.Count > 0)
                    {
                        category = applianceCategories[random.Next(applianceCategories.Count)];
                    }
                    else
                    {
                        category = leafCategories[random.Next(leafCategories.Count)];
                    }

                    var itemPool = SeedDataLoader.Products[category.Name];
                    var item = itemPool[random.Next(itemPool.Count)];
                    var title = item.Title;

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
                        ImageUrls = JsonSerializer.Serialize(new[] { item.ImageUrl }),
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
                        // 70% Open, 20% Completed, 10% Failed — so most offers have a live batch
                        var roll = random.Next(0, 10);
                        if (roll < 7)
                        {
                            status = BatchStatus.Open;
                            currentQuantity = random.Next(0, targetQuantity); // partially filled, still open
                        }
                        else if (roll < 9)
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
                                JoinedAt = batchCreatedAt.AddHours(random.Next(1, 96)),
                                ShippingAddress = buyer.ShippingAddress,
                                PhoneNumber = buyer.PhoneNumber
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
                .Where(c => !categories.Any(child => child.ParentId == c.Id) && c.Name != "Other")
                .ToList();

            var random = new Random(47);
            var groupRequests = new List<GroupRequest>();
            var allParticipants = new List<GroupRequestParticipant>();

            const int requestCount = 60;

            for (int i = 0; i < requestCount; i++)
            {
                var initiator = buyers[random.Next(buyers.Count)];
                Category category;
                var applianceCategories = leafCategories
                    .Where(c => c.Name == "Large Appliances" || c.Name == "Small Appliances")
                    .ToList();

                if (random.NextDouble() < 0.75 && applianceCategories.Count > 0)
                {
                    category = applianceCategories[random.Next(applianceCategories.Count)];
                }
                else
                {
                    category = leafCategories[random.Next(leafCategories.Count)];
                }

                var itemPool = SeedDataLoader.GroupRequestTitles[category.Name];
                var item = itemPool[random.Next(itemPool.Count)];
                var title = item.Title;
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
                    ImageUrls = JsonSerializer.Serialize(new[] { item.ImageUrl }),
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
                .Take(35)
                .ToList();

            int candidateIndex = 0;
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
                var categoryName = categoryById[request.CategoryId].Name;

                // Simulate multi-round negotiations (1 to 4 rounds) with varying final statuses
                int totalRounds = 1;
                GroupRequestOfferStatus finalStatus = GroupRequestOfferStatus.Open;

                if (request.Status == GroupRequestStatus.Closed)
                {
                    // Closed request means final round ended in Accepted
                    finalStatus = GroupRequestOfferStatus.Accepted;
                    totalRounds = (candidateIndex % 4) + 1; // 1, 2, 3, or 4 rounds
                }
                else
                {
                    // Active request: can end in Open or Expired after 1-4 rounds
                    var typeRoll = candidateIndex % 3;
                    if (typeRoll == 0)
                    {
                        totalRounds = (candidateIndex % 3) + 1; // 1, 2, or 3 rounds
                        finalStatus = GroupRequestOfferStatus.Open;
                    }
                    else if (typeRoll == 1)
                    {
                        totalRounds = (candidateIndex % 3) + 2; // 2, 3, or 4 rounds
                        finalStatus = GroupRequestOfferStatus.Expired;
                    }
                    else
                    {
                        totalRounds = (candidateIndex % 4) + 1; // 1, 2, 3, or 4 rounds
                        finalStatus = GroupRequestOfferStatus.Open;
                    }
                }

                Guid? parentId = null;
                var currentPrice = unitPrice;
                var floor = Math.Round(unitPrice * 0.80m, 2); // floor is 20% below opening price
                var date = request.CreatedAt.AddDays(random.Next(1, 3));

                for (int round = 1; round <= totalRounds; round++)
                {
                    var isLastRound = round == totalRounds;
                    var roundStatus = isLastRound ? finalStatus : GroupRequestOfferStatus.Countered;
                    
                    if (round > 1)
                    {
                        // drop price for revised rounds
                        var step = (unitPrice - floor) / (totalRounds - 1 + 0.1m);
                        currentPrice = Math.Round(unitPrice - (step * (round - 1)), 2);
                        currentPrice = Math.Max(currentPrice, floor);
                    }

                    var offerId = Guid.NewGuid();
                    var expiresAt = date.AddDays(5);

                    var offer = new GroupRequestOffer
                    {
                        Id = offerId,
                        GroupRequestId = request.Id,
                        SupplierId = supplierId,
                        UnitPrice = unitPrice,
                        MinUnitPrice = floor,
                        CurrentUnitPrice = currentPrice,
                        QuantityAvailable = request.CurrentQuantity + random.Next(0, 11),
                        MinFallbackQuantity = random.Next(0, 2) == 1 ? (int)(request.CurrentQuantity * 0.6) : null,
                        VariantAttributes = GenerateVariantAttributes(categoryName, random),
                        RoundNumber = round,
                        ParentId = parentId,
                        Status = roundStatus,
                        CreatedAt = date,
                        ExpiresAt = expiresAt
                    };

                    offers.Add(offer);

                    // Add negotiation log for rounds > 1 (the counter step)
                    if (round > 1)
                    {
                        var previousRoundPrice = offers.First(o => o.Id == parentId).CurrentUnitPrice;
                        var log = new NegotiationLog
                        {
                            Id = Guid.NewGuid(),
                            OfferId = offerId,
                            PreviousPrice = previousRoundPrice,
                            NewPrice = currentPrice,
                            ReasoningSummary = $"AI Agent countered with a discounted price of {currentPrice:C} (Round {round}) based on market response and a floor of {floor:C}.",
                            ActedAt = date
                        };
                        _db.NegotiationLogs.Add(log);
                    }

                    parentId = offerId;
                    date = expiresAt.AddHours(random.Next(1, 12)); // next round starts after previous round expires/counters
                }

                candidateIndex++;
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
                    else if (offer.Status == GroupRequestOfferStatus.Countered)
                    {
                        // Countered offers were rejected/ignored, let's seed mostly rejections
                        var roll = random.Next(0, 10);
                        if (roll < 6) response = BuyerOfferResponseType.Rejected;
                        else continue; // no response (ignored)
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
                        var totalAmount = participant.Quantity * offer.CurrentUnitPrice; // FIX: use CurrentUnitPrice

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
            if (!SeedDataLoader.VariantTemplates.TryGetValue(categoryName, out var attributes))
                return null; // category has no meaningful variants (e.g. Paper Products, Printing Supplies)

            if (random.NextDouble() > chance)
                return null; // this particular offer happens to not specify variants

            var result = attributes.ToDictionary(
                kv => kv.Key,
                kv => kv.Value[random.Next(kv.Value.Count)]
            );

            return JsonSerializer.Serialize(result);
        }
        private static string SanitizeForEmail(string input) =>
            new string(input.Where(char.IsLetterOrDigit).ToArray());

        public async Task SeedAdminAsync()
        {
            const string adminEmail = "admin@jomla.test";

            if (await _userManager.FindByEmailAsync(adminEmail) != null)
                return;

            
            var roleName = UserRole.Admin.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });
            }

            var admin = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Jomla",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(admin, "Admin123!");
            if (!result.Succeeded)
                throw new Exception($"Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            await _userManager.AddToRoleAsync(admin, roleName);
        }

    }
}
