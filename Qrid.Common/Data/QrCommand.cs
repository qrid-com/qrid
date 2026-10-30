using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Qrid.Common.Data
{
    public class QrCommand : QrKeyValueBase
    {
        public const string CMD_LOGIN = "login";
        
        public static QrCommand Create(string qr)
        {
            var res = new QrCommand();
            res.Parse(qr);
            return res;
        }

        public QrCommand():base()
        {
        }
        public string App
        {
            get { return GetParam("app"); }
            set { SetParam("app", value); }
        }
        public string Cmd
        {
            get { return GetParam("cmd"); }
            set { SetParam("cmd", value); }
        }
        public string Server
        {
            get { return GetParam("srv"); }
            set { SetParam("srv", value); }
        }
        public string ClientPub
        {
            get { return GetParam("cpb"); }
            set { SetParam("cpb", value); }
        }
        public string ClientPubHash
        {
            get { return GetParam("cph"); }
            set { SetParam("cph", value); }
        }
        public string QrVer
        {
            get { return GetParam("v"); }
            set { SetParam("v", value); }
        }
        public string SessionKey
        {
            get { return GetParam("sk"); }
            set { SetParam("sk", value); }
        }
        public string ClientProtocolVersion
        {
            get { return GetParam("cpv"); }
            set { SetParam("cpv", value); }
        }
        
        public bool IsValidCmd => IsValid ;

    }
}
