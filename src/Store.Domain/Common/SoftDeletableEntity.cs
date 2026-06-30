namespace Store.Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    protected SoftDeletableEntity() : base()
    {
        IsDeleted = false;
    }
}
