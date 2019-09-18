using System;

namespace DJson
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class JsonElementAttribute : Attribute
	{
		public enum ParseType
		{
			AsValue,
			AsObject,
			AsArraydObject,
			AsArraydValues,
			AsSource
		}

		public string JsonName;
		public ParseType Type;
		public Type ForceConvert;
		public bool CanNull;

		/// <param name="jsonName">json key name</param>
		/// <param name="forceConvert">converts value to Type</param>
		/// <param name="canNull">can value be null or not defined</param>
		public JsonElementAttribute(string jsonName, Type forceConvert, bool canNull = false)
		{
			JsonName = jsonName;
			CanNull = canNull;
			Type = ParseType.AsValue;
			ForceConvert = forceConvert;
		}

		/// <param name="jsonName">json key name</param>
		/// <param name="canNull">can value be null or not defined</param>
		public JsonElementAttribute (string jsonName, bool canNull)
		{
			JsonName = jsonName;
			CanNull = canNull;
			Type = ParseType.AsValue;
		}

		/// <param name="jsonName">json key name</param>
		/// <param name="type">how we need parse field</param>
		/// <param name="forceConvert">converts value to Type</param>
		/// <param name="canNull">can value be null or not defined</param>
		public JsonElementAttribute (string jsonName, ParseType type = ParseType.AsValue, bool canNull = false, Type forceConvert = null)
		{
			JsonName = jsonName;
			Type = type;
			ForceConvert = forceConvert;
			CanNull = canNull;
		}
	}
}
