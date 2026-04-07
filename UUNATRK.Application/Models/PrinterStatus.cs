namespace UUNATRK.Application.Models
{
    public class PrinterStatus
    {
        public bool IsOpen { get; set; }
        public string PortName { get; set; } = "N/A";
        public bool IsPrinting { get; set; }
    }
}
