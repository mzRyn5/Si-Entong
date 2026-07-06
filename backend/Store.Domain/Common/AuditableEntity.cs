namespace Store.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected AuditableEntity() : base()
    {
    }
}
