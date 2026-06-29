using Microsoft.EntityFrameworkCore;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Cache.Context;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext()
    {
        
    }

    public DbSet<OsiLicense> Licenses { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var osiLicenseName = nameof(OsiLicense);
        var osiLicenseIdName = osiLicenseName + nameof(OsiLicense.Id);
        var osiLicenseLicenseTextName = osiLicenseName + nameof(OsiLicense.LicenseText);
        
        modelBuilder.Entity<OsiLicense>(entityBuilder =>
        {
            entityBuilder
                .ToTable(osiLicenseName)
                .SplitToTable(osiLicenseLicenseTextName, tableBuilder =>
                {
                    tableBuilder.Property(license => license.Id).HasColumnName(osiLicenseIdName);
                    tableBuilder.Property(license => license.LicenseText);
                });
            
            entityBuilder.HasKey(e => e.Id);
            entityBuilder.Property(e => e.Id).ValueGeneratedNever().HasColumnName(osiLicenseIdName);
        });
    }
}