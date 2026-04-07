using Microsoft.Extensions.Options;

namespace UUNATRK.Application.Services.Approval;

public class MockApprovalService : IApprovalService
{
    private readonly ApprovalServiceSettings _settings;

    public MockApprovalService(IOptions<ApprovalServiceSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<ApprovalResponse> RequestApprovalAsync(byte[] paperImageBytes, Guid requestId)
    {
        await Task.Delay(500);

        Console.WriteLine($"[MockApprovalService] Processing approval request for RequestId: {requestId}");
        Console.WriteLine($"[MockApprovalService] Image size: {paperImageBytes.Length} bytes");
        
        return new ApprovalResponse(
            IsApproved: true,
            Message: "Mock approval - Default approved",
            RejectionReason: null
        );
    }
}
