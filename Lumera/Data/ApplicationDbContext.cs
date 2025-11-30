using Microsoft.EntityFrameworkCore;
using Lumera.Models;

namespace Lumera.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // User Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Organizer> Organizers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        // Event Tables
        public DbSet<Event> Events { get; set; }

        // Service Tables
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceGallery> ServiceGallery { get; set; }

        // Booking Tables
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingMessage> BookingMessages { get; set; }

        // Review Tables
        public DbSet<Review> Reviews { get; set; }

        // Transaction Tables
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Payout> Payouts { get; set; }

        // Conversation Tables
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MySQL specific configuration
            modelBuilder.HasCharSet("utf8mb4");

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired();
            });

            // Client Configuration
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.ClientID);
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Client)
                    .HasForeignKey<Client>(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Organizer Configuration
            modelBuilder.Entity<Organizer>(entity =>
            {
                entity.HasKey(e => e.OrganizerID);
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Organizer)
                    .HasForeignKey<Organizer>(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.AverageRating).HasPrecision(3, 2);
            });

            // Supplier Configuration
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.SupplierID);
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Supplier)
                    .HasForeignKey<Supplier>(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.AverageRating).HasPrecision(3, 2);
            });

            // Event Configuration
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventID);
                entity.HasOne(e => e.Client)
                    .WithMany(c => c.Events)
                    .HasForeignKey(e => e.ClientID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Organizer)
                    .WithMany(o => o.Events)
                    .HasForeignKey(e => e.OrganizerID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Budget).HasPrecision(10, 2);
            });

            // Service Configuration
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.ServiceID);
                entity.Property(e => e.BasePrice).HasPrecision(10, 2);
                entity.Property(e => e.AverageRating).HasPrecision(3, 2);
            });

            // ServiceGallery Configuration
            modelBuilder.Entity<ServiceGallery>(entity =>
            {
                entity.HasKey(e => e.GalleryID);
                entity.ToTable("ServiceGallery");
                entity.HasOne(e => e.Service)
                    .WithMany(s => s.Gallery)
                    .HasForeignKey(e => e.ServiceID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Booking Configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingID);
                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.Bookings)
                    .HasForeignKey(e => e.EventID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Service)
                    .WithMany(s => s.Bookings)
                    .HasForeignKey(e => e.ServiceID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Client)
                    .WithMany(c => c.Bookings)
                    .HasForeignKey(e => e.ClientID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.QuoteAmount).HasPrecision(10, 2);
                entity.Property(e => e.FinalAmount).HasPrecision(10, 2);
            });

            // BookingMessage Configuration
            modelBuilder.Entity<BookingMessage>(entity =>
            {
                entity.HasKey(e => e.MessageID);
                entity.HasOne(e => e.Booking)
                    .WithMany(b => b.Messages)
                    .HasForeignKey(e => e.BookingID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Review Configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.ReviewID);
                entity.HasOne(e => e.Booking)
                    .WithMany()
                    .HasForeignKey(e => e.BookingID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Reviewer)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewerID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Conversation Configuration
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.ConversationID);
                entity.HasOne(e => e.Event)
                    .WithMany()
                    .HasForeignKey(e => e.EventID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Client)
                    .WithMany()
                    .HasForeignKey(e => e.ClientID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Organizer)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizerID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ConversationParticipant Configuration
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasKey(e => e.ParticipantID);
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(e => e.ConversationID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Message Configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageID);
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Transaction Configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionID);
                entity.HasOne(e => e.Booking)
                    .WithMany()
                    .HasForeignKey(e => e.BookingID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Payer)
                    .WithMany()
                    .HasForeignKey(e => e.PayerID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Payee)
                    .WithMany()
                    .HasForeignKey(e => e.PayeeID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Amount).HasPrecision(10, 2);
            });

            // Payout Configuration
            modelBuilder.Entity<Payout>(entity =>
            {
                entity.HasKey(e => e.PayoutID);
                entity.HasOne(e => e.Payee)
                    .WithMany()
                    .HasForeignKey(e => e.PayeeID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Amount).HasPrecision(10, 2);
            });
        }
    }
}