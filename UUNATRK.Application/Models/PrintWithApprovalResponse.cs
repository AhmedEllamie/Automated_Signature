namespace UUNATRK.Application.Models;

public class PrintWithApprovalResponse
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool WasApproved { get; set; }
    public bool WasPrinted { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CommandsSent { get; set; }
}
