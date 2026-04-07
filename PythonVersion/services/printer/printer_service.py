from __future__ import annotations

import asyncio
import threading
import time
from typing import Any

from PythonVersion.models.contracts import PrintResponse, PrinterStatus
from PythonVersion.models.printer_settings import PrinterSettings
from PythonVersion.services.printer.i_printer_service import IPrinterService

try:
    import serial
except ImportError:  # pragma: no cover - depends on environment
    serial = None


class PrinterService(IPrinterService):
    def __init__(self, settings: PrinterSettings):
        self._settings = settings
        self._port: Any | None = None
        self._is_printing = False
        self._print_lock = threading.Lock()

    @property
    def is_open(self) -> bool:
        return bool(self._port and self._port.is_open)

    @property
    def port_name(self) -> str:
        if self._port and self._port.is_open:
            return str(self._port.port)
        return "N/A"

    @property
    def is_printing(self) -> bool:
        return self._is_printing

    @property
    def default_com_port(self) -> str:
        return self._settings.com_port

    @property
    def default_baud_rate(self) -> int:
        return self._settings.baud_rate

    def open_port(self, com_port: str | None = None, baud_rate: int | None = None) -> None:
        if serial is None:
            raise RuntimeError("pyserial is not installed. Install requirements first.")

        port_name = com_port or self._settings.com_port
        baud = baud_rate or self._settings.baud_rate

        if self._port and self._port.is_open:
            self._port.close()

        self._port = serial.Serial(
            port=port_name,
            baudrate=baud,
            parity=serial.PARITY_NONE,
            bytesize=serial.EIGHTBITS,
            stopbits=serial.STOPBITS_ONE,
            timeout=0,
            write_timeout=2.0,
        )

        # Match the C# serial settings and startup delay.
        self._port.dtr = True
        self._port.rts = True
        time.sleep(1.5)
        self._port.reset_input_buffer()
        self._port.reset_output_buffer()

    def close_port(self) -> None:
        if self._port and self._port.is_open:
            self._port.close()
        self._port = None

    def get_status(self) -> PrinterStatus:
        return PrinterStatus(
            is_open=self.is_open,
            port_name=self.port_name,
            is_printing=self.is_printing,
        )

    async def print(self, gcode: list[str]) -> PrintResponse:
        self._begin_print_job()
        try:
            await asyncio.to_thread(self._execute_print_cycle, gcode)
            return PrintResponse(message="Print complete.", commands_sent=len(gcode))
        finally:
            self._end_print_job()

    async def bulk_print(self, gcode: list[str], copies: int) -> PrintResponse:
        self._begin_print_job()
        total_commands = 0
        try:
            def run() -> None:
                nonlocal total_commands
                for _ in range(copies):
                    self._execute_print_cycle(gcode)
                    total_commands += len(gcode)

            await asyncio.to_thread(run)
            return PrintResponse(
                message="Bulk print complete.",
                copies=copies,
                total_commands_sent=total_commands,
            )
        finally:
            self._end_print_job()

    async def void_print(self) -> PrintResponse:
        self._begin_print_job()
        try:
            await asyncio.to_thread(self._execute_void_cycle)
            return PrintResponse(
                message="Void print complete - paper ejected without printing.",
                commands_sent=0,
            )
        finally:
            self._end_print_job()

    async def pen_change_start(self) -> PrintResponse:
        self._begin_print_job()
        try:
            await asyncio.to_thread(self._execute_pen_change_start)
            return PrintResponse(
                message="Pen change start complete. Replace pen, then run pen-change-finish.",
                commands_sent=2,
            )
        finally:
            self._end_print_job()

    async def pen_change_finish(self) -> PrintResponse:
        self._begin_print_job()
        try:
            await asyncio.to_thread(self._execute_pen_change_finish)
            return PrintResponse(
                message="Pen change finish complete. Printer is ready to continue.",
                commands_sent=2,
            )
        finally:
            self._end_print_job()

    def _begin_print_job(self) -> None:
        with self._print_lock:
            if self._is_printing:
                raise RuntimeError("Printer is busy.")
            self._is_printing = True

    def _end_print_job(self) -> None:
        with self._print_lock:
            self._is_printing = False

    def _execute_print_cycle(self, gcode: list[str]) -> None:
        try:
            print("=== Starting print cycle ===")
            print("Sending M998R handshake...")
            self._send("M998R")

            print("Waiting for 'paper ready'...")
            self._wait_for("paper ready", timeout_seconds=60)
            print("Paper ready!")

            print("Sending init commands...")
            self._send("G92 X9.0 Y-56.0 Z0")
            self._send("G21")
            self._send("G90")
            self._send("G1 E0.0 F4000")
            print("Init complete")

            print(f"Sending {len(gcode)} G-code commands...")
            sent_count = 0
            for line in gcode:
                cmd = line.strip()
                if not cmd or cmd.startswith(";"):
                    continue
                self._send(cmd)
                sent_count += 1
                if sent_count % 50 == 0:
                    print(f"  Sent {sent_count} commands...")
            print(f"Sent all {sent_count} commands")
        except Exception as ex:
            print(f"!!! ERROR during print: {ex}")
            raise
        finally:
            print("=== Starting eject sequence ===")
            self._eject_paper()
            print("=== Print cycle complete ===")

    def _execute_void_cycle(self) -> None:
        try:
            print("=== Starting VOID cycle (no printing) ===")
            print("Sending M998R handshake...")
            self._send("M998R")

            print("Waiting for 'paper ready'...")
            self._wait_for("paper ready", timeout_seconds=60)
            print("Paper ready!")

            print("Sending init commands (pen stays UP)...")
            self._send("G92 X9.0 Y-56.0 Z0")
            self._send("G21")
            self._send("G90")
            self._send("G1 E0.0 F4000")
            print("Init complete - no printing, pen remains up")
        except Exception as ex:
            print(f"!!! ERROR during void cycle: {ex}")
            raise
        finally:
            print("=== Starting eject sequence ===")
            self._eject_paper()
            print("=== Void cycle complete ===")

    def _execute_pen_change_start(self) -> None:
        print("=== Starting pen-change-start ===")
        self._send("G90")
        self._send("G1 E7.5 F5000")
        print("Pen moved to change position (E7.5)")
        print("=== Pen-change-start complete ===")

    def _execute_pen_change_finish(self) -> None:
        print("=== Starting pen-change-finish ===")
        self._send("G90")
        self._send("G1 E0.0 F5000")
        print("Pen moved to ready/up position (E0.0)")
        print("=== Pen-change-finish complete ===")

    def _eject_paper(self) -> None:
        print("  Ejecting: Pen up...")
        self._send_safe("G1 E0.0 F4000")

        print("  Ejecting: Move X to 215...")
        self._send_safe("G0 X215.0 F6000.0")

        print("  Ejecting: Start motor (M106)...")
        self._send_safe("M106")

        print("  Ejecting: Push paper Y500...")
        self._send_safe("G0 Y500.0 F6000.0")

        print("  Ejecting: Wait (M400)...")
        self._send_safe("M400")

        print("  Ejecting: Stop motor (M107)...")
        self._send_safe("M107")

        print("Eject complete")

    def _send(self, gcode: str) -> None:
        self._ensure_port_open()
        payload = (gcode + "\n").encode("ascii", errors="ignore")
        self._port.write(payload)
        self._wait_for_ok()

    def _send_safe(self, gcode: str) -> None:
        try:
            self._send(gcode)
        except Exception:
            # Keep eject cycle resilient even if one command fails.
            pass

    def _wait_for_ok(self, timeout_seconds: int = 10) -> None:
        start = time.time()
        buffer = ""

        while True:
            if time.time() - start > timeout_seconds:
                # Keep parity with C# behavior: timeout does not fail the job.
                return

            try:
                data = self._read_existing()
                if data:
                    buffer += data
                    if "ok" in buffer.lower():
                        return
            except Exception:
                pass

            time.sleep(0.005)

    def _wait_for(self, expected: str, timeout_seconds: int) -> None:
        start = time.time()
        buffer = ""
        expected_lower = expected.lower()

        while True:
            if time.time() - start > timeout_seconds:
                raise TimeoutError(f"Timeout waiting for '{expected}'.")

            try:
                data = self._read_existing()
                if data:
                    buffer += data
                    if expected_lower in buffer.lower():
                        return
            except Exception:
                pass

            time.sleep(0.01)

    def _read_existing(self) -> str:
        self._ensure_port_open()
        waiting = getattr(self._port, "in_waiting", 0)
        if waiting <= 0:
            return ""
        raw = self._port.read(waiting)
        if not raw:
            return ""
        return raw.decode("ascii", errors="ignore")

    def _ensure_port_open(self) -> None:
        if not self._port or not self._port.is_open:
            raise RuntimeError("Printer port is not open.")

