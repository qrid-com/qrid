using Qrid.Common.Data;
using Qrid.Common.Proto;
using Qrs.Proto;
using Qrs.Proto.Api;
using System.Collections.Generic;
using System.Text;
using Qrs.Proto.Extensions;
using Qrs.Mobile.Client.Data;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

namespace Qrs.Mobile.Client.Flows
{
    public class QrCommandFlow
    {
        // last scanned command
        public QrCommand QrCommand;
        public string ManualQrCodeString;


        //public QP.Qrs.AssociatedData AssociatedData { get; set; }

        // last Error while downloading/executing Command Request
        public string ErrorMessage;

        // request validation errors - can be ignored by turning on Setting to ignore validation errors
        // (this is session-only Setting, it cannot be permanently saved)
        // invalid Associated Data, invalid AppName, etc
        public Dictionary<string, string> ValidationErrors;

        // request validation - FATAL errors - cannot be ignored, cannot see "Yes" button even if Setting to ignore validation errors is set
        // missing KekName key, multiple AppNames, etc
        public Dictionary<string, string> FatalErrors;

        public TypedMessage Payload;

        // keyop command
        public KeyOpRequest KeyOpRequest;
        public List<RequestItem> RequestItems;
        public Dictionary<KeyOp, int> KeyOpCount;
        public bool TopLevelKey;
        public int InvalidRequestCount;

        // it is possible that instead of a command, we would need to do a PAIRING first
        public PairingRequest PairingRequest;

        // security alerts about the Tunnel Server URL (shown on the Confirmation screen)
        public List<string> Alerts;
        // when non-null, this server URL should be added to known servers if the user confirms the operation
        public string PendingKnownServer;

        
        public void Init(QrCommand qrCommand)
        {
            ClearRequest();
            QrCommand = qrCommand;
            ManualQrCodeString = string.Empty;
        }

        public void ClearRequest()
        {
            ValidationErrors = new Dictionary<string, string>();
            FatalErrors = new Dictionary<string, string>();
            ErrorMessage = string.Empty;
            Payload = null;
            KeyOpRequest = null;
            RequestItems = null;
            KeyOpCount = null;
            TopLevelKey = false;
            PairingRequest = null;
            InvalidRequestCount = 0;
            Alerts = new List<string>();
            PendingKnownServer = null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("KeyOpRequest: ");
            sb.AppendLine(KeyOpRequest == null ? "null" : "NOT null");
            return sb.ToString();
        }

        public bool HasPayload()
        {
            return Payload != null;
        }

        public bool HasPairing()
        {
            return PairingRequest != null;
        }

        public bool HasError()
        {
           return !string.IsNullOrWhiteSpace(ErrorMessage);
        }

        public bool HasValidationErrors()
        {
            return (ValidationErrors?.Count ?? 0) > 0;
        }

        public bool HasFatalErrors()
        {
            return (FatalErrors?.Count ?? 0) > 0; ;
        }

        public bool IsWrapKeyCommand()
        {
            return QrCommand.Cmd == "wrapkey";
        }

        public bool IsMasterKeyNameRequired()
        {
            return KeyOpCount[KeyOp.WrapUpdatedPayload] > 0 || KeyOpCount[KeyOp.Reseal] > 0 || KeyOpCount[KeyOp.Unwrap] > 0;
        }
        

        public void ParsePayload(string kekName)
        {
            // after downloading Payload as TypedMessage, we come here to PARSE it 

            switch (QrCommand.Cmd)
            {
                case "keyop":
                    ParseKeyOpPayload();
                    break;
                default:
                    // UI - show error
                    ErrorMessage = $"QR code contains unsupported cmd='{QrCommand.Cmd}'";
                    break;
            }
        }

        public void ParseKeyOpPayload()
        {
            TopLevelKey = false;
            KeyOpRequest = Payload.Create<KeyOpRequest>();
            RequestItems = new List<RequestItem>(KeyOpRequest.Items.Count);
            KeyOpCount = new Dictionary<KeyOp, int>(4);
            KeyOpCount[KeyOp.Unwrap] = 0;
            KeyOpCount[KeyOp.WrapNewPayload] = 0;
            KeyOpCount[KeyOp.WrapUpdatedPayload] = 0;
            KeyOpCount[KeyOp.Reseal] = 0;
            InvalidRequestCount = 0;
            foreach (var i in KeyOpRequest.Items)
            {
                var ri = new RequestItem()
                {
                    KeyOpRequestItem = i,
                    IdentityData = IdentityData.Create(i.IdentityDataBytes),
                    OldAssociatedData = AssociatedData2.Create(i.OldAssociatedDataBytes),
                    NewAssociatedData = AssociatedData2.Create(i.NewAssociatedDataBytes)
                };
                ri.Validate();
                if (ri.IsError)
                {
                    FatalErrors.Add($"Invalid Request Item #{i.Index}", $"Error: {ri.ErrorMessage}");
                    InvalidRequestCount++;
                }
                RequestItems.Add(ri);
                KeyOpCount[i.KeyOp] = KeyOpCount[i.KeyOp] + 1;
                TopLevelKey = TopLevelKey || ri.IdentityData?.TopLevelKey == 1 || i.NewTopLevelKey == 1;
            }
        }



        public void ValidateCommand(string loadedKekName)
        {

            if (RequestItems.Count == 0)
                FatalErrors.Add("Empty Request", $"At least one request item is required");

            var appNames = RequestItems.Select(x => x.IdentityData?.AppName ?? KeyOpRequest.AppName)
                .Append(KeyOpRequest.AppName)
                .Distinct().ToList();
            var kekNames = RequestItems.Select(x => x.OldAssociatedData?.KekName ?? string.Empty)
                .Union(RequestItems.Where(x => !string.IsNullOrWhiteSpace(x.NewAssociatedData?.KekName)).Select(x => x.NewAssociatedData?.KekName ?? string.Empty))
                .Distinct().ToList();

            // AppName being the same in all items
            if (appNames.Count > 1)
                ValidationErrors.Add("Multiple Application Names", $"Only one AppName value is allowed in the single request. Found the following AppNames: {string.Join(",", appNames)}");
            else if (appNames.Count == 0)
            {
                ValidationErrors.Add("Missing Application Name", $"Application Name is required");
                return;
            }
            // AppName being in CANONICAL format
            if (!IsCanonicalAppName(appNames[0]))
                ValidationErrors.Add("Invalid Application Name", $"Application Name is '{appNames[0]}' while it must be all UPPERCASE and/or DIGITS with words separated by exactly one 'space' or 'dash' character");


            // KekName, when specified, is the same in all items
            if (kekNames.Count > 1)
                FatalErrors.Add("Multiple Master Keys", $"Only one Master Key Name value is allowed in the single request. Found the following Names: {string.Join(",", kekNames)}");
            else if (kekNames.Count == 0)
            {
                // only allowed when we have "WrapNewPayload" and no other KeyOp types
                if (IsMasterKeyNameRequired())
                {
                    FatalErrors.Add("Missing Master Key Name", $"Master Key Name is required for this request");
                    return;
                }
            }

            // KekName - compare with loaded Master Key
            if (IsMasterKeyNameRequired())
            {
                // KekName must be specified and must match with the loaded Master Key
                if (string.IsNullOrEmpty(kekNames[0]))
                {
                    FatalErrors.Add("Empty Master Key Name", $"Request contains an empty Master Key Name which is not allowed for this request");
                }
                else
                {
                    // KekName is NOT EMPTY, so it must match loaded Master Key
                    if (kekNames[0] != loadedKekName)
                        // KekName is NOT loaded into OrangeKeys - Fatal Error 
                        FatalErrors.Add("Master Key is not loaded", $"Master Key with name '{kekNames[0]}' is not loaded into this QRS app. If you have a Backup Image and Password for that Master Key, please load your Master Key and then try again");
                }
            }
        }



        public bool IsValidCommand()
        {
            return (!HasValidationErrors()) &&
                (!HasFatalErrors());
        }

        public bool IsImpossibleToContinueCommand()
        {
            return HasFatalErrors();
        }

        public bool IsCanonicalAppName(string appName)
        {
            // valid - UPPERCASE letters and DIGITS, separated by exactly one "space" or "dash" (no "dot"?)
            // empty is invalid - at least one WORD is required by that regex
            return Regex.IsMatch(appName, @"^(?-i)(\b[A-Z0-9]+\b(-|\x20)?)+\b$");
        }



    }
}
