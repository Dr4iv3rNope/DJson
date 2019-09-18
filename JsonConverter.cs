using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DJson
{
	public static class JsonConverter
	{
		#region Parse string value

		public static string CleanUpString(string key) =>
			key != null
				? key.StartsWith("\"") && key.EndsWith("\"") ? key.Substring(1, key.Length - 2) : key
				: null;

		public static dynamic ParseStringValue(object Data)
		{
			if (Data == null) return null;

			string DataString = Data.ToString();

			if (DataString == "null") return null;
			if (DataString == "false") return false;
			if (DataString == "true") return true;

			if (DataString.Contains(".") && double.TryParse(DataString, out double doubleResult))
				return doubleResult;

			if (decimal.TryParse(DataString, out decimal decimalResult))
				return decimalResult;

			return CleanUpString(DataString);
		}

		#endregion

		#region Parse json

		private static bool IsStringEnd(string json, int index)
		{
			char i = json[index];

			if (i == '\"')
			{
				int j = index;
				int count = 0;

				while (j > 0 && json[--j] == '\\')
				{
					count++;
				}

				return count % 2 == 0;
			}

			return false;
		}

		private static KeyValuePair<string, string> ParseJson_KeyPairValue(string JsonValue)
		{
			string key = null;
			string val = null;

			bool IsString = false;
			bool IsValue = true;

			StringBuilder builder = new StringBuilder();

			for (int index = 0; index < JsonValue.Length; index++)
			{
				char i = JsonValue[index];

				if (IsStringEnd(JsonValue.ToString(), index))
					IsString = !IsString;

				if (!IsString)
				{
					if (i == ' ') continue;

					if (i == ':' && IsValue)
					{
						IsValue = false;

						key = builder.ToString();
						builder.Clear();
						continue;
					}

					if (i == '\0' || index == JsonValue.Length - 1)
					{
						val = builder.Append(i).ToString();
						break;
					}
				}

				builder.Append(i);
			}

			return new KeyValuePair<string, string>(key, val);
		}

		private static JsonInterface ParseJson_ParseValue(string json)
		{
			if (json.Length == 0) return null;

			var pair = ParseJson_KeyPairValue(json);

			var i = new JsonInterface(JsonInterfaceType.Value, pair.Key, pair.Value);

			return i;
		}

		private static JsonInterface ParseJson_StartObject(JsonInterface Parent, string json, bool IsArray)
		{
			var pair = ParseJson_KeyPairValue(json);

			var i = new JsonInterface(IsArray ? JsonInterfaceType.Array : JsonInterfaceType.Object, pair.Key, pair.Value);

			return Parent != null ? Parent.Insert(i) : i;
		}

		private static JsonInterface ParseJson_EndObject(JsonInterface Last, string json)
		{
			var value = ParseJson_ParseValue(json);

			Last.Insert(value);

			return Last.Parent ?? Last;
		}

		private static void ParseJson_EndValue(JsonInterface Parent, string json)
		{
			JsonInterface parsedValue = ParseJson_ParseValue(json);

			Parent.Insert(parsedValue);
		}

		public static JsonInterface ParseJson(string Json)
		{
			StringBuilder builder = new StringBuilder();

			JsonInterface last = null;

			bool IsString = false;

			for (int i = 0; i < Json.Length; i++)
			{
				char j = Json[i];

				if ( j == '\0' )
					continue;

				if (IsStringEnd(Json, i))
					IsString = !IsString;

				if (!IsString)
					switch (j)
					{
						case char c when c == '\t' || c == '\n':
							continue;

						case ',':
							ParseJson_EndValue(last, builder.ToString());
							builder.Clear();

							continue;

						case char c when c == '{' || c == '[':
							last = ParseJson_StartObject(last, builder.ToString(), c == '[');
							builder.Clear();

							continue;

						case char c when c == '}' || c == ']':
							last = ParseJson_EndObject(last, builder.ToString());
							builder.Clear();

							continue;
					}

				builder.Append(j);
			}

			return last;
		}

		#endregion

		#region Parse JsonInterface as json

		private static string ParseJsonString_Value(JsonInterface value)
		{
			if (value.Type != JsonInterfaceType.Value)
				throw new JsonException("ParseJsonString_Value: Tried parse JsonInterface as value, but JsonInterface is not a value");

			return $"{(value.Key != null ? $"\"{value.Key}\":" : "")}{(value.Value is string s ? $"\"{s}\"" : value.Value is bool b ? b.ToString().ToLower() : value.Value != null ? value.Value : "null")}";
		}

		private static string ParseJsonString_Auto(JsonInterface jsonInterface)
		{
			switch (jsonInterface.Type)
			{
				case JsonInterfaceType type when type == JsonInterfaceType.Object || type == JsonInterfaceType.Array:
					return ParseJsonString_Container(jsonInterface);

				case JsonInterfaceType.Value:
					return ParseJsonString_Value(jsonInterface);

				case JsonInterfaceType.Void:
					return "";
			}

			throw new JsonException("ParseJsonString_Auto: JsonInterface has unsupported type");
		}

		private static string ParseJsonString_Container(JsonInterface obj)
		{
			if(obj.Type != JsonInterfaceType.Object && obj.Type != JsonInterfaceType.Array)
				throw new JsonException("ParseJsonString_Container: Tried parse JsonInterface as object or array, but JsonInterface is not a object or array");

			bool IsArray = obj.Type == JsonInterfaceType.Array;

			var builder = new StringBuilder((obj.Key != null ? $"\"{obj.Key}\":" : "") + (IsArray ? "[" : "{"));

			for (var i = 0; i < obj.Interfaces.Count; i++)
			{
				var jsonInterface = obj.Interfaces[i];

				builder.Append(ParseJsonString_Auto(jsonInterface));

				if (i != obj.Interfaces.Count - 1)
					builder.Append(",");
			}

			return builder.Append(IsArray ? "]" : "}").ToString();
		}

		/// <summary>
		/// Parse <code>JsonInterface</code> as string that contains json
		/// </summary>
		/// <returns>Ready json</returns>
		/// <exception cref="JsonException"></exception>
		public static string ParseJsonString(JsonInterface root) =>
			ParseJsonString_Container(root);

		#endregion

		#region Parse JsonInterface as object (structure)

		private static void ParseObject_ArraydValues(JsonInterface jsonObj, object obj, FieldInfo field)
		{
			if (!field.FieldType.IsArray) return;

			var arrayType = field.FieldType;
			var type = arrayType.GetElementType();

			if (type == null) return;

			var newArray = Array.CreateInstance(type, jsonObj.Interfaces.Count);

			for (var index = 0; index < jsonObj.Interfaces.Count; index++)
			{
				var value = jsonObj.Interfaces[index].Value;

				newArray.SetValue(value, index);
			}

			field.SetValue(obj, newArray);
		}

		private static void ParseObject_ArraydObject(JsonInterface jsonObj, object obj, FieldInfo field)
		{
			if (!field.FieldType.IsArray) return;

			var arrayType = field.FieldType;
			var type = arrayType.GetElementType();

			if (type == null) return;

			var newArray = Array.CreateInstance(type, jsonObj.Interfaces.Count);

			for (var index = 0; index < jsonObj.Interfaces.Count; index++)
			{
				object instance = Activator.CreateInstance(type);

				newArray.SetValue(ParseObject_Object(jsonObj.Interfaces[index], instance), index);
			}

			field.SetValue(obj, newArray);
		}

		private static void ParseObject_Value(JsonInterface value, object obj, FieldInfo field, JsonElementAttribute attribute = null)
		{
			if (value.Type != JsonInterfaceType.Value)
			{
				if (attribute.CanNull)
					return;

				throw new JsonException("ParseObject_Value: Tried parse JsonInterface as value, but JsonInterface is not a value");
			}

			if (value.Value != null && attribute != null)
				value.Value = Convert.ChangeType(value.Value,
					attribute.ForceConvert ?? (Nullable.GetUnderlyingType(field.FieldType) != null
						? field.FieldType.GetGenericArguments()[0]
						: field.FieldType));

#if DEBUG
			try
			{
#endif
				field.SetValue(obj, value.Value);
#if DEBUG
			}
			catch ( Exception ex )
			{
				throw ex;
			}
#endif
		}

		private static object ParseObject_Object(JsonInterface jsonObj, object obj, FieldInfo field = null, JsonElementAttribute attribute = null)
		{
			if ( jsonObj.Type != JsonInterfaceType.Object )
			{
				if ( attribute != null && attribute.CanNull )
					goto returnObject;

				throw new JsonException("ParseObject_Object: Tried parse JsonInterface as object, but JsonInterface is not a object");
			}

			var fields = field == null ? obj.GetType().GetFields() : field.FieldType.GetFields();

			foreach (var fieldInfo in fields)
			{
				ParseObject_Auto(jsonObj, obj, fieldInfo);
			}

			returnObject:

			return obj;
		}

		private static void ParseObject_Auto(JsonInterface jsonInterface, object obj, FieldInfo field)
		{
			var attributes = field.GetCustomAttributes(typeof(JsonElementAttribute), false);

			if (attributes.Length != 1) return;

			var attribute = (JsonElementAttribute)attributes[0];

			if ( field.IsStatic || obj == null )
				return;

#if DEBUG
			try
			{
#endif
				switch ( attribute.Type )
				{
					case JsonElementAttribute.ParseType.AsValue:
						ParseObject_Value(jsonInterface[attribute.JsonName], obj, field, attribute);
						break;

					case JsonElementAttribute.ParseType.AsObject:
						field.SetValue(obj, ParseObject_Object(jsonInterface[attribute.JsonName], field.GetValue(obj), field, attribute));
						break;

					case JsonElementAttribute.ParseType.AsArraydObject:
						ParseObject_ArraydObject(jsonInterface[attribute.JsonName], obj, field);
						break;

					case JsonElementAttribute.ParseType.AsArraydValues:
						ParseObject_ArraydValues(jsonInterface[attribute.JsonName], obj, field);
						break;

					case JsonElementAttribute.ParseType.AsSource:
						field.SetValue(obj, jsonInterface[attribute.JsonName]);
						break;
				}
#if DEBUG
			}
			catch (Exception ex)
			{
				throw ex;
			}
#endif
		}

		public static T ParseObject<T>(JsonInterface jsonInterface) where T : new()
		{
			T obj = new T();

			obj = (T)ParseObject_Object(jsonInterface, obj);

			return obj;
		}

		/// <exception cref="JsonException"></exception>
		public static T[] ParseObjects<T>(JsonInterface jsonInterface) where T : new()
		{
			if ( jsonInterface.Type != JsonInterfaceType.Array )
				throw new JsonException($"ParseObjectArrayd: {nameof(jsonInterface)} is not array");

			var array = new T[jsonInterface.Interfaces.Count];

			for ( int index = 0; index < array.Length; index++ )
			{
				var i = jsonInterface.Interfaces[index];

				T obj = ParseObject<T>(i);

				array[index] = obj;
			}

			return array;
		}

		#endregion

		#region Unparse object (structure) as JsonInterface

		private static void UnParseObject_ArraydObject(JsonInterface root, object obj, FieldInfo field)
		{
			var array = (IList)field.GetValue(obj);

			for (var index = 0; index < array.Count; index++)
			{
				var element = array[index];

				UnParseObject_Object(root[index], element);
			}
		}

		private static void UnParseObject_ArraydValues(JsonInterface root, object obj, FieldInfo field)
		{
			var array = (IList)field.GetValue(obj);

			for (var index = 0; index < array.Count; index++)
			{
				var element = array[index];

				root[index].Value = element;
			}
		}

		private static void UnParseObject_Value(JsonInterface scheme, object value, Type convertTo = null)
		{
			if (value != null && convertTo != null)
				value = Convert.ChangeType(value, convertTo);

			scheme.Value = value;
		}

		private static void UnParseObject_Object(JsonInterface root, object obj, FieldInfo field = null)
		{
			if (field != null && obj != null && Nullable.GetUnderlyingType(field.FieldType) != null)
			{
				obj = Convert.ChangeType(obj, field.FieldType.GetGenericArguments()[0]);

				field = null;
			}

			var fields = field == null ? obj.GetType().GetFields() : field.FieldType.GetFields();

			foreach (var _field in fields)
			{
				var attributes = _field.GetCustomAttributes(typeof(JsonElementAttribute), false);

				if (attributes.Length == 0) continue;

				var attribute = (JsonElementAttribute)attributes[0];

				var value = _field.GetValue(obj);

				switch (attribute.Type)
				{
					case JsonElementAttribute.ParseType.AsValue:

						if (value == null && attribute.CanNull)
							break;

						UnParseObject_Value(root[attribute.JsonName], value, attribute.ForceConvert);
						break;

					case JsonElementAttribute.ParseType.AsSource:
						var _value = (JsonInterface) value;
						_value.Key = attribute.JsonName;

						root.Insert(_value);
						break;

					case JsonElementAttribute.ParseType.AsObject:
						if (value == null && attribute.CanNull)
							break;

						UnParseObject_Object(root[attribute.JsonName], value, _field);
						break;

					case JsonElementAttribute.ParseType.AsArraydObject:
						UnParseObject_ArraydObject(root[attribute.JsonName], obj, _field);
						break;

					case JsonElementAttribute.ParseType.AsArraydValues:
						UnParseObject_ArraydValues(root[attribute.JsonName], obj, _field);
						break;
				}
			}
		}

		public static JsonInterface UnParseObject(object obj)
		{
			var i = new JsonInterface();

			UnParseObject_Object(i, obj);

			return i;
		}


		public static JsonInterface UnParseObjects(object[] objs)
		{
			var array = new JsonInterface(JsonInterfaceType.Array);

			foreach ( var obj in objs )
			{
				JsonInterface i = new JsonInterface();

				UnParseObject_Object(i, obj);

				array.Insert(i);
			}

			return array;
		}

		#endregion

		#region JsonInterface methods

		public static T[] FromArrayJsonInterface<T> (JsonInterface jsonInterface)
		{
			if ( jsonInterface.Type != JsonInterfaceType.Array )
				throw new JsonException($"{nameof(jsonInterface)} is not array");

			var array = new T[jsonInterface.Interfaces.Count];

			for ( int i = 0; i < jsonInterface.Interfaces.Count; i++ )
			{
				var el = jsonInterface.Interfaces[i];

				if ( el.Type == JsonInterfaceType.Value )
					array[i] = el.Value; 
			}

			return array;
		}

		public static JsonInterface GetArrayInterface (object[] array)
		{
			var i = new JsonInterface(JsonInterfaceType.Array);

			for ( int index = 0; index < array.Length; index++ )
			{
				i[index].Value = array[index];
			}

			return i;
		}

		#endregion
	}
}
