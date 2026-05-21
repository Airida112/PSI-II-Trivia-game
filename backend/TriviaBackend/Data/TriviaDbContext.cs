using Microsoft.EntityFrameworkCore;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Data
{
    public class TriviaDbContext(DbContextOptions<TriviaDbContext> options)
        : DbContext(options), ITriviaDbContext
    {
        public DbSet<TriviaQuestion> Questions { get; set; }
        public DbSet<BaseUser> Users { get; set; }
        public DbSet<Clan> Clans { get; set; }
        public DbSet<FriendshipRequest> Friendships { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BaseUser>()
                .HasDiscriminator<string>("user_type")
                .HasValue<Player>("Player")
                .HasValue<Admin>("Admin");

            modelBuilder.Entity<FriendshipRequest>(entity =>
            {
                entity.HasOne(f => f.Requester)
                      .WithMany()
                      .HasForeignKey(f => f.RequesterId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Addressee)
                      .WithMany()
                      .HasForeignKey(f => f.AddresseeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            });

            modelBuilder.Entity<TriviaQuestion>().HasData(
                new TriviaQuestion
                {
                    Id = 100,
                    QuestionText = "What is the most populated city?",
                    AnswerOptions = ["Paris", "Tokyo", "Shanghai", "Gelgaudiškis"],
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Easy,
                    TimeLimit = 20
                },
                new TriviaQuestion
                {
                    Id = 200,
                    QuestionText = "which is least",
                    AnswerOptions = ["pi", "e", "golden ratio", "square root of 2"],
                    CorrectAnswerIndex = 3,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Medium,
                    TimeLimit = 30
                }
            );
        }
    }
}