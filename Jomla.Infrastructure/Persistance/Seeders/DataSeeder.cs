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
            ["Large Appliances"] = new[] { "Double-Door Refrigerator 450L", "Compact Mini Fridge 90L", "Side-by-Side Refrigerator 600L", "Chest Freezer 200L", "Upright Freezer 300L", "Front-Load Washing Machine 8kg", "Top-Load Washing Machine 10kg", "Built-In Dishwasher 12 Place Settings", "Freestanding Dishwasher 14 Place Settings", "Split Air Conditioner 1.5 Ton", "Window Air Conditioner 1 Ton" },
            ["Small Appliances"] = new[] { "Digital Microwave Oven 25L", "Convection Microwave Oven 30L", "Automatic Drip Coffee Maker", "Espresso Machine with Grinder", "Heavy-Duty Countertop Blender", "Commercial Smoothie Blender" },
            ["Laptops"] = new[] { "Business Laptop 14-inch Core i5", "Ultrabook 13-inch Core i7" },
            ["Computer Accessories"] = new[] { "Wireless Mouse and Keyboard Combo", "USB-C Docking Station", "Padded Laptop Backpack 15.6-inch", "Laptop Sleeve Case 14-inch" },
            ["Smartphones"] = new[] { "Android Smartphone 128GB", "Smartphone 256GB Dual SIM", "10-inch Android Tablet", "Tablet with Stylus 11-inch" },
            ["Cameras"] = new[] { "Digital DSLR Camera Kit", "4K Action Camera", "Tempered Glass Screen Protector Pack", "Fast Charging Power Bank 20000mAh" },
            ["Paper Products"] = new[] { "A4 Copy Paper Ream Pack (5x500)", "Sticky Notes Bulk Pack" },
            ["Writing Instruments"] = new[] { "Ballpoint Pens Box of 50", "Whiteboard Markers Set of 12" },
            ["Office Furniture"] = new[] { "Ergonomic Office Chair", "Adjustable Standing Desk" },
            ["Printing Supplies"] = new[] { "Toner Cartridge Multipack", "Inkjet Printer Ink Set" },
            ["Furniture"] = new[] { "3-Seater Fabric Sofa", "Dining Table Set (6-Seater)" },
            ["Bedding"] = new[] { "Queen Size Bedsheet Set", "Microfiber Comforter Set" },
            ["Home Decor"] = new[] { "Wall Art Canvas Set of 3", "Decorative Throw Pillow Set" },
            // Supermarket & Groceries
            ["Beverages"] = new[] { "Mineral Water 1.5L Case (12 Pack)", "Organic Orange Juice 1L Case (6 Pack)", "Premium Espresso Coffee Beans 1kg", "Assorted Soft Drinks Cans 330ml (24 Pack)" },
            ["Snacks & Sweets"] = new[] { "Assorted Protein Bars Box (12 Pack)", "Organic Dark Chocolate Bars (10 Pack)", "Bulk Roasted Mixed Nuts Bag 1kg", "Salted Potato Chips Family Pack (15 Pack)" },
            ["Pantry Staples"] = new[] { "Extra Virgin Olive Oil 5L Bottle", "Premium Basmati Rice 10kg Bag", "Canned Tomato Paste Bulk Case (24 Pack)", "Organic Penne Rigate Pasta 500g (12 Pack)" },
            ["Baby Care"] = new[] { "Premium Baby Diapers Pack (Size 4)", "Sensitive Baby Wipes Bulk (12x80 Pack)", "Organic Baby Formula Stage 1 800g" },
            // Home & Kitchen / Cookware & Dining
            ["Cookware & Dining"] = new[] { "Non-Stick Cookware Set (10-Piece)", "Stainless Steel Cutlery Set (24-Piece)", "Ceramic Dinnerware Set (16-Piece)", "Insulated Stainless Steel Water Bottle" },
            // Fashion & Apparel
            ["Clothing"] = new[] { "Unisex 100% Cotton Crewneck T-Shirt", "Corporate Polo Shirt Bulk Pack (5-Pack)", "Premium Denim Jeans Straight Fit", "Heavyweight Fleece Hoodie" },
            ["Footwear"] = new[] { "Ergonomic Leather Safety Shoes", "Casual Canvas Lace-Up Sneakers", "Classic Leather Dress Shoes", "Lightweight Running Athletic Shoes" },
            ["Bags & Accessories"] = new[] { "Waterproof Travel Duffle Bag", "Classic Leather Wallet & Belt Gift Set", "Hard Shell Spinner Suitcase (3-Piece Set)" }
        };

        private static readonly Dictionary<string, string[]> GroupRequestTitlesByCategory = new()
        {
            ["Large Appliances"] = new[] { "Double-door refrigerators for branch offices", "Chest freezers for restaurant supply", "Front-load washing machines for staff housing", "Commercial dishwashers for cafeteria", "Split ACs for new office building" },
            ["Small Appliances"] = new[] { "Microwaves for office pantry", "Coffee machines for office branches", "Blenders for juice bar chain" },
            ["Laptops"] = new[] { "Business laptops for new hires", "Laptops for training center" },
            ["Computer Accessories"] = new[] { "Wireless mouse and keyboard sets for office", "Laptop backpacks for onboarding kits" },
            ["Smartphones"] = new[] { "Company phones for sales team", "Tablets for retail checkout stations" },
            ["Cameras"] = new[] { "Security cameras for warehouse", "Power banks and accessories for field staff" },
            ["Paper Products"] = new[] { "A4 paper reams for office restock" },
            ["Writing Instruments"] = new[] { "Pens and markers for branches" },
            ["Office Furniture"] = new[] { "Office chairs for new branch", "Standing desks for HQ renovation" },
            ["Printing Supplies"] = new[] { "Toner cartridges for office printers" },
            ["Furniture"] = new[] { "Sofas for office lounge", "Dining tables for cafeteria" },
            ["Bedding"] = new[] { "Bedsheets for staff dormitory" },
            ["Home Decor"] = new[] { "Wall art for office decoration", "Throw pillows for lounge area" },
            ["Beverages"] = new[] { "Bottled water and juices for offices", "Espresso coffee beans for office coffee machines" },
            ["Snacks & Sweets"] = new[] { "Snack bars and dark chocolates for pantry", "Mixed nuts bulk supply for kitchen" },
            ["Pantry Staples"] = new[] { "Bulk olive oil and basmati rice for restaurant", "Pasta and tomato paste cases for cafeteria" },
            ["Baby Care"] = new[] { "Diapers and wipes for daycare center restock", "Organic baby formula for nurseries" },
            ["Cookware & Dining"] = new[] { "Dinnerware and cutlery sets for office kitchen", "Non-stick cookware for staff cooking classes" },
            ["Clothing"] = new[] { "Uniform crewneck t-shirts for staff", "Corporate polo shirts for event" },
            ["Footwear"] = new[] { "Safety boots for logistics warehouse", "Casual sneakers for event staff" },
            ["Bags & Accessories"] = new[] { "Waterproof duffle bags for fitness club", "Travel suitcases for sales team trips" }
        };

        private static readonly Dictionary<string, Func<Random, Dictionary<string, string>>> VariantGeneratorsByCategory = new()
        {
            ["Large Appliances"] = r => new() { ["color"] = Pick(r, "Silver", "White", "Black"), ["capacity"] = Pick(r, "350L", "450L", "600L", "10kg", "1.5 Ton") },
            ["Small Appliances"] = r => new() { ["color"] = Pick(r, "Black", "Silver", "Red"), ["capacity"] = Pick(r, "25L", "30L") },
            ["Laptops"] = r => new() { ["color"] = Pick(r, "Silver", "Black", "Gray"), ["storage"] = Pick(r, "256GB", "512GB", "1TB"), ["ram"] = Pick(r, "8GB", "16GB") },
            ["Computer Accessories"] = r => new() { ["color"] = Pick(r, "Black", "Gray", "Navy") },
            ["Smartphones"] = r => new() { ["color"] = Pick(r, "Black", "White", "Blue", "Gold"), ["storage"] = Pick(r, "64GB", "128GB", "256GB") },
            ["Cameras"] = r => new() { ["color"] = Pick(r, "Black", "Silver") },
            ["Office Furniture"] = r => new() { ["color"] = Pick(r, "Black", "Gray", "Brown"), ["material"] = Pick(r, "Mesh", "Leather", "Fabric") },
            ["Furniture"] = r => new() { ["color"] = Pick(r, "Gray", "Beige", "Brown"), ["material"] = Pick(r, "Fabric", "Leather") },
            ["Bedding"] = r => new() { ["color"] = Pick(r, "White", "Gray", "Blue"), ["size"] = Pick(r, "Twin", "Queen", "King") },
            ["Home Decor"] = r => new() { ["color"] = Pick(r, "Multicolor", "Beige", "Gray") },
            ["Beverages"] = r => new() { ["flavor"] = Pick(r, "Natural", "Orange", "Espresso", "Cola"), ["size"] = Pick(r, "1.5L", "1L", "1kg", "330ml") },
            ["Snacks & Sweets"] = r => new() { ["type"] = Pick(r, "Protein", "Dark Chocolate", "Mixed Roasted", "Salted Chips") },
            ["Pantry Staples"] = r => new() { ["volume/weight"] = Pick(r, "5L", "10kg", "Case of 24", "Pack of 12") },
            ["Baby Care"] = r => new() { ["size/type"] = Pick(r, "Diapers Size 4", "Wipes Bulk Case", "Formula Stage 1") },
            ["Cookware & Dining"] = r => new() { ["material"] = Pick(r, "Non-Stick", "Stainless Steel", "Ceramic") },
            ["Clothing"] = r => new() { ["color"] = Pick(r, "Black", "Navy", "White", "Heather Gray"), ["size"] = Pick(r, "S", "M", "L", "XL") },
            ["Footwear"] = r => new() { ["color"] = Pick(r, "Black", "Brown", "White", "Navy"), ["size"] = Pick(r, "40", "41", "42", "43", "44") },
            ["Bags & Accessories"] = r => new() { ["color"] = Pick(r, "Black", "Navy", "Charcoal") }
        };

        public async Task<bool> SeedAsync()
        {
            if (await _db.Users.AnyAsync())
                return false; // DB has been seeded
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
            var supermarket = new Category { Id = Guid.NewGuid(), Name = "Supermarket & Groceries" };
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

            // Level 2 Subcategories under Supermarket & Groceries
            var beverages = new Category { Id = Guid.NewGuid(), Name = "Beverages", ParentId = supermarket.Id };
            var snacksSweets = new Category { Id = Guid.NewGuid(), Name = "Snacks & Sweets", ParentId = supermarket.Id };
            var pantryStaples = new Category { Id = Guid.NewGuid(), Name = "Pantry Staples", ParentId = supermarket.Id };
            var babyCare = new Category { Id = Guid.NewGuid(), Name = "Baby Care", ParentId = supermarket.Id };

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
                electronics, supermarket, homeKitchen, officeSupplies, fashion, other,
                laptops, smartphones, largeAppliances, smallAppliances, computerAccessories, cameras,
                beverages, snacksSweets, pantryStaples, babyCare,
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

            const int requestCount = 60;

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
            if (!VariantGeneratorsByCategory.TryGetValue(categoryName, out var generator))
                return null; // category has no meaningful variants (e.g. Paper Products, Printing Supplies)

            if (random.NextDouble() > chance)
                return null; // this particular offer happens to not specify variants

            return JsonSerializer.Serialize(generator(random));
        }
        private static string SanitizeForEmail(string input) =>
            new string(input.Where(char.IsLetterOrDigit).ToArray());
        private static string Pick(Random r, params string[] options) => options[r.Next(options.Length)];

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
