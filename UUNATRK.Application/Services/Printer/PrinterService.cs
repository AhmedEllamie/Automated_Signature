using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Options;
using UUNATRK.Application.Models;

namespace UUNATRK.Application.Services.Printer
{
    public class PrinterService(IOptions<PrinterSettings> settings) : IPrinterService
    {
        private readonly PrinterSettings _settings = settings.Value;
        private SerialPort? _port;
        private bool _isPrinting;

        public bool IsOpen => _port?.IsOpen ?? false;
        public string PortName => _port?.PortName ?? "N/A";
        public bool IsPrinting => _isPrinting;
        public string DefaultComPort => _settings.ComPort;
        public int DefaultBaudRate => _settings.BaudRate;

        public void OpenPort(string? comPort = null, int? baudRate = null)
        {
            var port = comPort ?? _settings.ComPort;
            var baud = baudRate ?? _settings.BaudRate;

            if (_port?.IsOpen == true)
                _port.Close();

            _port = new SerialPort
            {
                PortName = port,
                BaudRate = baud,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Encoding = Encoding.ASCII,
                NewLine = "\n",
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = 1000,
                WriteTimeout = 2000
            };

            _port.Open();
            Thread.Sleep(1500);
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
        }

        public void ClosePort()
        {
            if (_port?.IsOpen == true)
            {
                _port.Close();
                _port.Dispose();
            }
            _port = null;
        }

        public PrinterStatus GetStatus() => new()
        {
            IsOpen = IsOpen,
            PortName = PortName,
            IsPrinting = IsPrinting
        };

        public async Task<PrintResponse> Print(List<string> gcode)
        {
            if (_isPrinting)
                throw new InvalidOperationException("Printer is busy.");

            _isPrinting = true;
            try
            {
                await Task.Run(() => ExecutePrintCycle(gcode));
                return new PrintResponse { Message = "Print complete.", CommandsSent = gcode.Count };
            }
            finally { _isPrinting = false; }
        }

        public async Task<PrintResponse> BulkPrint(List<string> gcode, int copies)
        {
            if (_isPrinting)
                throw new InvalidOperationException("Printer is busy.");

            _isPrinting = true;
            int totalCommands = 0;
            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < copies; i++)
                    {
                        ExecutePrintCycle(gcode);
                        totalCommands += gcode.Count;
                    }
                });

                return new PrintResponse
                {
                    Message = "Bulk print complete.",
                    Copies = copies,
                    TotalCommandsSent = totalCommands
                };
            }
            finally { _isPrinting = false; }
        }

        public async Task<PrintResponse> VoidPrint()
        {
            if (_isPrinting)
                throw new InvalidOperationException("Printer is busy.");

            _isPrinting = true;
            try
            {
                await Task.Run(() => ExecuteVoidCycle());
                return new PrintResponse { Message = "Void print complete - paper ejected without printing.", CommandsSent = 0 };
            }
            finally { _isPrinting = false; }
        }

        private void ExecutePrintCycle(List<string> gcode)
        {
            try
            {
                Console.WriteLine("=== Starting print cycle ===");

                // -- Handshake: wait for paper insertion --
                Console.WriteLine("Sending M998R handshake...");
                Send("M998R");

                Console.WriteLine("Waiting for 'paper ready'...");
                WaitFor("paper ready", 60);
                Console.WriteLine("Paper ready!");

                // -- Init --
                Console.WriteLine("Sending init commands...");
                Send("G92 X9.0 Y-56.0 Z0");
                Send("G21");
                Send("G90");
                Send("G1 E0.0 F4000");
                Console.WriteLine("Init complete");

                // -- Send G-code --
                Console.WriteLine($"Sending {gcode.Count} G-code commands...");
                int count = 0;
                foreach (var line in gcode)
                {
                    var cmd = line.Trim();
                    if (string.IsNullOrEmpty(cmd) || cmd.StartsWith(";")) continue;
                    Send(cmd);
                    count++;

                    if (count % 50 == 0)
                        Console.WriteLine($"  Sent {count} commands...");
                }
                Console.WriteLine($"Sent all {count} commands");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! ERROR during print: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                Console.WriteLine("=== Starting eject sequence ===");
                EjectPaper();
                Console.WriteLine("=== Print cycle complete ===");
            }
        }

        private void ExecuteVoidCycle()
        {
            try
            {
                Console.WriteLine("=== Starting VOID cycle (no printing) ===");

                Console.WriteLine("Sending M998R handshake...");
                Send("M998R");

                Console.WriteLine("Waiting for 'paper ready'...");
                WaitFor("paper ready", 60);
                Console.WriteLine("Paper ready!");

                Console.WriteLine("Sending init commands (pen stays UP)...");
                Send("G92 X9.0 Y-56.0 Z0");
                Send("G21");
                Send("G90");
                Send("G1 E0.0 F4000");
                Console.WriteLine("Init complete - no printing, pen remains up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! ERROR during void cycle: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                Console.WriteLine("=== Starting eject sequence ===");
                EjectPaper();
                Console.WriteLine("=== Void cycle complete ===");
            }
        }

        private void EjectPaper()
        {
            Console.WriteLine("  Ejecting: Pen up...");
            SendSafe("G1 E0.0 F4000");

            Console.WriteLine("  Ejecting: Move X to 215...");
            SendSafe("G0 X215.0 F6000.0");

            Console.WriteLine("  Ejecting: Start motor (M106)...");
            SendSafe("M106");

            Console.WriteLine("  Ejecting: Push paper Y500...");
            SendSafe("G0 Y500.0 F6000.0");

            Console.WriteLine("  Ejecting: Wait (M400)...");
            SendSafe("M400");

            Console.WriteLine("  Ejecting: Stop motor (M107)...");
            SendSafe("M107");

            Console.WriteLine("Eject complete");
        }


        private void Send(string gcode)
        {
            _port!.WriteLine(gcode);
            WaitForOk();
        }

        private void SendSafe(string gcode)
        {
            try { Send(gcode); }
            catch { /* never stop the eject */ }
        }

        private void WaitForOk(int timeoutSeconds = 10)
        {
            var buffer = new StringBuilder();
            var start = DateTime.Now;

            while (true)
            {
                if ((DateTime.Now - start).TotalSeconds > timeoutSeconds)
                    return;

                try
                {
                    string data = _port!.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        buffer.Append(data);
                        if (buffer.ToString().Contains("ok", StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }
                catch { }

                Thread.Sleep(5);
            }
        }

        private void WaitFor(string expected, int timeoutSeconds)
        {
            var buffer = new StringBuilder();
            var start = DateTime.Now;

            while (true)
            {
                if ((DateTime.Now - start).TotalSeconds > timeoutSeconds)
                    throw new TimeoutException($"Timeout waiting for '{expected}'.");

                try
                {
                    string data = _port!.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        buffer.Append(data);
                        if (buffer.ToString().Contains(expected, StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }
                catch { }

                Thread.Sleep(10);
            }
        }
    }
}
