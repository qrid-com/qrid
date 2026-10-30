using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Data;
using Qrid.Common.Http;
using Qrs.Mobile.Client;
using Qrs.Mobile.Client.Flows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Qrs.TestConsole.IntegrationTests
{
    public class TestBase //: ILog, IFilePasswordProvider
    {
        
        // these two are famous
        public HeadlessUser Alice { get; set; }
        public HeadlessUser Bob { get; set; }
        public HeadlessUser Admin { get; set; }

        public TestBase()
        {
            Admin = new HeadlessUser("admin");
            Alice = new HeadlessUser("alice");
            Bob = new HeadlessUser("bob");
        }


        private void FatalError(string v)
        {
            LogDebug($"FATAL ERROR: {v}");
            throw new ApplicationException($"FATAL ERROR: {v}");
        }

        public void LogDebug(string message)
        {
            var text = $"{DateTime.Now.ToString()} - {message}";
            var logFile = Environment.GetEnvironmentVariable("QRID_TEST_LOG_FILE");
            if (!string.IsNullOrWhiteSpace(logFile))
                System.IO.File.AppendAllText(logFile, text + Environment.NewLine);
            Console.WriteLine(text);
        }

        public void LogThrow(Exception ex)
        {
            LogDebug($"ERROR - - - {ex.Message}");
            throw ex;
        }

        public void LogThrow(string exceptionMessage)
        {
            LogDebug($"ERROR - LogThrow(string) - - - {exceptionMessage}");
            throw new ApplicationException(exceptionMessage);
        }

        public void DisplayAlert(string caption, string message, string button)
        {
            LogDebug($"{caption}: {message}");
        }

        public bool IsError<T>(HttpResponseModel<T> response) where T : Qrid.Common.Proto.BaseData
        {
            if (!response.IsSuccess())
            {
                // error, stay on this page
                DisplayAlert("ERROR", response.Message, "OK");
                return true;
            }
            return false;
        }

        public bool IsError<T>(ResponseModel<T> response) 
        {
            if (!response.IsSuccess())
            {
                // error, stay on this page
                DisplayAlert("ERROR", response.Message, "OK");
                return true;
            }
            return false;
        }

        #region ILog implementation
        public void LogInfo(string message)
        {
            LogDebug(message);
        }
        #endregion

        #region IFilePasswordProvider implementation

        public Task<ResponseModel<string>> CreateFilePassword(string filePath, bool createPasstxtFile)
        {
            throw new NotImplementedException("CreateFilePassword is not supported in test mode");
        }

        public async Task<ResponseModel<string>> GetFilePassword(string filePath)
        {
            var passtxtFilePath = filePath + ".passtxt";
            if (File.Exists(passtxtFilePath))
            {
                var lines = await File.ReadAllLinesAsync(passtxtFilePath);
                if (lines.Length > 0)
                    return ResponseModel.SuccessResponse(lines[0]);
            }
            return ResponseModel.SuccessResponse(string.Empty);
        }

        public async Task<ResponseModel<Dictionary<string, ResponseModel<string>>>> GetFilesPasswords(List<string> files)
        {
            var res = new Dictionary<string, ResponseModel<string>>();
            foreach (var file in files)
            {
                var fres = await GetFilePassword(file);
                res.Add(file, fres);
            }
            return ResponseModel.SuccessResponse(res);
        }

        #endregion



        public static async Task<bool> PollUntilTrue(Func<bool> f, int timeoutMs, int pollIntervalMs)
        {
            return await PollUntilTrue(() => Task.FromResult(f()), timeoutMs, pollIntervalMs);
        }

        public static async Task<bool> PollUntilTrue(Func<Task<bool>> f, int timeoutMs, int pollIntervalMs)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (await f())
                    return true;

                if (sw.ElapsedMilliseconds >= timeoutMs)
                    return false;

                var delayMs = (int)Math.Min(pollIntervalMs, timeoutMs - sw.ElapsedMilliseconds);
                if (delayMs > 0)
                    await Task.Delay(delayMs);
            }
        }


    }
}
