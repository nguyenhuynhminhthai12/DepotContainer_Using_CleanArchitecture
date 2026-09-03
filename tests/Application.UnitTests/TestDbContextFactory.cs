/**
 * Lớp Factory tạo AppDbContext cho Unit Tests.
 * Sử dụng InMemory Database của Entity Framework Core để test không phụ thuộc database thật.
 * Mỗi lần gọi Create() sẽ tạo một database mới với tên ngẫu nhiên (Guid) để đảm bảo isolation giữa các test.
 * Bản quyền (c) 2026 TechSpherex.
 */
using Microsoft.EntityFrameworkCore;
using TechSpherex.CleanArchitecture.Infrastructure.Persistence;

namespace TechSpherex.CleanArchitecture.Application.UnitTests;
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
