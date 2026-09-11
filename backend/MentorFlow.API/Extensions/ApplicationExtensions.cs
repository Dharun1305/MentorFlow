using Microsoft.AspNetCore.Identity;
using MentorFlow.API.Data;
using MentorFlow.API.Entities;

namespace MentorFlow.API.Extensions;

public static class ApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MentorFlowDbContext>();
        if (!await db.Database.CanConnectAsync())
        {
            throw new InvalidOperationException("MentorFlowDb is not reachable. Run database/MentorFlowDb.sql against SQL Server before starting the API.");
        }
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
    }
}
