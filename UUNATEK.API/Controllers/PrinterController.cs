using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UUNATRK.Application.Enums;
using UUNATRK.Application.Models;
using UUNATRK.Application.Services.Printer;
using UUNATRK.Application.Services.PrintApproval;

namespace UUNATEK.API.Controllers;

[ApiController]
[Route("printer")]
public class PrinterController : ControllerBase
{
    private readonly PrinterService _printer;
    private readonly IPrintApprovalService _printApprovalService;

    public PrinterController(
        PrinterService printer,
        IPrintApprovalService printApprovalService)
    {
        _printer = printer;
        _printApprovalService = printApprovalService;
    }


    [HttpPost("connect")]
    public ActionResult Connect(
        [FromQuery] string? comPort = null,
        [FromQuery] int? baudRate = null)
    {
        if (_printer.IsOpen)
            return Conflict(new { Message = $"Already connected to {_printer.PortName}. Disconnect first." });

        var port = comPort ?? _printer.DefaultComPort;
        var baud = baudRate ?? _printer.DefaultBaudRate;

        try
        {
            _printer.OpenPort(port, baud);
            return Ok(new { Message = $"Connected to {port} at {baud} baud." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Failed to connect: {ex.Message}" });
        }
    }

    [HttpPost("disconnect")]
    public ActionResult Disconnect()
    {
        if (!_printer.IsOpen)
            return Conflict(new { Message = "Not connected." });

        if (_printer.IsPrinting)
            return Conflict(new { Message = "Cannot disconnect while printing." });

        _printer.ClosePort();
        return Ok(new { Message = "Disconnected." });
    }



    [HttpGet("status")]
    public ActionResult<PrinterStatus> GetStatus()
    {
        return Ok(_printer.GetStatus());
    }



    [HttpPost("generate")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Generate(
        IFormFile svg,
        [FromForm] PrintRequest req)
    {
        var (gcode, error) = await ConvertSvg(svg, req);

        if (error != null)
            return error;

        return Ok(new
        {
            Message = $"Generated {gcode!.Count} G-code commands.",
            CommandCount = gcode.Count,
            GCode = gcode
        });
    }



    [HttpPost("print")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PrintResponse>> Print(
        IFormFile svg,
        [FromForm] PrintRequest req)
    {
        if (!_printer.IsOpen)
            return BadRequest(new { Message = "Printer is not connected. Call POST /printer/connect first." });

        if (_printer.IsPrinting)
            return Conflict(new { Message = "Printer is busy." });

        var (gcode, error) = await ConvertSvg(svg, req);

        if (error != null)
            return error;

        var result = await _printer.Print(gcode!);
        return Ok(result);
    }

    [HttpPost("print/bulk")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PrintResponse>> BulkPrint(
        IFormFile svg,
        [FromForm] int copies,
        [FromForm] PrintRequest req)
    {
        if (!_printer.IsOpen)
            return BadRequest(new { Message = "Printer is not connected. Call POST /printer/connect first." });

        if (_printer.IsPrinting)
            return Conflict(new { Message = "Printer is busy." });

        if (copies < 1 || copies > 100)
            return BadRequest(new { Message = "Copies must be between 1 and 100." });

        var (gcode, error) = await ConvertSvg(svg, req);

        if (error != null)
            return error;

        var result = await _printer.BulkPrint(gcode!, copies);
        return Ok(result);
    }

    [HttpPost("print-with-approval")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PrintWithApprovalResponse>> PrintWithApproval(
        IFormFile? paperImage,
        string? paperImageBase64,
        IFormFile signatureSvg,
        [FromForm] string printRequestJson)
    {
        try
        {
            if (!_printer.IsOpen)
                return BadRequest(new { Message = "Printer is not connected. Call POST /printer/connect first." });

            if (_printer.IsPrinting)
                return Conflict(new { Message = "Printer is busy." });

            var printWithApprovalReq = JsonSerializer.Deserialize<PrintWithApprovalRequest>(printRequestJson);
            if (printWithApprovalReq == null)
                return BadRequest(new { Message = "Invalid printRequest JSON." });

            if (signatureSvg == null || signatureSvg.Length == 0)
                return BadRequest(new { Message = "No signature SVG file provided." });

            Stream? paperImageStream = null;
            if (paperImage != null && paperImage.Length > 0)
            {
                paperImageStream = paperImage.OpenReadStream();
            }
            else if (!string.IsNullOrEmpty(paperImageBase64))
            {
                var base64Data = paperImageBase64.Contains(",") ? paperImageBase64.Split(',')[1] : paperImageBase64;
                var bytes = Convert.FromBase64String(base64Data);
                paperImageStream = new MemoryStream(bytes);
            }

            using (paperImageStream)
            using (var svgStream = signatureSvg.OpenReadStream())
            {
                var request = new PrintApprovalRequest
                {
                    PaperImageStream = paperImageStream,
                    PaperImageFileName = paperImage?.FileName,
                    SignatureSvgStream = svgStream,
                    SignatureSvgFileName = signatureSvg.FileName,
                    PrintSettings = printWithApprovalReq.PrintSettings,
                    ShouldApprove = printWithApprovalReq.ShouldApprove
                };

                var response = await _printApprovalService.PrintWithApprovalAsync(request);
                return Ok(response);
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error: {ex.Message}" });
        }
    }

    [HttpGet("requests/{requestId}")]
    public async Task<ActionResult> GetRequestLog(Guid requestId)
    {
        var log = await _printApprovalService.GetRequestLogAsync(requestId);
        if (log == null)
            return NotFound(new { Message = $"Request log with ID {requestId} not found." });

        return Ok(log);
    }


    private async Task<(List<string>? gcode, ActionResult? error)> ConvertSvg(
        IFormFile svg, PrintRequest req)
    {
        if (svg == null || svg.Length == 0)
            return (null, BadRequest(new { Message = "No SVG file provided." }));

        if (req.Scale < 1)
            return (null, BadRequest(new { Message = "Scale must be at least 1." }));

        if (req.Rotation < 0 || req.Rotation > 360)
            return (null, BadRequest(new { Message = "Rotation must be between 0 and 360." }));

        if (req.Paper.HasValue)
        {
            var (pw, ph) = PaperSizes.GetSizeMm(req.Paper.Value);
            req.Width = $"{pw}mm";
            req.Height = $"{ph}mm";
        }

        using var stream = new MemoryStream();
        await svg.CopyToAsync(stream);
        stream.Position = 0;

        var gcode = SvgConverter.ConvertToGCode(stream, req);

        if (gcode.Count == 0)
            return (null, BadRequest(new { Message = "No drawable paths found. If SVG contains text, convert to path first (Inkscape: Path > Object to Path)." }));

        return (gcode, null);
    }
}
