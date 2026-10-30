using Qrid.Common.Proto.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrid.Common.Mailer
{
    public interface IMailer
    {
        public void SendMail(AsymEncryptedBytes encryptedMailerRequest);

    }
}
