using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;

namespace Qrid.Common.Proto
{
    public class ProtoBase : BaseData
    {

        public static ProtoBase Null = null;

        protected IMessage IM => (IMessage)this;

        public void MergeFrom(byte[] bytes)
        {
            IM.MergeFrom(bytes);
        }

        public virtual DataType GetDataType()
        {
            // do nothing
            throw new NotImplementedException("not implemented here - this needs to be overwritten in a deriving class");
        }

        public virtual byte[] GetByteArray()
        {
            return IM.ToByteArray();

            //return GetByteString().ToByteArray();
        }

        public virtual ByteString GetByteString()
        {
            return IM.ToByteString();

            // do nothing
            //throw new NotImplementedException("not implemented here - this needs to be overwritten in a deriving class");
        }

        public virtual string GetBase64String()
        {
            //return Convert.ToBase64String(GetByteArray());

            // alternative implementation:
            return IM.ToByteString().ToBase64();
        }

        public string GetBase64Safe()
        {
            // make it safe for FILE NAMES and for URLs:
            // replace "\" with "_" and "+" with "-"
            // as per https://www.rfc-editor.org/rfc/rfc3548#page-6

            var res = GetBase64String();
            res = res.Replace('/', '_').Replace('+', '-');
            return res;
        }




        public Timestamp GetTimestamp(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return Timestamp.FromDateTime(dateTime);
            else
                return Timestamp.FromDateTime(dateTime.ToUniversalTime());
        }

        public static T Create<T>(byte[] bytes) where T : ProtoBase, new()
        {
            if (bytes == null || bytes.Length == 0) return null;

            T res = new T();
            res.IM.MergeFrom(bytes);
            return res;                
        }

        public static T Create<T>(ByteString byteString) where T : ProtoBase, new()
        {
            if (byteString == null || byteString.Length == 0) return null;

            T res = new T();
            res.IM.MergeFrom(byteString);
            return res;
        }

        public static T Create<T>(string base64string) where T : ProtoBase, new()
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            T res = Create<T>(bytes);
            return res;
        }

    }
}
