using Qrid.Common.Analytics;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Http;
using Qrs.App.Analytics.Client.Models;

namespace Qrs.App.Analytics.Client
{
    public class AnalyticsContext : CommonContext
    {
        public ContextSettings ContextSettings;
        public DbClient Db;
        public AnalyticsClient Api;
        public IAnalyticLogger Logger;

        public AnalyticsContext(ContextSettings contextSettings, ICookieProvider cookieProvider, IAnalyticLogger logger)
        {
            ContextSettings = contextSettings;
            Db = new DbClient(ContextSettings.SQLiteFile);
            Api = new AnalyticsClient(ContextSettings.BaseUri, cookieProvider);
            Api.ApiKey = ContextSettings.ApiKey;
            Logger = logger;
        }

        public void Init()
        {
            Db.Init();
        }

        public async Task UpdateAndLog()
        {
            if (!ContextSettings.IsActive) return;
            
            var res = await UpdateSessionLogData();

            if (res.IsSuccess())
            {
                if (res.Data.Item2?.Count > 0)
                    Logger.Log($"{res.Data.Item2?.Count} new items, {res.Data.Item1} response items");
                else
                    Logger.Log($"no new items, {res.Data.Item1} response items");
            }
            else
                Logger.Log($"ERROR: {res.Message}");
        }

        public async Task<ResponseModel<Tuple<int, List<SessionLogItem>>>> UpdateSessionLogData()
        {
            // call API
            var apiResponse = await Api.GetSessionLog();
            if (apiResponse.IsError())
                return NpErrorResponse<Tuple<int, List<SessionLogItem>>>(apiResponse);

            // clear session log at server
            var clearResponse = await Api.ClearSessionLog();
            if (clearResponse.IsError())
                NpErrorResponse<Tuple<int, List<SessionLogItem>>>(clearResponse);

            // save into DB if does not exist already
            var newSessions = new List<SessionLogItem>();
            Db.ConnectionOpenIfClosed();
            foreach (var item in apiResponse.Data)
            {
                if (!Db.Exists(item.InstanceId, item.SessionId))
                {
                    Db.Insert(item);
                    newSessions.Add(item);
                }
            }

            Db.ConnectionClose();

            var res = new Tuple<int, List<SessionLogItem>>(apiResponse.Data.Count, newSessions);
            return NpSuccessResponse<Tuple<int, List<SessionLogItem>>>(res);
        }

        public void LogSessionItems(List<SessionLogItem> sessionLogItems)
        {
            foreach (var item in sessionLogItems)
            {
                LogSessionItem(item);
            }
        }

        public void LogSessionItem(SessionLogItem item)
        {
            Logger.Log(item.ToLogString());   
        }

    }
}
