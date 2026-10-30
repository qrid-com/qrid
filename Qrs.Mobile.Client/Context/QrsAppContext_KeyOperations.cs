using Google.Protobuf;
using Qrid.Common.Crypto;
using Qrid.Common.Proto.Crypto;
using Qrs.Mobile.Client.Flows;
using Qrs.Proto;
using Qrs.Proto.Api;
using System;

namespace Qrs.Mobile.Client.Context
{
    public partial class QrsAppContext 
    {

        public KeyOpResponse KeyOp()
        {
            var request = QrCommandFlow.KeyOpRequest;
            var res = new KeyOpResponse();
            foreach (var item in QrCommandFlow.RequestItems)
            {
                var requestItem = item.KeyOpRequestItem;
                var responseItem = new KeyOpResponseItem();
                responseItem.Index = requestItem.Index;
                responseItem.KeyOp = requestItem.KeyOp; // echo back the requested operation so the CA knows how to process this item
                if (item.IsError)
                {
                    responseItem.ErrorMessage = item.ErrorMessage;
                }
                else
                {
                    IdentityData idData = null;
                    CryptoKey keyToWrap = null;
                    switch(requestItem.KeyOp)
                    {
                        case Proto.Api.KeyOp.Unwrap:
                            // UNWRAP
                            responseItem.OldUnwrappedKey = CryptoKey.Create(CryptoUtils.SymDecrypt(requestItem.OldWrappedKey, OrangeKeys.MasterKeySym, requestItem.OldAdIdBytes));
                            break;
                        case Proto.Api.KeyOp.WrapNewPayload:
                            // WRAP NEW
                            // 1. generate new random DEK
                            responseItem.NewUnwrappedKey = CryptoUtils.GetRandomSymKey();
                            // 2. generate new ID Data
                            idData = new IdentityData();
                            idData.AppName = request.AppName;
                            idData.TopLevelKey = requestItem.NewTopLevelKey;
                            idData.Created = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.Date);
                            idData.DekId = requestItem.NewDekId?.Length > 0 
                                ? requestItem.NewDekId 
                                : ByteString.CopyFrom(CryptoUtils.GetRandomBytes(16));
                            // 3. set current MasterKey Name into AD 
                            item.NewAssociatedData.KekName = OrangeKeys.KeyName;
                            // 4. serialize AD and ID into response
                            responseItem.NewAssociatedDataBytes = item.NewAssociatedData.ToByteString();
                            responseItem.IdentityDataBytes = idData.ToByteString();
                            // 5. wrap DEK 
                            responseItem.NewWrappedKey = CryptoUtils.SymEncrypt(responseItem.NewUnwrappedKey, OrangeKeys.MasterKeySym, responseItem.NewAdIdBytes);
                            break;
                        case Proto.Api.KeyOp.WrapUpdatedPayload:
                            // WRAP UPDATED

                            // 0. unwrap old key, if requested and available wrapped
                            if (requestItem.OldKeyRequest && requestItem.OldWrappedKey != null)
                            {
                                responseItem.OldUnwrappedKey = CryptoKey.Create(CryptoUtils.SymDecrypt(requestItem.OldWrappedKey, OrangeKeys.MasterKeySym, requestItem.OldAdIdBytes));
                            }

                            // 1. generate new random DEK (if requested), otherwise keep using the existing DEK
                            if (requestItem.NewKeyRequest)
                            {
                                responseItem.NewUnwrappedKey = CryptoUtils.GetRandomSymKey();
                                keyToWrap = responseItem.NewUnwrappedKey;
                            }
                            else
                            {
                                // no new DEK requested - the existing DEK is kept and re-wrapped with the new AD
                                // (it is only returned to the CA when the CA has asked for it)
                                if (responseItem.OldUnwrappedKey == null)
                                {
                                    responseItem.ErrorMessage = "Cannot wrap updated payload - no new key was requested and the existing key is not available to be re-wrapped";
                                    break;
                                }
                                keyToWrap = responseItem.OldUnwrappedKey;
                            }

                            // 3. set current MasterKey Name into AD 
                            item.NewAssociatedData.KekName = OrangeKeys.KeyName;
                            
                            // 4. serialize AD and ID into response
                            responseItem.NewAssociatedDataBytes = item.NewAssociatedData.ToByteString();
                            // this never worked before b/c idData is always null here!
                            // TODO: we need to carefully analyse this flow in details
                            responseItem.IdentityDataBytes = requestItem.IdentityDataBytes; // idData.ToByteString(); << this never worked before b/c idData is always null here!
                            // 5. wrap DEK 
                            responseItem.NewWrappedKey = CryptoUtils.SymEncrypt(keyToWrap, OrangeKeys.MasterKeySym, responseItem.NewAdIdBytes);
                            break;
                        case Proto.Api.KeyOp.Reseal:
                            // RESEAL - the DEK itself is not changed, it only gets re-wrapped with the modified Associated Data
                            // this is very similar to WRAP UPDATED - but no unwrapped key is ever returned to the CA

                            if (requestItem.OldWrappedKey == null)
                            {
                                responseItem.ErrorMessage = "Cannot reseal - the existing wrapped key is missing in the request";
                                break;
                            }

                            // 1. unwrap the existing DEK using the OLD AD+ID (never returned to the CA)
                            keyToWrap = CryptoKey.Create(CryptoUtils.SymDecrypt(requestItem.OldWrappedKey, OrangeKeys.MasterKeySym, requestItem.OldAdIdBytes));

                            // 2. set current MasterKey Name into AD 
                            item.NewAssociatedData.KekName = OrangeKeys.KeyName;

                            // 3. serialize AD and ID into response - IdentityData is immutable, it is carried over as is
                            responseItem.NewAssociatedDataBytes = item.NewAssociatedData.ToByteString();
                            responseItem.IdentityDataBytes = requestItem.IdentityDataBytes;

                            // 4. wrap the very same DEK using the NEW AD+ID
                            responseItem.NewWrappedKey = CryptoUtils.SymEncrypt(keyToWrap, OrangeKeys.MasterKeySym, responseItem.NewAdIdBytes);
                            break;
                        default:
                            responseItem.ErrorMessage = $"Unknown KeyOp code = '{requestItem.KeyOp}'";
                            break;
                    }

                }
                res.Items.Add(responseItem);
            }

            return res;
        }

    }
}
