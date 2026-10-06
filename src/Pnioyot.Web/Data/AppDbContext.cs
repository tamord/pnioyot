using Microsoft.EntityFrameworkCore;
using Pnioyot.Web.Models;

namespace Pnioyot.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var inquiry = modelBuilder.Entity<Inquiry>();

        inquiry.ToTable("Inquiries");
        inquiry.HasKey(x => x.Id);

        inquiry.Property(x => x.Title).HasMaxLength(200).IsRequired();
        inquiry.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
        inquiry.Property(x => x.AssignedTo).HasMaxLength(100).IsRequired();
        inquiry.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        inquiry.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
        inquiry.Property(x => x.RowVersion).IsRowVersion();

        inquiry.HasIndex(x => x.Status);
        inquiry.HasIndex(x => x.Priority);
        inquiry.HasIndex(x => x.OrganizationName);
        inquiry.HasIndex(x => x.AssignedTo);
        inquiry.HasIndex(x => x.CreatedAt);
        inquiry.HasIndex(x => new { x.Status, x.Priority, x.CreatedAt }).HasDatabaseName("IX_Inquiries_Status_Priority_CreatedAt");
    }
}
