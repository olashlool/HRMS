using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed class Invoice : AuditableEntity
{
    public const int NumberMaxLength = 32;

    public Guid TenantId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Number { get; private set; } = null!;
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }

    private Invoice()
    {
    }

    private Invoice(
        Guid tenantId,
        Guid subscriptionId,
        string number,
        long amountMinor,
        string currency,
        DateTimeOffset issuedAtUtc)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        Number = number;
        AmountMinor = amountMinor;
        Currency = currency;
        Status = InvoiceStatus.Issued;
        IssuedAtUtc = issuedAtUtc;
    }

    public static Invoice Issue(
        Guid tenantId,
        Guid subscriptionId,
        string number,
        long amountMinor,
        string currency,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty || subscriptionId == Guid.Empty)
        {
            throw new DomainException("Tenant id and subscription id are required.");
        }

        number = number?.Trim() ?? string.Empty;

        if (number.Length == 0 || number.Length > NumberMaxLength)
        {
            throw new DomainException("Invoice number is required and must not exceed 32 characters.");
        }

        if (amountMinor < 0)
        {
            throw new DomainException("Invoice amount cannot be negative.");
        }

        return new Invoice(tenantId, subscriptionId, number, amountMinor, currency.ToUpperInvariant(), now);
    }

    public void MarkPaid(DateTimeOffset now)
    {
        if (Status == InvoiceStatus.Void)
        {
            throw new DomainException("A void invoice cannot be paid.");
        }

        Status = InvoiceStatus.Paid;
        PaidAtUtc = now;
    }

    public void Void()
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new DomainException("A paid invoice cannot be voided; issue a credit note instead.");
        }

        Status = InvoiceStatus.Void;
    }
}
