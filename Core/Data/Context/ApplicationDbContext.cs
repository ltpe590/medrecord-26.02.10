using Core.Data.Configurations;
using Core.Data.Seeding;
using Core.Entities;
using Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Core.Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ===== Clinical Core =====
        public DbSet<Patient> Patients { get; set; }

        public DbSet<Visit> Visits { get; set; }

        // ===== Clinical Modules =====
        public DbSet<TestsCatalog> TestCatalogs { get; set; }

        public DbSet<DrugCatalog> DrugCatalogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;Database=MedRecordDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                    b => b.MigrationsAssembly("Core"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SpecialtyProfile>().HasData(new SpecialtyProfile { SpecialtyProfileId = 1, Name = "General Medicine" });
            modelBuilder.Entity<VisitEntry>().Property(x => x.SystemCode).HasMaxLength(20);
            modelBuilder.ApplyConfiguration(new SpecialtyProfileConfiguration());
            modelBuilder.ApplyConfiguration(new PatientConfiguration());
            modelBuilder.ApplyConfiguration(new VisitConfiguration());
            modelBuilder.ApplyConfiguration(new TestsCatalogConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new DrugCatalogConfiguration());

            TestCatalogSeeder.SeedTestCatalogs(modelBuilder);
        }
    }
}