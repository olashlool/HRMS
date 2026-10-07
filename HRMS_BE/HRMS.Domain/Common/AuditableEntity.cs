namespace HRMS.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditable, ISoftDeletable
{
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? ModifiedAtUtc { get; private set; }
    public string? ModifiedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    DateTimeOffset IAuditable.CreatedAtUtc { get => CreatedAtUtc; set => CreatedAtUtc = value; }
    string? IAuditable.CreatedBy { get => CreatedBy; set => CreatedBy = value; }
    DateTimeOffset? IAuditable.ModifiedAtUtc { get => ModifiedAtUtc; set => ModifiedAtUtc = value; }
    string? IAuditable.ModifiedBy { get => ModifiedBy; set => ModifiedBy = value; }
    bool ISoftDeletable.IsDeleted { get => IsDeleted; set => IsDeleted = value; }
    DateTimeOffset? ISoftDeletable.DeletedAtUtc { get => DeletedAtUtc; set => DeletedAtUtc = value; }
    string? ISoftDeletable.DeletedBy { get => DeletedBy; set => DeletedBy = value; }
}
