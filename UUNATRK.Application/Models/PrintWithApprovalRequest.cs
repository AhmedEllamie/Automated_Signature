namespace UUNATRK.Application.Models;

public class PrintWithApprovalRequest
{
    public PrintRequest PrintSettings { get; set; } = new();
    public bool ShouldApprove { get; set; } = true;
}
