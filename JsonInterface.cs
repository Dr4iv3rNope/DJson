using System;
using System.Collections.Generic;

namespace DJson
{
	public enum JsonInterfaceType
	{
		Void,
		Object,
		Array,
		Value
	}

	public class JsonException : Exception
	{
		public JsonException (string Message) : base(Message) { }
	}

	public class JsonInterface
	{
		public const string DJsonVersion = "v1.1";

		public JsonInterfaceType Type { get; private set; }

		public List<JsonInterface> Interfaces = new List<JsonInterface>();
		public JsonInterface Parent;
		public string Key;
		private dynamic value;

		public dynamic Value
		{
			get => value;

			set
			{
				ChangeType(JsonInterfaceType.Value, false);

				this.value = value;
			}
		}


		public void ChangeType (JsonInterfaceType type, bool checkInterfaces = true)
		{
			if ( Type == type )
				return;

			if ( !checkInterfaces || type == JsonInterfaceType.Value )
			{
				Interfaces.Clear();

				goto setVal;
			}

			if ( type != JsonInterfaceType.Value )
			{
				value = null;
			}

			foreach ( var jsonInterface in Interfaces )
				if ( type == JsonInterfaceType.Object
					? jsonInterface.Key == null
					: jsonInterface.Key != null)

					Interfaces.Remove(jsonInterface);

			setVal:

			Type = type;
		}

		public JsonInterface (
			JsonInterfaceType type = JsonInterfaceType.Void,
			string key = null,
			dynamic value = null)
		{
			Type = type;
			Key = JsonConverter.CleanUpString(key);
			this.value = value is string s ? JsonConverter.ParseStringValue(s) : value;
		}

		public JsonInterface Insert (JsonInterface jsonInterface)
		{
			if ( jsonInterface == null )
				return this;

			if ( jsonInterface.Type == JsonInterfaceType.Void )
			{
				ChangeType(jsonInterface.Key != null
					? JsonInterfaceType.Object
					: jsonInterface.Key == null
					? JsonInterfaceType.Array
					: throw new JsonException("JsonInterface.Insert: Can't resolve Void type"));
			}

			if ( jsonInterface.Key == null )
			{
				if ( Type == JsonInterfaceType.Object )
					throw new JsonException(
						"Tried insert JsonInterface without index and key to JsonInterface with Type Object");
			}

			Interfaces.Add(jsonInterface);
			jsonInterface.Parent = this;

			return jsonInterface;
		}


		public JsonInterface Get (string key)
		{
			foreach ( var jsonInterface in Interfaces.ToArray() )
				if ( jsonInterface.Key == key)
					return jsonInterface;

			return null;
		}

		public JsonInterface Get (int index) =>
			Interfaces[index];

		JsonInterface CreateTemplate (string key = null, int? index = null) =>
			Insert(new JsonInterface(JsonInterfaceType.Void, key, index));

		public JsonInterface this[string key] =>
			Get(key) ?? CreateTemplate(key);

		public JsonInterface this[int index] =>
			Get(index) ?? CreateTemplate(null, index);
	}
}
