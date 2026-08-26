using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence;

public static class AppDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminUserAsync(userManager, logger);
        await SeedSampleTodosAsync(context, logger);
        await SeedDepotDomainAsync(context, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = ["Admin", "YardOperator", "Viewer", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@TechSpherex.dev";

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            FirstName = "Admin",
            LastName = "User",
            Email = adminEmail,
            UserName = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            await userManager.AddToRoleAsync(admin, "YardOperator");
            await userManager.AddToRoleAsync(admin, "Viewer");
            logger.LogInformation("Seeded admin user: {Email}", adminEmail);
        }
    }

    private static async Task SeedSampleTodosAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Todos.AnyAsync())
            return;

        var todos = new List<TodoItem>
        {
            new() { Title = "Explore the Clean Architecture template", Description = "Read through the layers: Domain → Application → Infrastructure → Api" },
            new() { Title = "Run the API with Aspire", Description = "Use 'dotnet run' in the AppHost project to start PostgreSQL, Redis, and the API" },
            new() { Title = "Try the Scalar API docs", Description = "Navigate to /scalar/v1 to explore and test the endpoints" },
            new() { Title = "Add your first feature", Description = "Create a new entity, command/query handlers, and endpoint following the Todos pattern" },
            new() { Title = "Check the architecture tests", Description = "Run 'dotnet test' to verify dependency rules are enforced" }
        };

        context.Todos.AddRange(todos);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} sample todos", todos.Count);
    }

    private static async Task SeedDepotDomainAsync(AppDbContext context, ILogger logger)
    {
        // Container types — ISO 6346 families (Phụ lục II).
        if (!await context.ContainerTypes.AnyAsync())
        {
            context.ContainerTypes.AddRange(
                new ContainerType { Code = "22G1", Name = "Dry 20' Standard", Family = "Dry", Description = "20ft general-purpose dry container" },
                new ContainerType { Code = "42G1", Name = "Dry 40' Standard", Family = "Dry", Description = "40ft general-purpose dry container" },
                new ContainerType { Code = "45G1", Name = "Dry 40' High Cube", Family = "Dry", Description = "40ft high-cube dry container" },
                new ContainerType { Code = "22R1", Name = "Reefer 20'", Family = "Reefer", Description = "20ft refrigerated container" },
                new ContainerType { Code = "42R1", Name = "Reefer 40'", Family = "Reefer", Description = "40ft refrigerated container" },
                new ContainerType { Code = "22U1", Name = "Open Top 20'", Family = "OpenTop", Description = "20ft open-top container" },
                new ContainerType { Code = "42U1", Name = "Open Top 40'", Family = "OpenTop", Description = "40ft open-top container" },
                new ContainerType { Code = "22P1", Name = "Flat Rack 20'", Family = "FlatRack", Description = "20ft flat-rack container" },
                new ContainerType { Code = "42P1", Name = "Flat Rack 40'", Family = "FlatRack", Description = "40ft flat-rack container" },
                new ContainerType { Code = "22B1", Name = "Bunker 20'", Family = "Bunker", Description = "20ft container for solid bulk" },
                new ContainerType { Code = "22V1", Name = "Ventilated 20'", Family = "Ventilated", Description = "20ft ventilated container" },
                new ContainerType { Code = "22S1", Name = "Specialized 20'", Family = "Specialized", Description = "Special-purpose 20ft container" });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded container types");
        }

        // Line operators.
        if (!await context.LineOperators.AnyAsync())
        {
            context.LineOperators.AddRange(
                new LineOperator { Code = "CMA", Name = "CMA CGM", Country = "France" },
                new LineOperator { Code = "MSK", Name = "Maersk", Country = "Denmark" },
                new LineOperator { Code = "MSC", Name = "MSC", Country = "Switzerland" },
                new LineOperator { Code = "HMM", Name = "HMM", Country = "South Korea" },
                new LineOperator { Code = "ONE", Name = "ONE", Country = "Singapore" },
                new LineOperator { Code = "YML", Name = "Yang Ming", Country = "Taiwan" },
                new LineOperator { Code = "COS", Name = "COSCO", Country = "China" },
                new LineOperator { Code = "EMC", Name = "Evergreen", Country = "Taiwan" },
                new LineOperator { Code = "APL", Name = "APL", Country = "Singapore" });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded line operators");
        }

        // One sample customer for the delivery-order workflow.
        if (!await context.Customers.AnyAsync())
        {
            context.Customers.Add(new Customer
            {
                TaxCode = "0101234567",
                Name = "Default Logistics Customer",
                Address = "Cat Lai, Thu Duc, Ho Chi Minh City",
                Phone = "+842812345678",
                Email = "operations@example.test",
                IsActive = true
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded sample customer");
        }

        // One default depot (multi-tenant mode still works — seed a single tenant).
        Depot depot;
        if (!await context.Depots.AnyAsync())
        {
            depot = new Depot
            {
                Code = "DEFAULT",
                Name = "Default Depot",
                Address = "Cat Lai, Thu Duc, Ho Chi Minh City",
                TimeZone = "Asia/Ho_Chi_Minh",
                IsActive = true
            };
            context.Depots.Add(depot);
            await context.SaveChangesAsync();

            // Seed 2 sample Blocks: A (real, 5 bays x 4 rows x 3 tiers) and B-VIRTUAL.
            var blockA = new Block
            {
                DepotId = depot.Id,
                Code = "A",
                Name = "Block A",
                IsVirtual = false,
                MaxBay = 5,
                MaxRow = 4,
                MaxTier = 3,
                DisplayOrder = 1
            };
            var blockVirtual = new Block
            {
                DepotId = depot.Id,
                Code = "V",
                Name = "Block V (Virtual)",
                IsVirtual = true,
                DisplayOrder = 2
            };
            context.Blocks.AddRange(blockA, blockVirtual);
            await context.SaveChangesAsync();

            // Pre-populate the slot grid for Block A.
            for (var bay = 1; bay <= 5; bay++)
            for (var row = 1; row <= 4; row++)
            for (var tier = 1; tier <= 3; tier++)
            {
                context.YardSlots.Add(new YardSlot
                {
                    BlockId = blockA.Id,
                    Bay = bay,
                    Row = row,
                    Tier = tier,
                    IsOccupied = false
                });
            }
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded default depot + 2 blocks");
        }

        // Backfill slots for any existing non-virtual blocks that lack slots
        var nonVirtualBlocks = await context.Blocks
            .Where(b => !b.IsVirtual && b.MaxBay != null && b.MaxRow != null && b.MaxTier != null)
            .ToListAsync();

        foreach (var blk in nonVirtualBlocks)
        {
            var hasSlots = await context.YardSlots.AnyAsync(s => s.BlockId == blk.Id);
            if (!hasSlots)
            {
                var newSlots = new List<YardSlot>();
                for (var bay = 1; bay <= blk.MaxBay!.Value; bay++)
                for (var row = 1; row <= blk.MaxRow!.Value; row++)
                for (var tier = 1; tier <= blk.MaxTier!.Value; tier++)
                {
                    newSlots.Add(new YardSlot
                    {
                        BlockId = blk.Id,
                        Bay = bay,
                        Row = row,
                        Tier = tier,
                        IsOccupied = false
                    });
                }
                context.YardSlots.AddRange(newSlots);
                await context.SaveChangesAsync();
                logger.LogInformation("Backfilled {Count} slots for block {Code}", newSlots.Count, blk.Code);
            }
        }
    }
}