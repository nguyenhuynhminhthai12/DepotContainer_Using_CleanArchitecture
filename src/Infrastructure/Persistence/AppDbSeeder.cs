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
        await SeedContainerTypesAsync(context, logger);
        await SeedLineOperatorsAsync(context, logger);
        await SeedSampleCustomerAsync(context, logger);
        await SeedDefaultDepotAsync(context, logger);
    }

    private static async Task SeedContainerTypesAsync(AppDbContext context, ILogger logger)
    {
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
    }

    private static async Task SeedLineOperatorsAsync(AppDbContext context, ILogger logger)
    {
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
    }

    private static async Task SeedSampleCustomerAsync(AppDbContext context, ILogger logger)
    {
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
    }

    private static async Task SeedDefaultDepotAsync(AppDbContext context, ILogger logger)
    {
        if (!await context.Depots.AnyAsync())
        {
            var depot = await CreateDefaultDepotAsync(context);
            await SeedBlockASlotsAsync(context, depot);
            logger.LogInformation("Seeded default depot + 2 blocks");
        }

        await BackfillMissingSlotsAsync(context, logger);
    }

    private static async Task<Depot> CreateDefaultDepotAsync(AppDbContext context)
    {
        var depot = new Depot
        {
            Code = "DEFAULT",
            Name = "Default Depot",
            Address = "Cat Lai, Thu Duc, Ho Chi Minh City",
            TimeZone = "Asia/Ho_Chi_Minh",
            IsActive = true
        };
        context.Depots.Add(depot);
        await context.SaveChangesAsync();

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

        return depot;
    }

    private static async Task SeedBlockASlotsAsync(AppDbContext context, Depot depot)
    {
        var blockA = await context.Blocks.FirstAsync(b => b.Code == "A" && b.DepotId == depot.Id);

        for (var bay = 1; bay <= 5; bay++)
        {
            for (var row = 1; row <= 4; row++)
            {
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
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task BackfillMissingSlotsAsync(AppDbContext context, ILogger logger)
    {
        var nonVirtualBlocks = await context.Blocks
            .Where(b => !b.IsVirtual && b.MaxBay != null && b.MaxRow != null && b.MaxTier != null)
            .ToListAsync();

        foreach (var blk in nonVirtualBlocks)
        {
            var hasSlots = await context.YardSlots.AnyAsync(s => s.BlockId == blk.Id);
            if (!hasSlots)
            {
                var newSlots = CreateBlockSlots(blk);
                context.YardSlots.AddRange(newSlots);
                await context.SaveChangesAsync();
                logger.LogInformation("Backfilled {Count} slots for block {Code}", newSlots.Count, blk.Code);
            }
        }
    }

    private static List<YardSlot> CreateBlockSlots(Block block)
    {
        var slots = new List<YardSlot>();
        for (var bay = 1; bay <= block.MaxBay!.Value; bay++)
        {
            for (var row = 1; row <= block.MaxRow!.Value; row++)
            {
                for (var tier = 1; tier <= block.MaxTier!.Value; tier++)
                {
                    slots.Add(new YardSlot
                    {
                        BlockId = block.Id,
                        Bay = bay,
                        Row = row,
                        Tier = tier,
                        IsOccupied = false
                    });
                }
            }
        }
        return slots;
    }
}