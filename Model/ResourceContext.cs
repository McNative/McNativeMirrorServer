using Microsoft.EntityFrameworkCore;

namespace McNativeMirrorServer.Model
{
    public class ResourceContext : DbContext {

        public ResourceContext(DbContextOptions<ResourceContext> options) : base(options){}

        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceVersion> ResourceVersions { get; set; }

        public DbSet<License> Licenses { get; set; }

        public DbSet<Server> Servers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("mcnative");
            modelBuilder.Entity<Server>().ToTable("mcnative_server");
            modelBuilder.Entity<Resource>().ToTable("mcnative_resource");
            modelBuilder.Entity<ResourceVersion>().ToTable("mcnative_resource_versions");
            modelBuilder.Entity<ResourceEdition>().ToTable("mcnative_resource_editions");
            modelBuilder.Entity<License>().ToTable("mcnative_license");
        }

    }
}
