using System;

namespace Qrid.Common.Analytics
{
    public class SessionLogItem
    {
        public string InstanceId { get; set; }
        public int SessionId { get; set; } // NOT a security-related field, this is not used for security, used only for analytics
        public string App { get; set; }
        public string Ip  { get; set; }
        public DateTime Created { get; set; }

        public string ToLogString()
        {
            return $"App='{App}',Ip='{Ip}',InstanceId='{InstanceId}',SessionId='{SessionId}',Created='{Created}'";
        }
    }
}
