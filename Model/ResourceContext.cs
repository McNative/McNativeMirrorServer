using Microsoft.EntityFrameworkCore;

namespace McNativeMirrorServer.Model
{
    public class ResourceContext : DbContext {

        public ResourceContext(DbContextOptions<ResourceContext> options) : base(options){}

        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceVersion> ResourceVersions { get; set; }

        public DbSet<License> Licenses { get; set; }

        public DbSet<LicenseIssued> LicenceIssued { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<SubscriptionActive> SubscriptionActives { get; set; }

        public DbSet<SubscriptionResource> SubscriptionResources { get; set; }

        public DbSet<Server> Servers { get; set; }

        public DbSet<RolloutServer> RolloutServers { get; set; }

        public DbSet<Organisation> Organisations { get; set; }

        public DbSet<RolloutProfile> Profiles { get; set; }

        public DbSet<Template> Templates { get; set; }

        public DbSet<AliveReport> AliveReports { get; set; }

        public DbSet<SystemLoaders> SystemLoaders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("mcnative");
            modelBuilder.Entity<Server>().ToTable("mcnative_server");
            modelBuilder.Entity<Resource>().ToTable("mcnative_resource");
            modelBuilder.Entity<ResourceVersion>().ToTable("mcnative_resource_versions");
            modelBuilder.Entity<ResourceEdition>().ToTable("mcnative_resource_editions");
            modelBuilder.Entity<License>().ToTable("mcnative_license");
            modelBuilder.Entity<LicenseIssued>().ToTable("mcnative_license_issued");
            modelBuilder.Entity<Subscription>().ToTable("mcnative_subscription");
            modelBuilder.Entity<SubscriptionActive>().ToTable("mcnative_subscription_active");
            modelBuilder.Entity<SubscriptionResource>().ToTable("mcnative_subscription_resources");
            modelBuilder.Entity<Organisation>().ToTable("mcnative_organisation");
            modelBuilder.Entity<RolloutServer>().ToTable("mcnative_organisation_rollout_servers");
            modelBuilder.Entity<AliveReport>().ToTable("mcnative_resource_reporting");
            modelBuilder.Entity<RolloutProfile>().ToTable("mcnative_organisation_rollout_profiles");
            modelBuilder.Entity<Template>().ToTable("mcnative_templates");

            modelBuilder.Entity<SystemLoaders>().ToTable("system_loader-build-service_loaders");
        }

    }
}
