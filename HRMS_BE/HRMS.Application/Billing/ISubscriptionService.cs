using HRMS.Application.Billing.Dtos;

namespace HRMS.Application.Billing;

public interface ISubscriptionService
{
    Task<IReadOnlyList<PlanResponse>> ListPlansAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task CancelAsync(CancellationToken cancellationToken = default);

    Task<EntitlementResponse> GetEntitlementsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceResponse>> ListInvoicesAsync(CancellationToken cancellationToken = default);
}
