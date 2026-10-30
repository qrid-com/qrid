using Google.Protobuf;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qrid.Common.Proto
{
    /// <summary>
    /// Serialises any <see cref="ProtoBase"/> subclass as a base-64 string so that
    /// System.Text.Json never tries to walk Protobuf-generated properties such as
    /// <c>Google.Protobuf.ByteString</c>, which it cannot deserialise.
    /// </summary>
    public class ProtoBaseJsonConverter<T> : JsonConverter<T> where T : ProtoBase, new()
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var base64 = reader.GetString();
            return ProtoBase.Create<T>(base64); // returns null for null/empty
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.GetBase64String());
        }
    }

    /// <summary>
    /// Converter factory that creates a <see cref="ProtoBaseJsonConverter{T}"/> for any
    /// concrete <see cref="ProtoBase"/> subclass.
    /// </summary>
    public class ProtoBaseJsonConverterFactory : JsonConverterFactory
    {
        public static readonly ProtoBaseJsonConverterFactory Instance = new();

        public override bool CanConvert(Type typeToConvert)
            => !typeToConvert.IsAbstract
               && typeToConvert.IsSubclassOf(typeof(ProtoBase));

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(ProtoBaseJsonConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}