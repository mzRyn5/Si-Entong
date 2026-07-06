using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Configurations;

public class AiChatSessionConfiguration : IEntityTypeConfiguration<AiChatSession>
{
    public void Configure(EntityTypeBuilder<AiChatSession> builder)
    {
        builder.ToTable("ai_chat_sessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.LastActiveAt).HasColumnName("last_active_at").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Note: StoreId is mapped globally in AppDbContext.cs for ITenantEntity
    }
}

public class AiChatMessageConfiguration : IEntityTypeConfiguration<AiChatMessage>
{
    public void Configure(EntityTypeBuilder<AiChatMessage> builder)
    {
        builder.ToTable("ai_chat_messages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").IsRequired();
        builder.Property(x => x.Intent).HasColumnName("intent").HasMaxLength(100);
        builder.Property(x => x.ToolCalls).HasColumnName("tool_calls");
        builder.Property(x => x.ToolResults).HasColumnName("tool_results");
        builder.Property(x => x.FunctionName).HasColumnName("function_name").HasMaxLength(100);
        builder.Property(x => x.TokenCount).HasColumnName("token_count").HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.CreatedAt })
            .HasDatabaseName("idx_chat_messages_session");
    }
}

public class AiActionDraftConfiguration : IEntityTypeConfiguration<AiActionDraft>
{
    public void Configure(EntityTypeBuilder<AiActionDraft> builder)
    {
        builder.ToTable("ai_action_drafts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ActionName).HasColumnName("action_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.DraftPayload).HasColumnName("draft_payload").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.ExpiredAt).HasColumnName("expired_at").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Session)
            .WithMany(s => s.ActionDrafts)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SessionId, x.Status })
            .HasDatabaseName("idx_action_drafts_session_status");

        builder.HasIndex(x => x.ExpiredAt)
            .HasDatabaseName("idx_action_drafts_expiry")
            .HasFilter("status = 'pending'");
    }
}

public class AiActionLogConfiguration : IEntityTypeConfiguration<AiActionLog>
{
    public void Configure(EntityTypeBuilder<AiActionLog> builder)
    {
        builder.ToTable("ai_action_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ActionName).HasColumnName("action_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.RequestPayload).HasColumnName("request_payload").IsRequired();
        builder.Property(x => x.ResponsePayload).HasColumnName("response_payload").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
