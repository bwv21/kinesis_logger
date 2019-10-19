using System;
using Newtonsoft.Json;

namespace KLogger.Libs
{
    // " 없이 Serialize 하기 위한 클래스.
    internal class PlainJsonStringConverter : JsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return objectType == typeof(String);
        }

        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            return reader.Value;
        }

        public override void WriteJson(JsonWriter writer, Object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteRawValue((String)value);
        }
    }
}
