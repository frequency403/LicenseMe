using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Cache.Context;

public class LicenseDbContext(DbContextOptions<LicenseDbContext> options) : DbContext(options)
{
    private const string LicenseTextTableName = "LicenseText";
    private const string TimestampTableName = "OsiLicenseTimestamp";
    public const string LastUpdatedPropertyName = "LastUpdatedUtc";

    public DbSet<OsiLicense> Licenses { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // OsiLicense.Links etc. carry [JsonPropertyName] for the OSI REST payload, not for JSON-column
        // mapping. The attribute convention picks it up anyway and fails validation because Links is a
        // top-level (non-nested) owned navigation mapped to plain columns, not a JSON column.
        configurationBuilder.Conventions.Remove(typeof(RelationalNavigationJsonPropertyNameAttributeConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stewardsConverter = new ValueConverter<IReadOnlyCollection<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
        );

        var keywordsConverter = new ValueConverter<IReadOnlyCollection<OsiLicenseKeyword>, string>(
            v => JsonSerializer.Serialize(v.Select(k => (int)k), (JsonSerializerOptions?)null),
            v => (JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null).Select(i => (OsiLicenseKeyword)i).ToList())
        );
        modelBuilder.Entity<OsiLicense>(entityBuilder =>
        {
            entityBuilder.ToTable(nameof(OsiLicense));
            entityBuilder.HasKey(license => license.Id);
            entityBuilder.Property(license => license.Id).ValueGeneratedNever();

            entityBuilder.OwnsOne(license => license.Links, linksBuilder =>
            {
                linksBuilder.OwnsOne(links => links.Self);
                linksBuilder.OwnsOne(links => links.Html);
                linksBuilder.OwnsOne(links => links.Collection);
            });

            entityBuilder.Property(license => license.Keywords).HasConversion(keywordsConverter);
            entityBuilder.Property(license => license.Stewards).HasConversion(stewardsConverter);
            
            entityBuilder.Property<DateTime>(LastUpdatedPropertyName)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entityBuilder.SplitToTable(LicenseTextTableName, tableBuilder =>
            {
                tableBuilder.Property(license => license.LicenseText);
            });

            entityBuilder.SplitToTable(TimestampTableName, tableBuilder =>
            {
                tableBuilder.Property<DateTime>(LastUpdatedPropertyName).HasColumnName("Timestamp");
            });
        });
    }
}
