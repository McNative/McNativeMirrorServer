using Microsoft.EntityFrameworkCore;

namespace McNativeMirrorServer.Model
{
    public class ResourceContext : DbContext {

        public ResourceContext(DbContextOptions<ResourceContext> options) : base(options){}

        public DbSet<Resource> Resources { get; set; }

        public DbSet<ResourceVersion> ResourceVersions { get; set; }

        public DbSet<LicenseActive> ActiveLicense { get; set; }

        public DbSet<LicenseIssued> LicenceIssued { get; set; }

        public DbSet<LicenseResource> LicenseResources { get; set; }

        public DbSet<License> Licenses { get; set; }

        public DbSet<LicenseResource> SubscriptionResources { get; set; }

        public DbSet<Server> Servers { get; set; }

        public DbSet<RolloutServer> RolloutServers { get; set; }

        public DbSet<Organisation> Organisations { get; set; }

        public DbSet<RolloutProfile> Profiles { get; set; }

        public DbSet<Template> Templates { get; set; }

        public DbSet<AliveReport> AliveReports { get; set; }

        public DbSet<SystemLoaders> SystemLoaders { get; set; }

        public DbSet<ResourceDownload> ResourceDownloads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("mcnative");
            modelBuilder.Entity<Server>().ToTable("mcnative_server");
            modelBuilder.Entity<Resource>().ToTable("mcnative_resource");
            modelBuilder.Entity<ResourceVersion>().ToTable("mcnative_resource_versions");
            modelBuilder.Entity<ResourceEdition>().ToTable("mcnative_resource_editions");
            modelBuilder.Entity<License>().ToTable("mcnative_license");
            modelBuilder.Entity<LicenseActive>().ToTable("mcnative_license_active");
            modelBuilder.Entity<LicenseIssued>().ToTable("mcnative_license_issued");
            modelBuilder.Entity<LicenseResource>().ToTable("mcnative_license_resources");
            modelBuilder.Entity<Organisation>().ToTable("mcnative_organisation");
            modelBuilder.Entity<RolloutServer>().ToTable("mcnative_organisation_rollout_servers");
            modelBuilder.Entity<AliveReport>().ToTable("mcnative_resource_reporting");
            modelBuilder.Entity<RolloutProfile>().ToTable("mcnative_organisation_rollout_profiles");
            modelBuilder.Entity<Template>().ToTable("mcnative_templates");
            modelBuilder.Entity<ResourceDownload>().ToTable("mcnative_resource_downloads").HasKey(a => new { a.ResourceId, a.IpAddressHash }); ;
            modelBuilder.Entity<SystemLoaders>().ToTable("system_loader-build-service_loaders");
        }

    }
}
