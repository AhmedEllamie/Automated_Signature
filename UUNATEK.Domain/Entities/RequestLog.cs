using UUNATEK.Domain.Enums;

namespace UUNATEK.Domain.Entities;

public class RequestLog
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public RequestStatus Status { get; set; }
    public string? ApprovalResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
