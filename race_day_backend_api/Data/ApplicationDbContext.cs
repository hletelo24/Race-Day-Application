using Microsoft.EntityFrameworkCore;
using race_day_backend_api.Models;

namespace race_day_backend_api.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }

        public DbSet<Participant> Participants { get; set; }

        public DbSet<Organiser> Organisers { get; set; }

        public DbSet<Enrolment> Enrolments { get; set; }

        public DbSet<Result> Results { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Models.Route> Routes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // ==========================================
            // UserAccount -> Participant
            // One-to-One
            // ==========================================

            modelBuilder.Entity<UserAccount>()
                .HasOne(u => u.Participant)
                .WithOne(p => p.UserAccount)
                .HasForeignKey<Participant>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // UserAccount -> Organiser
            // One-to-One
            // ==========================================

            modelBuilder.Entity<UserAccount>()
                .HasOne(u => u.Organiser)
                .WithOne(o => o.UserAccount)
                .HasForeignKey<Organiser>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // Participant -> Enrolment
            // One-to-Many
            // ==========================================

            modelBuilder.Entity<Participant>()
                .HasMany(p => p.Enrolments)
                .WithOne(e => e.Participant)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // Organiser -> Event
            // One-to-Many
            // ==========================================

            modelBuilder.Entity<Organiser>()
                .HasMany(o => o.Events)
                .WithOne(e => e.Organiser)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Event -> Enrolment
            // One-to-Many
            // ==========================================

            modelBuilder.Entity<Event>()
                .HasMany(e => e.Enrolments)
                .WithOne(en => en.Event)
                .HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // Category -> Event
            // One-to-Many
            // ==========================================

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Events)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Route -> Event
            // One-to-Many
            // ==========================================

            modelBuilder.Entity<Models.Route>()
                .HasMany(r => r.Events)
                .WithOne(e => e.Route)
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Enrolment -> Result
            // One-to-One
            // ==========================================

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Result)
                .WithOne(r => r.Enrolment)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // Unique Enrolment
            //
            // Prevent same participant enrolling
            // in same event multiple times
            // ==========================================

            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new
                {
                    e.EventId,
                    e.UserId
                })
                .IsUnique();


            // ==========================================
            // Email should be unique
            // ==========================================

            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.EmailAddress)
                .IsUnique();


            // ==========================================
            // Decimal precision
            // ==========================================

            modelBuilder.Entity<Event>()
                .Property(e => e.Distance)
                .HasPrecision(10, 2);

            modelBuilder.Entity<race_day_backend_api.Models.Route>()
                .Property(r => r.Distance)
                .HasPrecision(10, 2);
        }
    }
}
