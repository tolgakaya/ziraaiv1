using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for PaymentTransaction
    /// Defines table structure, constraints, and relationships
    /// </summary>
    public class PaymentTransactionEntityConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.ToTable("PaymentTransactions");

            builder.HasKey(x => x.Id);

            // User ID (required)
            builder.Property(x => x.UserId)
                .IsRequired();

            // Flow Type (required)
            builder.Property(x => x.FlowType)
                .HasMaxLength(50)
                .IsRequired();

            // Flow Data JSON (required)
            builder.Property(x => x.FlowDataJson)
                .HasColumnType("TEXT")
                .IsRequired();

            // Foreign keys (nullable)
            builder.Property(x => x.SponsorshipPurchaseId)
                .IsRequired(false);

            builder.Property(x => x.UserSubscriptionId)
                .IsRequired(false);

            // iyzico Token (required, unique)
            builder.Property(x => x.IyzicoToken)
                .HasMaxLength(255)
                .IsRequired();

            // iyzico Payment ID (nullable)
            builder.Property(x => x.IyzicoPaymentId)
                .HasMaxLength(255)
                .IsRequired(false);

            // Conversation ID (required, unique)
            builder.Property(x => x.ConversationId)
                .HasMaxLength(100)
                .IsRequired();

            // Amount (required, precision 18,2)
            builder.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            // Currency (required, default TRY)
            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("TRY")
                .IsRequired();

            // Status (required, default Initialized)
            builder.Property(x => x.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Initialized")
                .IsRequired();

            // Timestamps
            builder.Property(x => x.InitializedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(x => x.CompletedAt)
                .IsRequired(false);

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            // Response JSONs (nullable, TEXT type)
            builder.Property(x => x.InitializeResponse)
                .HasColumnType("TEXT")
                .IsRequired(false);

            builder.Property(x => x.VerifyResponse)
                .HasColumnType("TEXT")
                .IsRequired(false);

            // Error Message (nullable, TEXT type)
            builder.Property(x => x.ErrorMessage)
                .HasColumnType("TEXT")
                .IsRequired(false);

            // Audit timestamps
            builder.Property(x => x.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(x => x.UpdatedDate)
                .IsRequired(false);

            // Relationships
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SponsorshipPurchase)
                .WithOne(x => x.PaymentTransaction)
                .HasForeignKey<PaymentTransaction>(x => x.SponsorshipPurchaseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.UserSubscription)
                .WithOne(x => x.PaymentTransaction)
                .HasForeignKey<PaymentTransaction>(x => x.UserSubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_PaymentTransactions_UserId");

            builder.HasIndex(x => x.IyzicoToken)
                .IsUnique()
                .HasDatabaseName("IX_PaymentTransactions_IyzicoToken");

            builder.HasIndex(x => x.ConversationId)
                .IsUnique()
                .HasDatabaseName("IX_PaymentTransactions_ConversationId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_PaymentTransactions_Status");

            builder.HasIndex(x => x.FlowType)
                .HasDatabaseName("IX_PaymentTransactions_FlowType");

            builder.HasIndex(x => x.SponsorshipPurchaseId)
                .HasDatabaseName("IX_PaymentTransactions_SponsorshipPurchaseId");

            builder.HasIndex(x => x.UserSubscriptionId)
                .HasDatabaseName("IX_PaymentTransactions_UserSubscriptionId");

            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_PaymentTransactions_ExpiresAt");
        }
    }
}
