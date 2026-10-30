using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Qrid.Common.Server.Controller
{
    // todo: should we inherit from ControllerBase instead of Controller? Controller has View Support - do we need this for API controllers ????
    public partial class MyControllerBase : Microsoft.AspNetCore.Mvc.Controller
    {
        public readonly ILogger<MyControllerBase> _Logger;

        public MyControllerBase(ILogger<MyControllerBase> logger)
        {
            this._Logger = logger;
        }



        [Conditional("DEBUG")]
        protected void LogDebug(string message)
        {
            Debug.WriteLine($"{DateTime.Now} - {message}");
            _Logger.LogDebug(message);
        }

        #region ModelState
        protected List<string> GetErrorsFromModelState()
        {
            var errors = new List<string>();
            foreach (var value in ModelState.Values)
            {
                foreach (var error in value.Errors)
                {
                    var errorMessage = !string.IsNullOrEmpty(error.ErrorMessage)
                        ? error.ErrorMessage
                        : error.Exception.Message;
                    errors.Add(errorMessage);
                }
            }
            return errors;
        }
        #endregion

        protected T GetRequestBody<T>()
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    // Arguments: Stream, Encoding, detect encoding, buffer size 
                    // AND, the most important: keep stream opened
                    using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true))
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                    }
                }
            }
            catch (Exception e)
            {
                _Logger.LogError("Parse request body error");
                _Logger.LogError(e.Message, e);
                return default(T);
            }
        }


    }
}