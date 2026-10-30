using Google.Protobuf;
using Microsoft.VisualBasic;
using Qrid.Common.Client.Api;
using Qrid.Common.Crypto;
using Qrid.Common.Http;
using Qrs.Proto;
using Qrs.Proto.Api;
using Qrs.Proto.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using QCP = Qrid.Common.Proto;

namespace Qrs.Client.Context
{
    public partial class ClientContext : CommonContext
    {
        public async Task<ResponseModel> WrapNewPayload(IEnumerable<WrapperBase> wrappersEnumerable)
        {
            var wrappers = wrappersEnumerable.ToList();
            var request = new KeyOpRequest() { AppName = this.AppName };
            
            // PREPARE bulk request
            for (int i=0; i< wrappers.Count; i++)
            {
                var wrapper = wrappers[i];

                // generate AsociatedData from DATA
                //wrapper.ADFromData = wrapper.GetAssociatedData2(); // TODO - do we need wrapper.ADFromData ???

                var requestItem = new KeyOpRequestItem()
                {
                    Index = i,
                    KeyOp = KeyOp.WrapNewPayload,
                    NewAssociatedDataBytes = wrapper.GetAssociatedData2().ToByteString(),
                    NewDekId = wrapper.GetImmutableDekId(),
                    NewTopLevelKey = wrapper.GetImmutableTopLevelKey(),
                    NewKeyRequest = true,
                    OldKeyRequest = false
                };
                
                request.Items.Add(requestItem);
            }

            // Call MA to "CREATE and WRAP" one or more DEKs in a single QR code scan operation
            var response = await KeyOpAsync(request);
            if (response.IsError())
                return ErrorResponse(response.Message);

            // WRAP PAYLOADs
            int cntErrors = 0;
            int cntTotal = 0;
            foreach (var responseItem in response.Data.Items)
            {
                cntTotal++;
                var wrapper = wrappers[responseItem.Index];
                
                if (responseItem.IsError())
                {
                    wrapper.Status = ErrorResponse($"QRS mobile app has returned Error for this item: '{responseItem.ErrorMessage}'. Item's Index='{responseItem.Index}'");
                    cntErrors++;
                    continue;
                }

                //wrapper.IDPAY = new IdentityData();
                //wrapper.IDPAY.DekId = wrapper.ADFromData.DekId;
                //wrapper.IDPAY.AppName = wrapper.ADFromData.AppName;

                wrapper.DEK = responseItem.NewUnwrappedKey;

                // Wrap Payload into QrsEnvelope
                var payload = await Task.Run(() => wrapper.GetPayloadMessageBytes()); // this takes 2+ seconds on 1Gb payload
                var envelope = new QrsEnvelope();
                envelope.IdentityDataBytes = responseItem.IdentityDataBytes;
                envelope.AssociatedDataBytes = responseItem.NewAssociatedDataBytes;
                envelope.EncryptedDek = responseItem.NewWrappedKey;
                envelope.EncryptedPayload = await Task.Run(() => CryptoUtils.SymEncrypt(payload, wrapper.DEK, envelope.IdBytes));
                wrapper.QrsEnvelope = envelope;
                wrapper.Status = SuccessResponse();
            }

            if (cntErrors == 0)
                return SuccessResponse();
            else
                return ErrorResponse($"Encountered {cntErrors} errors while processing {cntTotal} envelope wrappers");
        }

        public async Task<ResponseModel> WrapUpdatedPayload(IEnumerable<WrapperBase> wrappersEnumerable)
        {
            var wrappers = wrappersEnumerable.ToList();

            // TODO - triple check this method logic - too many combinations

            var request = new KeyOpRequest() { AppName = this.AppName };
            var sentToMobileApp = new List<WrapperBase>();

            // PREPARE bulk request
            for (int i = 0; i < wrappers.Count; i++)
            {
                var wrapper = wrappers[i];

                // TODO - this assumption below is wrong - for VAULT - we start the *new* VAULT app to WRAP updated files
                // so by the time we run this, we dont have DEK!!!

                // assuming original payload was UNWRAPPED from .QrsEnvelope
                // so original AD/ID could be obtained from .QrsEnvelope 
                // and .DEK is available

                // ID from ENV
                wrapper.IDPAY = wrapper.QrsEnvelope.GetIdentityData();

                // AD from ENV
                wrapper.ADFromEnvelope = wrapper.QrsEnvelope.GetAssociatedData2();

                // AD from DATA
                wrapper.ADFromData = wrapper.GetAssociatedData2();
                wrapper.ADFromData.CopyCarriedOverFields(wrapper.ADFromEnvelope); // KekName

                // validate ID - IdentityData 
                if (!wrapper.IsValidIdentityData(out var errorMessage))
                    return ErrorResponse(errorMessage);

                // has AD changed b/c of payload (and other data) being updated?
                var diffAd = wrapper.ADFromEnvelope.DifferenceWith(wrapper.ADFromData);
                wrapper.IsModifiedAssociatedData = diffAd != string.Empty;

                var helper = new RequestHelper();
                helper.IsModifiedPayload = wrapper.IsModifiedPayload;
                helper.IsModifiedAssociatedData = wrapper.IsModifiedAssociatedData;
                helper.GenerateNewDekOnModifiedPayload = wrapper.GenerateNewDekOnModifiedPayload;
                helper.IsDekOpen = wrapper.DEK != null;
                helper.Calculate();

                if (helper.NeedsMobileApp)
                {
                    // add Item to Request

                    var requestItem = new KeyOpRequestItem()
                    {
                        Index = i,
                        KeyOp = helper.KeyOp,
                        IdentityDataBytes = wrapper.QrsEnvelope.IdentityDataBytes,
                        OldAssociatedDataBytes = wrapper.QrsEnvelope.AssociatedDataBytes,
                        NewAssociatedDataBytes = wrapper.ADFromData.ToByteString(),
                        OldWrappedKey = wrapper.QrsEnvelope.EncryptedDek,
                        NewKeyRequest = helper.NeedsNewKey,
                        OldKeyRequest = helper.NeedsOldKey
                    };

                    request.Items.Add(requestItem);

                    // also track this wrapper in a separate list
                    sentToMobileApp.Add(wrapper);
                }
            }

            // WRAP/RESEAL/GENERATE DEKs in MA
            HttpResponseModel<KeyOpResponse> response = null;
            if (request.Items.Count > 0)
            {
                // at least 1 item where
                // call into MobileApp

                response = await KeyOpAsync(request);
                if (response.IsError())
                    return ErrorResponse(response.Message);
            }

            // Payload
            int cntErrors = 0;
            int cntTotal = 0;
            for (int i=0; i<wrappers.Count; i++)
            {
                var wrapper = wrappers[i];

                cntTotal++;
                if (sentToMobileApp.Contains(wrapper))
                {
                    // this wrapper was sent to MA (not every wrapper is necessarily sent to MA)
                    // look it up in a response
                    var responseItem = response.Data.Items.Where(item => item.Index == i).FirstOrDefault();
                    if (responseItem == null || responseItem.IsError())
                    {
                        wrapper.Status = ErrorResponse($"QRS mobile app has returned Error for this item: '{responseItem.ErrorMessage}'. Item's Index='{responseItem.Index}'");
                        cntErrors++;
                        continue;
                    }
                    // save WRAPPED DEK into EXISTING QrsEnvelope instance
                    // this could be newly-generated DEK, or re-wrapped existing DEK
                    wrapper.QrsEnvelope.EncryptedDek = responseItem.NewWrappedKey;
                    // save AD
                    wrapper.QrsEnvelope.AssociatedDataBytes = responseItem.NewAssociatedDataBytes;
                    // save unwrapped DEK
                    if (responseItem.KeyOp == KeyOp.WrapUpdatedPayload)
                    {
                        if (responseItem.NewUnwrappedKey != null)
                            wrapper.DEK = responseItem.NewUnwrappedKey; // newly-generated DEK
                        else if (responseItem.OldUnwrappedKey != null)
                            wrapper.DEK = responseItem.OldUnwrappedKey; // existing DEK that we asked MA to unwrap for us
                    }
                }

                
                // also support the case when only non-secret attributes were edited without unwrapping the DEK and decrypting Payload 
                // the non-secret attributes may be used as AD so it still may require round-trip to QRS app to RESEAl DEK with new AD. DEK itself is not modified, Payload is not modified.

                if (wrapper.IsModifiedPayload)
                {
                    if (wrapper.DEK == null)
                    {
                        // this is unintended, there must be some bug in the code
                        wrapper.Status = ErrorResponse("Unexpected Error - payload has been modified, but we dont have the DEK to re-encrypt payload - must be a bug");
                        cntErrors++;
                        continue;
                    }
                    // Wrap Payload into EXISTING QrsEnvelope instance
                    wrapper.QrsEnvelope.EncryptedPayload = await Task.Run(() => 
                    {
                        var payload = wrapper.GetPayloadMessageBytes(); // could be 1-2 sec for large payloads 1Gb+
                        return CryptoUtils.SymEncrypt(payload, wrapper.DEK, wrapper.QrsEnvelope.IdBytes); // could be 5-7 sec for large payloads 1Gb+
                    });

                }

                wrapper.Status = SuccessResponse();
            }


            if (cntErrors == 0)
                return SuccessResponse();
            else
                return ErrorResponse($"Encountered {cntErrors} errors while processing {cntTotal} envelope wrappers");
        }

        public async Task<ResponseModel> UnwrapPayload(IEnumerable<WrapperBase> wrappersEnumerable)
        {
            var wrappers = wrappersEnumerable.ToList();
            // prepare request
            var request = new KeyOpRequest() { AppName = this.AppName };
            for (int i = 0; i< wrappers.Count; i++)
            {
                var wrapper = wrappers[i];
                // AD from DATA
                wrapper.ADFromData = wrapper.GetAssociatedData2();
                // AD from saved Envelope
                wrapper.ADFromEnvelope = wrapper.QrsEnvelope.GetAssociatedData2();
                // IDPAY
                wrapper.IDPAY = wrapper.QrsEnvelope.GetIdentityData();

                // validate ID - IdentityData 
                if (!wrapper.IsValidIdentityData(out var errorMessage))
                    return ErrorResponse(errorMessage);

                
                // validation 3 - check AD Items in ENV and DATA
                var diff2 = wrapper.ADFromEnvelope.DifferenceInItemsWith(wrapper.ADFromData);
                if (diff2 != string.Empty)
                    return ErrorResponse($"Invalid QrsEnvelope - Associated Data from envelope are not matching with the real data: {diff2}");

                var requestItem = new KeyOpRequestItem()
                {
                    Index = i,
                    KeyOp = KeyOp.Unwrap,
                    IdentityDataBytes = wrapper.QrsEnvelope.IdentityDataBytes,
                    OldAssociatedDataBytes = wrapper.QrsEnvelope.AssociatedDataBytes,
                    OldWrappedKey = wrapper.QrsEnvelope.EncryptedDek,
                    NewKeyRequest = false,
                    OldKeyRequest = true
                };

                request.Items.Add(requestItem);
            }

            // UNWRAP DEKs in a single QR code scan operation
            var response = await KeyOpAsync(request);
            if (response.IsError())
                return ErrorResponse(response.Message);


            // UNWRAP PAYLOADs - process response
            int cntErrors = 0;
            int cntTotal = 0;
            foreach(var responseItem in response.Data.Items)
            {
                cntTotal++;
                var wrapper = wrappers[responseItem.Index];

                if (responseItem.IsError())
                {
                    wrapper.Status = ErrorResponse($"QRS mobile app has returned Error for this item: '{responseItem.ErrorMessage}'. Item's Index='{responseItem.Index}'");
                    cntErrors++;
                    continue;
                }

                // we've got DEK key unwrapped!
                wrapper.DEK = responseItem.OldUnwrappedKey;

                // Decrypt Payload
                var payloadMessage = await Task.Run(() => 
                {
                    var payloadBytes = CryptoUtils.SymDecrypt(wrapper.QrsEnvelope.EncryptedPayload, wrapper.DEK, wrapper.QrsEnvelope.IdBytes); // TODO - add try/catch ?
                    return QCP.TypedMessage.Create(payloadBytes); 
                });

                // Save Payload into Envelope
                // this Validates Payload type, then sets Payload property in the Envelope
                var payloadError = wrapper.SetPayload(payloadMessage);
                if (!string.IsNullOrWhiteSpace(payloadError))
                {
                    wrapper.Status = ErrorResponse(payloadError);
                    cntErrors++;
                    continue;
                }

                wrapper.Status = SuccessResponse();
            }

            if (cntErrors == 0)
                return SuccessResponse();
            else
                return ErrorResponse($"Encountered {cntErrors} errors while processing {cntTotal} envelope wrappers");
        }


        public async Task<ResponseModel> WrapNewPayload(WrapperBase wrapper)
        {
            var res = await WrapNewPayload(new List<WrapperBase>() { wrapper });
            return res;
        }

        public async Task<ResponseModel> WrapUpdatedPayload(WrapperBase wrapper)
        {
            var res = await WrapUpdatedPayload(new List<WrapperBase>() { wrapper });
            return res;
        }

        public async Task<ResponseModel> UnwrapPayload(WrapperBase wrapper)
        {
            var res = await UnwrapPayload(new List<WrapperBase>() { wrapper });
            return res;
        }

    }
}
