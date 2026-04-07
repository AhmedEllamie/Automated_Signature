namespace UUNATRK.Application.Models;

public class PrintApprovalRequest
{
    public Stream? PaperImageStream { get; set; }
    public string? PaperImageFileName { get; set; }
    
    public Stream SignatureSvgStream { get; set; } = null!;
    public string SignatureSvgFileName { get; set; } = string.Empty;
    
    public PrintRequest PrintSettings { get; set; } = new();
    
    public bool ShouldApprove { get; set; } = true;
}
