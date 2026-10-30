using Google.Protobuf;
using QRCoder;
using Qrid.Common.Http;
using Qrid.Common.Proto.Const;
using Qrid.Common.Proto.Crypto;
using Qrid.Proto.Data;
using Qrid.Proto.Enclave;
using Qrid.Proto.Mailer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;
using GWK = Google.Protobuf.WellKnownTypes;
using QCP = Qrid.Common.Proto;
using QP = Qrid.Proto;

namespace Qrid.Common.Crypto.Enclave
{
    public class EnclaveProcessor : IEnclave
    {
        private CertifiedKeyPair enclaveCKP; // EnclaveRead KeyRole
        private CertifiedKeyPair domainCKP;  // DomainAccount KeyRole
        private string domain;

        public EnclaveProcessor(CertifiedKeyPair enclaveCKP, CertifiedKeyPair domainCKP, string domain)
        {
            this.enclaveCKP = enclaveCKP;
            this.domainCKP = domainCKP;
            this.domain = domain;
        }

        [Conditional("DEBUG")]
        private void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - {message}");
        }

        public ResponseModel<NewUserCertifyResponse> NewUserCertify(NewUserCertifyRequest request, string fullUsername, Guid? userId)
        {
            var certifyResponse = new NewUserCertifyResponse();

            // validate CSR
            if (!CryptoUtils.VerifyCertificateSigningRequest(request.AccountCertificateCsr, out var cpk, out var errorMessage2, KeyRole.CA.User, fullUsername, SubjectType.User))
                return ResponseModel.ErrorResponse<NewUserCertifyResponse>(errorMessage2);

            // create certificate - sign it with Domain DACC key
            var accountCert = CR.Certificate.Create(cpk);
            accountCert.CertifyingSig = CryptoUtils.SignCertified(accountCert.CertificateData, domainCKP);

#if DEBUG
            // if DEBUGGING, let's validate accountCert before returning it to client
            var x = CryptoUtils.ExplainCertificate(accountCert, out var explainContext);
            LogDebug(explainContext.ExplainedContent);
            var res = CryptoUtils.VerifyCertificate(accountCert, out var certContext);
            if (!res)
            {
                var errorMessage = $"AccountCert - CERTIFICATE ERROR - '{certContext.ErrorMessage}'";
                LogDebug(errorMessage);
                return ResponseModel.ErrorResponse<NewUserCertifyResponse>(errorMessage);
            }
            else
            {
                LogDebug("AccountCert - SUCCESS - is valid");
            }
#endif

            certifyResponse.AccountCertificate = accountCert;
            certifyResponse.ErrorCode = 0;
            return ResponseModel.SuccessResponse(certifyResponse);
        }

        public ResponseModel<ExistingUserEmailResponse> ExistingUserEmail(ExistingUserEmailRequest request)
        {
            var response = new ExistingUserEmailResponse();
            response.Username = request.Username;

            // decrypt EUD
            var eud = EncryptedUserData.Create(CryptoUtils.DecryptAsym(request.EncryptedUserData, enclaveCKP.KeyPair.PrivateKey));

            // generate random ephemeral sym key
            var ephKey = CryptoUtils.GetRandomSymKey();
            var ephKeyBase64 = ephKey.GetBase64String();

            // mailer request
            var mr = new MailerRequest();
            mr.TemplateName = "restore_account_qr_code";
            mr.Email = request.Email;
            mr.Params.Add(new MailerParam() { Key = "username", Value = request.Username });
            mr.Params.Add(new MailerParam() { Key = "domain", Value = domain.ToUpper() });
            // ephKey will be put into QR CODE emailed to user
            mr.Params.Add(new MailerParam() { Key = "qr_code_img_src", Value = GetQrCodeImgSrc(ephKeyBase64) });

            // re-encrypt EUD with ephKey
            response.MailEncryptedUserData = CryptoUtils.SymEncrypt(eud, ephKey, Array.Empty<byte>());
            response.EncryptedMailerRequest = CryptoUtils.EncryptAsym(mr.ToByteArray(), enclaveCKP.KeyPair.PublicKey);
#if DEBUG
            response.Hint = ephKeyBase64; // to simplify local development - no need to email anything
#endif
            return ResponseModel.SuccessResponse(response);
        }

        public ResponseModel<StatusResponse> GetStatus(string enclaveRequestId)
        {
            // this one is not supposed to be called
            throw new NotImplementedException("EnclaveProcesor.Status() is not supposed to be called");
        }

        private string GetQrCodeImgSrc(string qrCodeText)
        {
            var qrGen = new QRCodeGenerator();
            var data = qrGen.CreateQrCode(qrCodeText, QRCodeGenerator.ECCLevel.L);

            var qrCode = new PngByteQRCode(data);
            byte[] pngBytes = qrCode.GetGraphic(3);
            return $"data:image/png;base64, {Convert.ToBase64String(pngBytes)}";
        }
    }
}
