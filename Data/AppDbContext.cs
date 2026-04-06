using Microsoft.EntityFrameworkCore;
using UniKnowledge.Models;

namespace UniKnowledge.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<QuestionTag> QuestionTags { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.TagName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.HasIndex(e => e.TagName).IsUnique();
        });

        // Question configuration
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId);
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.FileUrl).HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Questions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Questions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Composite indexes for cursor-based pagination
            entity.HasIndex(e => new { e.CreatedAt, e.QuestionId })
                .IsDescending(true, true)
                .HasDatabaseName("IX_Questions_CreatedAt_Id");

            // Index for user's questions (profile page)
            entity.HasIndex(e => new { e.UserId, e.CreatedAt, e.QuestionId })
                .IsDescending(false, true, true)
                .HasDatabaseName("IX_Questions_UserId_CreatedAt_Id");
        });

        // Answer configuration
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.AnswerId);
            entity.Property(e => e.Content).IsRequired();

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Answers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Composite index for cursor-based pagination (answers by question)
            entity.HasIndex(e => new { e.QuestionId, e.IsAccepted, e.CreatedAt, e.AnswerId })
                .IsDescending(false, true, true, true)
                .HasDatabaseName("IX_Answers_QuestionId_Cursor");
        });

        // Vote configuration
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.VoteId);
            entity.Property(e => e.VoteType).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Votes)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Answer)
                .WithMany(a => a.Votes)
                .HasForeignKey(e => e.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure user can only vote once per question or answer
            entity.HasIndex(e => new { e.UserId, e.QuestionId }).IsUnique().HasFilter("[QuestionId] IS NOT NULL");
            entity.HasIndex(e => new { e.UserId, e.AnswerId }).IsUnique().HasFilter("[AnswerId] IS NOT NULL");
        });

        // QuestionTag configuration (Many-to-Many)
        modelBuilder.Entity<QuestionTag>(entity =>
        {
            entity.HasKey(e => new { e.QuestionId, e.TagId });

            entity.HasOne(e => e.Question)
                .WithMany(q => q.QuestionTags)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.QuestionTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.Content).IsRequired();

            entity.HasOne(e => e.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(e => e.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Composite indexes for cursor-based pagination
            // Conversation query: messages between two users, sorted by time
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.CreatedAt, e.MessageId })
                .IsDescending(false, false, true, true)
                .HasDatabaseName("IX_Messages_Sender_Receiver_CreatedAt");

            // Unread messages lookup
            entity.HasIndex(e => new { e.ReceiverId, e.IsRead, e.SenderId })
                .HasDatabaseName("IX_Messages_Unread_Lookup");
        });
        // PasswordResetToken configuration
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OtpCode).HasMaxLength(255).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}

