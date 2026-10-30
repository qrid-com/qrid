namespace Qrid.Server.Const
{
    public class JsonResponseStatus : AConstant<JsonResponseStatus>
    {
        public JsonResponseStatus() { }

        private JsonResponseStatus(string value, string name) : base(value, name) { }

        #region SUCCESS statuses
        public static readonly JsonResponseStatus SUCCESS = new JsonResponseStatus("SUCCESS", null);
        #endregion

        #region FAIL statuses
        public static readonly JsonResponseStatus ERROR = new JsonResponseStatus("ERROR", null);
        public static readonly JsonResponseStatus INCORRECT_MODEL_DATA = new JsonResponseStatus("INCORRECT_MODEL_DATA", null);
        public static readonly JsonResponseStatus ACCESS_DENIED = new JsonResponseStatus("ACCESS_DENIED", null);
        public static readonly JsonResponseStatus EMAIL_NOT_FOUND = new JsonResponseStatus("EMAIL_NOT_FOUND", null);
        public static readonly JsonResponseStatus USER_NOT_FOUND = new JsonResponseStatus("USER_NOT_FOUND", null);
        public static readonly JsonResponseStatus INCORRECT_JSON = new JsonResponseStatus("INCORRECT_JSON", null);
        public static readonly JsonResponseStatus SENDER_HAS_NO_MONEY = new JsonResponseStatus("SENDER_HAS_NO_MONEY", null);

        public override bool IsValid(string Value)
        {
            throw new NotImplementedException();
        }

        public override JsonResponseStatus FromString(string Value)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
