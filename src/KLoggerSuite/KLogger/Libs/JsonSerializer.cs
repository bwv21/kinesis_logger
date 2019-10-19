using System;
using Newtonsoft.Json;

namespace KLogger.Libs
{
    // 스레드에 안전해야 한다(Newtonsoft.Json은 안전).
    internal class JsonSerializer
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
                                                                          {
                                                                              NullValueHandling = NullValueHandling.Ignore // null 항목 무시.
                                                                          };

        public String Serialize<T>(T obj) where T : class
        {
            return JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
        }

        public T Deserialize<T>(String objString)
        {
            return JsonConvert.DeserializeObject<T>(objString, _jsonSerializerSettings);
        }
    }
}
