using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsManager.Domain.Entities;

namespace RequestsManager.Infrastructure.Persistence.Configurations;

public sealed class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> b)
    {
        b.ToTable("RequestStatusHistory");
        b.HasKey(h => h.Id);
        b.Property(h => h.PreviousStatus).HasConversion<byte>();
        b.Property(h => h.NewStatus).HasConversion<byte>();
        b.Property(h => h.ChangedAt).HasColumnType("datetime2(3)");
        b.Property(h => h.ChangedBy).HasMaxLength(100).IsRequired();

        // History screen: WHERE RequestId = @id ORDER BY ChangedAt DESC.
        b.HasIndex(h => new { h.RequestId, h.ChangedAt }).IsDescending(false, true);
    }
}
