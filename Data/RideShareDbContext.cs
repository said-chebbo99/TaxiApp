using Microsoft.EntityFrameworkCore;
using ProjetInfo.Models;
using System.Reflection.Emit;

namespace ProjetInfo.Data
{
	public class RideShareDbContext : DbContext
	{
		public RideShareDbContext(DbContextOptions<RideShareDbContext> options)
			: base(options)
		{
		}

		// DbSets for all entities
		public DbSet<User> Users { get; set; }
		public DbSet<Driver> Drivers { get; set; }
		public DbSet<Vehicle> Vehicles { get; set; }
		public DbSet<UserPreferences> UserPreferences { get; set; }
		public DbSet<Ride> Rides { get; set; }
		public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
		public DbSet<RideFeedback> RideFeedbacks { get; set; }
		public DbSet<Contact> Contacts { get; set; }
		public DbSet<ChatMessage> ChatMessages { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// User configuration
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(u => u.ID);
				entity.HasIndex(u => u.Email).IsUnique();

				entity.HasMany(u => u.ChatMessages)
					.WithOne(cm => cm.Sender)
					.HasForeignKey(cm => cm.SenderID)
					.OnDelete(DeleteBehavior.ClientNoAction);
			});

			// Vehicle configuration
			modelBuilder.Entity<Vehicle>(entity =>
			{
				entity.HasKey(v => v.VehicleID);
				entity.HasIndex(v => v.PlateNumber).IsUnique();

				entity.HasOne(v => v.Driver)
					.WithMany(d => d.Vehicles)
					.HasForeignKey(v => v.DriverID)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// UserPreferences configuration
			modelBuilder.Entity<UserPreferences>(entity =>
			{
				entity.HasKey(up => up.PreferenceID);

				entity.HasOne(up => up.User)
					.WithOne(u => u.UserPreferences)
					.HasForeignKey<UserPreferences>(up => up.UserID)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// Ride configuration
			modelBuilder.Entity<Ride>(entity =>
			{
				entity.HasKey(r => r.RideID);

				entity.HasMany(r => r.ChatMessages)
					.WithOne(cm => cm.Ride)
					.HasForeignKey(cm => cm.RideID)
					.OnDelete(DeleteBehavior.ClientCascade);

				entity.HasOne(r => r.User)
					.WithMany(u => u.Rides)
					.HasForeignKey(r => r.UserID)
					.OnDelete(DeleteBehavior.NoAction);

				entity.HasOne(r => r.Driver)
					.WithMany(d => d.Rides)
					.HasForeignKey(r => r.DriverID)
					.OnDelete(DeleteBehavior.NoAction);

				entity.HasOne(r => r.PaymentTransaction)
					.WithMany(pt => pt.Rides)
					.HasForeignKey(r => r.PaymentTransactionID)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// PaymentTransaction configuration
			modelBuilder.Entity<PaymentTransaction>(entity =>
			{
				entity.HasKey(pt => pt.TransactionID);

				entity.HasOne(pt => pt.User)
					.WithMany(u => u.PaymentTransactions)
					.HasForeignKey(pt => pt.UserID)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// RideFeedback configuration
			modelBuilder.Entity<RideFeedback>(entity =>
			{
				entity.HasKey(rf => rf.FeedbackID);

				entity.HasOne(rf => rf.Ride)
					.WithMany(r => r.RideFeedbacks)
					.HasForeignKey(rf => rf.RideID)
					.OnDelete(DeleteBehavior.NoAction);

				entity.HasOne(rf => rf.User)
					.WithMany(u => u.RideFeedbacks)
					.HasForeignKey(rf => rf.UserID)
					.OnDelete(DeleteBehavior.NoAction);

				entity.HasOne(rf => rf.Driver)
					.WithMany(d => d.DriverFeedbacks)
					.HasForeignKey(rf => rf.DriverID)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// Contact configuration (updated to match your model)
			modelBuilder.Entity<Contact>(entity =>
			{
				entity.HasKey(c => c.Id); // Matches your Contact model's Id property

				entity.Property(c => c.Name).IsRequired();
				entity.Property(c => c.Email).IsRequired();
				entity.Property(c => c.Message).IsRequired();
				entity.Property(c => c.PhoneNumber).IsRequired();
				entity.Property(c => c.SubmittedAt)
					.HasDefaultValueSql("GETDATE()");
			});

			// ChatMessage configuration
			modelBuilder.Entity<ChatMessage>(entity =>
			{
				entity.HasKey(cm => cm.Id);

				entity.Property(cm => cm.Timestamp)
					.HasDefaultValueSql("SYSDATETIME()")
					.ValueGeneratedOnAdd();

				entity.HasIndex(cm => new { cm.RideID, cm.Timestamp });
				entity.HasIndex(cm => cm.SenderID);

				entity.HasOne(cm => cm.Ride)
					.WithMany(r => r.ChatMessages)
					.HasForeignKey(cm => cm.RideID)
					.OnDelete(DeleteBehavior.ClientCascade);

				entity.HasOne(cm => cm.Sender)
					.WithMany(u => u.ChatMessages)
					.HasForeignKey(cm => cm.SenderID)
					.OnDelete(DeleteBehavior.ClientNoAction);
			});
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlServer(
					"YourConnectionStringHere",
					options => options.EnableRetryOnFailure(
						maxRetryCount: 5,
						maxRetryDelay: TimeSpan.FromSeconds(30),
						errorNumbersToAdd: null));
			}
		}
	}
}