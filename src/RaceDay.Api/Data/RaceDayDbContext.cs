using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<Organiser> Organisers => Set<Organiser>();
        public DbSet<Participant> Participants => Set<Participant>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Organiser>()
                .HasIndex(o => o.Email)
                .IsUnique();

            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Composite uniqueness: An athlete can enrol in a specific category only once
            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new { e.ParticipantID, e.CategoryID })
                .IsUnique();

            // Enforce 1:1 between Enrolment and Result
            modelBuilder.Entity<Result>()
                .HasIndex(r => r.EnrolmentID)
                .IsUnique();
        }
    }
}