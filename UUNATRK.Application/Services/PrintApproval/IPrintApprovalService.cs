using UUNATEK.Domain.Entities;
using UUNATRK.Application.Models;

namespace UUNATRK.Application.Services.PrintApproval;

public interface IPrintApprovalService
{
    Task<PrintWithApprovalResponse> PrintWithApprovalAsync(PrintApprovalRequest request);
    Task<RequestLog?> GetRequestLogAsync(Guid requestId);
    Task<List<RequestLog>> GetAllLogsByRequestIdAsync(Guid requestId);
    Task<List<RequestLog>> GetRecentRequestsAsync(int count = 10);
}
