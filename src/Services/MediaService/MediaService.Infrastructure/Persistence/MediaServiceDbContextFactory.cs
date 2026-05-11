namespace MediaService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class MediaServiceDbContextFactory : IDesignTimeDbContextFactory<MediaServiceDbContext>
{
    public MediaServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MediaServiceDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MediaService")
            ?? "Host=localhost;Port=5432;Database=mediaservice;Username=photoapp_user;Password=photoapp_secure_password_123!";

        optionsBuilder.UseNpgsql(connectionString);

        return new MediaServiceDbContext(optionsBuilder.Options);
    }
}