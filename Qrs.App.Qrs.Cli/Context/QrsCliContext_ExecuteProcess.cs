using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.App.Qrs.Cli.Models;
using Qrs.App.Qrs.Cli.Payload;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Proto.Extensions;
using Qrs.Shared.Data;
using Qrs.Tunnel.Client;
using Spectre.Console;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Qrs.App.Qrs.Cli.Context
{
    public partial class QrsCliContext 
    {

        public Task<int> Execute(string processFile, string processArguments, Dictionary<string, string> envars)
        {
            var source = new TaskCompletionSource<int>();
            var escapedCmd = processArguments.Replace("\"", "\\\""); // wtf??? TODO
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processFile, // "/bin/bash" ???
                    Arguments = escapedCmd,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    //RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };

            foreach (var kvp in envars)
            {
                process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }

            process.Exited += (sender, args) =>
            {
                if (process.ExitCode == 0)
                {
                    source.SetResult(0);
                }
                else
                {
                    source.SetException(new Exception($"Process '{process.StartInfo.FileName} {process.StartInfo.Arguments}' failed with exit code [{process.ExitCode}]"));
                }
                process.Dispose();
            };

            try
            {
                WriteHeader($"Executing: [blue]{process.StartInfo.FileName} {process.StartInfo.Arguments}[/]");
                process.Start();
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
                source.SetException(e);
            }

            return source.Task;
        }

        public Task<int> ExecuteCmd(string cmd, Dictionary<string, string> envars)
        {
            var source = new TaskCompletionSource<int>();
            var escapedCmd = cmd.Replace("\"", "\\\""); // wtf??? TODO
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd", // "/bin/bash" ???
                    Arguments = "/c " + escapedCmd,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    //RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };

            foreach (var kvp in envars)
            {
                process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }

            process.Exited += (sender, args) =>
            {
                //var processErrors = process.StandardError.ReadToEnd();
                //var processOutput = process.StandardOutput.ReadToEnd();

                //if (!string.IsNullOrWhiteSpace(processErrors))
                //{
                //    Context.WriteError(processErrors);
                //}
                //if (!string.IsNullOrWhiteSpace(processOutput))
                //{
                //    Context.WriteLine(processOutput);
                //}

                if (process.ExitCode == 0)
                {
                    source.SetResult(0);
                }
                else
                {
                    source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
                }

                process.Dispose();
            };

            try
            {
                WriteHeader($"Executing: [blue]{process.StartInfo.FileName} {process.StartInfo.Arguments}[/]");
                process.Start();
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
                source.SetException(e);
            }

            return source.Task;
        }

        public Task<int> ExecuteBash(string cmd, Dictionary<string, string> envars)
        {
            var source = new TaskCompletionSource<int>();
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash", // "/bin/bash" ???
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            foreach (var kvp in envars)
            {
                process.StartInfo.EnvironmentVariables.Add(kvp.Key, kvp.Value);
            }

            process.Exited += (sender, args) =>
            {
                var processErrors = process.StandardError.ReadToEnd();
                var processOutput = process.StandardOutput.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(processErrors))
                {
                    WriteError(processErrors);
                }
                if (!string.IsNullOrWhiteSpace(processOutput))
                {
                    WriteLine(processOutput);
                }

                if (process.ExitCode == 0)
                {
                    source.SetResult(0);
                }
                else
                {
                    source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
                }

                process.Dispose();
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
                source.SetException(e);
            }

            return source.Task;
        }

    }
}
