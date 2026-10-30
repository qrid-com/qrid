using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Qrid.Server.Const;
using System.Net;
using Qrid.Common.Server.Models;

namespace Qrid.Common.Server.Controller
{
    public partial class MyControllerBase
    {
        #region  Json Response

        protected ActionResult JsonSuccess(object payload = null, JsonResponseStatus status = null)
        {
            status = status ?? JsonResponseStatus.SUCCESS;
            var model = new HttpResponseModel
            {
                Status = status.GetValue()
            };
            if (payload != null)
            {
                if (payload is String)
                {
                    model.Message = (string)payload;
                }
                else
                {
                    model.Data = payload;
                }
            }

            var strModel = JsonConvert.SerializeObject(model);
            LogDebug($"response: {strModel}");


            return Ok(model);
        }

        protected ActionResult JsonSuccess(JsonResponseStatus status)
        {
            return JsonSuccess(null, status);
        }

        protected async Task<ActionResult> JsonSuccessAsync(JsonResponseStatus status)
        {
            return await Task.FromResult(JsonSuccess(null, status)); // todo - remove "await" here
        }

        protected async Task<ActionResult> JsonSuccessAsync(object payload = null, JsonResponseStatus status = null)
        {
            return await Task.FromResult(JsonSuccess(payload, status)); // todo - remove "await" here
        }
        #endregion

        // TODO - this whole mess needs to be cleaned - will do it later
        #region JsonError     

        protected HttpResponseMessage Error(HttpStatusCode statusCode = HttpStatusCode.Forbidden)
        {
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        }

        protected ActionResult JsonError(string error)
        {
            var response = Json(new JsonCommonResponse
            {
                Status = "ERROR",
                Message = error
            });
            LogDebug($"ERROR - response - {JsonConvert.SerializeObject(response.Value)}");
            return response;
        }

        protected async Task<ActionResult> JsonErrorAsync(string error)
        {
            return await Task.FromResult(JsonError(error));
        }

        protected ActionResult JsonError(
            JsonResponseStatus status = null,
            Exception exception = null,
            HttpStatusCode statusCode = HttpStatusCode.OK
        )
        {
            status = status ?? JsonResponseStatus.ERROR;
            var error = new List<string>();
            if (exception == null)
            {
                foreach (var modelError in GetErrorsFromModelState())
                    if (modelError.Trim() != string.Empty)
                        error.Add(modelError);
            }
            else
            {
                error.Add(exception.Message);
            }
            var response = Json(new JsonCommonResponse
            {
                Status = status.GetValue(),
                Errors = error
            });
            response.StatusCode = (int)statusCode;

            LogDebug($"ERROR - response - {JsonConvert.SerializeObject(response.Value)}");

            return response;
        }







        #endregion
    }
}