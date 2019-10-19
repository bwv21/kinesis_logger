# Logger

## 익명 객체 로그

```csharp
Int32 i = 42;

loggerAPI.Push("anonymous_type",
                new
                {
                    ValueInt = i,
                    ValueString = i.ToString()
                });
```

## 인스턴스 로그

```csharp
var log = new Log
{
    ValueInt = 42,
    ValueString = "42"
};

loggerAPI.Push("instance_type", log);
```

## 문자열 로그

```csharp
loggerAPI.Push("string_type", "this is log text....");
```

## JSON 문자열 로그

```csharp
loggerAPI.PushJsonString("json_string", @"{
                                              ""ValueInt"": 42,
                                              ""ValueString"" : ""42""
                                          }");
```
