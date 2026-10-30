using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using Qrid.Common.Proto;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Mail
{
    public partial class MailMessage : Qrid.Common.Proto.ProtoBase
    {

        // NOT included into protobuf serialization
        // used for convenience to be bound to controls, represents same data as .Recipients
        public string RecipientsString { get; set; } 

        public static MailMessage Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static MailMessage Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static MailMessage Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public List<string> ReceiversToList()
        {
            var res = new List<string>(Receivers.Count);
            foreach (var r in Receivers) res.Add(r);
            return res;
        }

        public void ReceiversAddFromString(string recipients)
        {
            Receivers.Add(Utils.ParseRecipients(recipients));
        }

        public string Recipients
        {
            get 
            { 
                return String.Join("; ", Receivers);

                // var res = new StringBuilder(50);
                // foreach (var r in Receivers) res.Append(r);
                // return res;
            }
        }

        public string BodyTruncated
        {
            get
            {
                if (string.IsNullOrEmpty(Body))
                    return string.Empty;

                if (Body.Length > 100)
                    return Body.Substring(0, 100) + " ...";
                else
                    return Body;
            }
        }


        public static MailMessage CreateReplyMessage(MailMessage old, string sender)
        {
            var res = new MailMessage();
            if (old != null)
            {
                res.Subject = old.Subject.StartsWith("Re:") ? old.Subject : "Re: " + old.Subject;
                res.Body = Environment.NewLine + Environment.NewLine + old.Body;
                res.Receivers.Add(old.Sender);
                res.RecipientsString = res.Recipients;
            }
            res.Sender = sender;
            return res;
        }

        public static MailMessage CreateReplyAllMessage(MailMessage old, string sender)
        {
            var res = new MailMessage();
            res.Subject = old.Subject.StartsWith("Re:") ? old.Subject : "Re: " + old.Subject;
            res.Body = Environment.NewLine + Environment.NewLine + old.Body;
            res.Receivers.Add(old.ReceiversToList());
            res.Receivers.Add(sender);
            res.RecipientsString = res.Recipients;
            res.Sender = sender;
            return res;
        }

        public static MailMessage CreateForwardMessage(MailMessage old, string sender)
        {
            var res = new MailMessage();
            res.Subject = old.Subject.StartsWith("Fw:") ? old.Subject : "Fw: " + old.Subject;
            res.Body = Environment.NewLine + Environment.NewLine + old.Body;
            //res.Receivers.Add(old.ReceiversToList());
            //res.Receivers.Add(sender);
            //res.RecipientsString = res.Recipients;
            res.Sender = sender;
            return res;
        }

    }
}