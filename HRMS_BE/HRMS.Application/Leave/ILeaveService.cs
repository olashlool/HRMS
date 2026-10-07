using HRMS.Application.Leave.Dtos;

namespace HRMS.Application.Leave;

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveTypeResponse>> ListTypesAsync(CancellationToken cancellationToken = default);

    Task<LeaveRequestResponse> SubmitAsync(SubmitLeaveRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestResponse>> ListMineAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestResponse>> ListPendingForApprovalAsync(CancellationToken cancellationToken = default);

    Task<LeaveRequestResponse> DecideAsync(Guid requestId, LeaveDecisionRequest decision, CancellationToken cancellationToken = default);

    Task<LeaveRequestResponse> CancelAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveBalanceResponse>> MyBalancesAsync(CancellationToken cancellationToken = default);
}
