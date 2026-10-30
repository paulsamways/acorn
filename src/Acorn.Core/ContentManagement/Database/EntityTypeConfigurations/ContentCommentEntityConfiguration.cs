using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class ContentCommentEntityConfiguration : IEntityTypeConfiguration<ContentCommentEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentCommentEntity> builder)
  {
    builder.ToTable("content_comment", table =>
    {
      table.HasCheckConstraint(
        "ck_content_comment_state",
        "comment_state IN ('Unverified', 'Verified', 'Approved', 'Hidden')");
      table.HasCheckConstraint("ck_content_comment_depth", "depth >= 0");
    });

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("content_comment_id")
      .ValueGeneratedNever()
      .IsRequired();

    builder
      .Property(x => x.ContentId)
      .HasColumnName("content_id")
      .IsRequired();

    builder
      .Property(x => x.ParentCommentId)
      .HasColumnName("parent_comment_id");

    builder
      .Property(x => x.Depth)
      .HasColumnName("depth")
      .IsRequired();

    builder
      .Property(x => x.AuthorName)
      .HasColumnName("author_name")
      .HasMaxLength(ContentCommentLimits.AuthorNameMaxLength)
      .IsRequired();

    builder
      .Property(x => x.AuthorEmail)
      .HasColumnName("author_email")
      .HasMaxLength(ContentCommentLimits.AuthorEmailMaxLength)
      .IsRequired();

    builder
      .Property(x => x.Body)
      .HasColumnName("body")
      .HasMaxLength(ContentCommentLimits.BodyMaxLength)
      .IsRequired();

    builder
      .Property(x => x.State)
      .HasColumnName("comment_state")
      .HasConversion<string>()
      .IsRequired();

    builder
      .Property(x => x.VerificationTokenHash)
      .HasColumnName("verification_token_hash");

    builder
      .Property(x => x.VerificationTokenExpiresAt)
      .HasColumnName("verification_token_expires_at");

    builder
      .Property(x => x.VerificationSentAt)
      .HasColumnName("verification_sent_at");

    builder
      .Property(x => x.VerifiedAt)
      .HasColumnName("verified_at");

    builder
      .Property(x => x.ApprovedAt)
      .HasColumnName("approved_at");

    builder
      .Property(x => x.HiddenAt)
      .HasColumnName("hidden_at");

    builder
      .Property(x => x.ConcurrencyToken)
      .HasColumnName("concurrency_token")
      .IsConcurrencyToken()
      .IsRequired();

    builder
      .Property(x => x.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder
      .Property(x => x.UpdatedAt)
      .HasColumnName("updated_at")
      .IsRequired();

    // Supports the composite parent foreign key so a reply cannot reference a comment on other content.
    builder
      .HasAlternateKey(x => new { x.ContentId, x.Id });

    builder
      .HasIndex(x => new { x.ContentId, x.State, x.CreatedAt });

    builder
      .HasIndex(x => new { x.State, x.CreatedAt });

    builder
      .HasIndex(x => x.VerificationTokenHash)
      .IsUnique()
      .HasFilter("\"verification_token_hash\" IS NOT NULL");

    builder
      .HasOne(x => x.Content)
      .WithMany()
      .HasForeignKey(x => x.ContentId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasOne(x => x.Parent)
      .WithMany(x => x.Replies)
      .HasForeignKey(x => new { x.ContentId, x.ParentCommentId })
      .HasPrincipalKey(x => new { x.ContentId, x.Id })
      .OnDelete(DeleteBehavior.Restrict);

    builder
      .HasQueryFilter(x => x.Content.DeletedAt == null);
  }
}
