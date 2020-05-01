# DJson

Easy way to work with JSON


Serialize example:
```csharp
JsonInterface root = new JsonInterface();

root["int"].Value = 10;
root["name"].Value = "Test";

for(int i = 0; i < 10; i++)
{
    root["array of values"][i] = i * 10;
}

string json = JsonConverter.ParseJsonString(root);
```

Deserialize example:
```csharp
string json = "{\"test\":\"this is string \"what\"\"}";

JsonInterface root = JsonInterface.ParseJson(json);

string test = root["test"].Value;
```

Deserialize as object (struct/class and etc..):
```csharp
string json = "{\"hello"\:true, \"this is must be enum\":1 "\arr"\:[1, 2, 3, 4], \"array of objs\":[{\"this is in object\":1, "some int":100}, {\"this is in object\":1, "some int":1337}, {\"this is in object\":0, "some int":15}]}"

enum TestEnum
{
    None, // = 0
    Something,
    ThisIs2
}

struct B
{
    [JsonElement("some int")]
    public int SomeInteger;
    
    [JsonElement("this is in object")]
    public int ThisIsInObj;
}

struct A
{
    [JsonElement("hello")] // [JsonElement("hello", JsonElementAttribute.ParseType.AsValue)]
    public string HelloVariable;
    
    [JsonElement("arr", JsonElementAttribute.ParseType.AsArraydValues)]
    public int[] ArraydValues;
    
    [JsonElement("array of objs", JsonElementAttribute.ParseType.AsArraydObject)]
    public B[] objs;
}

JsonInterface root = JsonConverter.ParseJson(json);

// also if you need deserialize multiple objects you can use
// out[] = JsonConverter.ParseObjects<out>(JsonInterface)
A a_object = JsonConverter.ParseObject<A>(root);
```

Also you can Deserialize object:
```csharp
struct A
{
    [JsonElement("test")]
    public int Test;
    
    [JsonElement("arr", JsonElementAttribute.ParseType.AsArraydValues)]
    public int[] ArrayOfInt;
}

A a_obj = A
{
    Test = 10,
    ArrayOfInt = { 1, 2, 3, 4, 5 }
};

// also if you need deserialize multiple objects you can use
// JsonInterface = JsonConverter.UnParseObjects(object[])
JsonInterface unserialized_a = JsonConverter.UnParseObject(a_obj);
```
