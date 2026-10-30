using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Qrs.App.SudoAskPass.Cli.Terminal
{
    /// <summary>
    /// Terminal helper for the sudo askpass helper program.
    ///
    /// The decrypted sudo password is the ONLY thing that may be written to stdout
    /// (sudo reads the password from the helper's stdout). Therefore every piece of
    /// interactive output (QR codes, prompts, status, errors) is written to the
    /// controlling terminal (/dev/tty) when available, falling back to stderr.
    /// User input (pairing code, password during init) is read from /dev/tty when
    /// available, falling back to stdin.
    /// </summary>
    public sealed class TerminalIo : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly TextReader _reader;
        private readonly FileStream? _ttyStream;
        private readonly StreamWriter? _ttyWriter;
        private readonly StreamReader? _ttyReader;

        public TerminalIo()
        {
            // Try to open the controlling terminal directly so that we keep working
            // even when stdin/stdout are redirected (e.g. when invoked by `sudo -A`).
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists("/dev/tty"))
            {
                try
                {
                    _ttyStream = new FileStream("/dev/tty", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    _ttyWriter = new StreamWriter(_ttyStream, new UTF8Encoding(false)) { AutoFlush = true };
                    _ttyReader = new StreamReader(_ttyStream, new UTF8Encoding(false));
                }
                catch
                {
                    // No controlling terminal available - fall back to stderr/stdin below.
                    _ttyStream?.Dispose();
                    _ttyStream = null;
                    _ttyWriter = null;
                    _ttyReader = null;
                }
            }

            _writer = _ttyWriter ?? Console.Error;
            _reader = _ttyReader ?? Console.In;
        }

        /// <summary>True when a real controlling terminal (/dev/tty) is in use.</summary>
        public bool HasTty => _ttyStream != null;

        public void WriteLine(string message = "")
        {
            _writer.WriteLine(message);
            _writer.Flush();
        }

        public void Write(string message)
        {
            _writer.Write(message);
            _writer.Flush();
        }

        public string? ReadLine()
        {
            return _reader.ReadLine();
        }

        /// <summary>
        /// Reads a line of input without echoing it back to the terminal.
        /// Uses `stty` (Linux/macOS) to disable echo around the read.
        /// </summary>
        public string ReadHidden()
        {
            var echoDisabled = TrySetEcho(false);
            try
            {
                var value = _reader.ReadLine() ?? string.Empty;
                return value;
            }
            finally
            {
                if (echoDisabled)
                {
                    TrySetEcho(true);
                    // The pressed Enter was not echoed, so emit a newline ourselves.
                    WriteLine();
                }
            }
        }

        private bool TrySetEcho(bool enabled)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                // Drive stty against the controlling terminal so it works under ssh too.
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = enabled ? "-c \"stty echo < /dev/tty\"" : "-c \"stty -echo < /dev/tty\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                    return false;
                if (!process.WaitForExit(2000))
                {
                    // stty hung - terminate it to avoid leaking the process
                    try { process.Kill(true); } catch { /* best effort */ }
                    return false;
                }
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _ttyWriter?.Dispose();
            _ttyReader?.Dispose();
            _ttyStream?.Dispose();
        }
    }
}
