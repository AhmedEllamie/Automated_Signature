using UUNATRK.Application.Models;

namespace UUNATRK.Application.Services.Printer;

public interface IPrinterService
{
    bool IsOpen { get; }
    string PortName { get; }
    bool IsPrinting { get; }
    string DefaultComPort { get; }
    int DefaultBaudRate { get; }
    
    void OpenPort(string? comPort = null, int? baudRate = null);
    void ClosePort();
    PrinterStatus GetStatus();
    Task<PrintResponse> Print(List<string> gcode);
    Task<PrintResponse> BulkPrint(List<string> gcode, int copies);
    Task<PrintResponse> VoidPrint();
}
