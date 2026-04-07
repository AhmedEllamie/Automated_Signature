namespace UUNATRK.Application.Services.Approval;

public class ApprovalServiceSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool UseMockService { get; set; } = true;
}
