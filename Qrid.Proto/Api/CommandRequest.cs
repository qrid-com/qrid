using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Api
{
    public partial class CommandRequest : Qrid.Common.Proto.ProtoBase
    {

        public static CommandRequest Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CommandRequest Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CommandRequest Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static CommandRequest Create(string commandJson, string command, string service)
        {
            var res = new CommandRequest();
            res.CommandJson = commandJson;
            res.CommandName = command;
            res.serviceName_ = service;
            return res;
        }

    }
}