using Qrid.Common.Http;
using Qrid.Common.Proto.Crypto;
using Qrid.Proto.Enclave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrid.Common.Crypto.Enclave
{
    public interface IEnclave
    {

        // used in "Create New User" flow in QRID mobile app
        ResponseModel<NewUserCertifyResponse> NewUserCertify(NewUserCertifyRequest request, string fullUsername, Guid? userId);

        // used in "Restore Existing User" flow in QRID mobile app
        ResponseModel<ExistingUserEmailResponse> ExistingUserEmail(ExistingUserEmailRequest request);

        // for OFFLINE / ASYNC enclaves, this is to check the requested operation results, based on its EnclaveRequestId
        ResponseModel<StatusResponse> GetStatus(string enclaveRequestId);

        
    }
}
