namespace UUNATRK.Application.Models;

public class PrintRetrySettings
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
