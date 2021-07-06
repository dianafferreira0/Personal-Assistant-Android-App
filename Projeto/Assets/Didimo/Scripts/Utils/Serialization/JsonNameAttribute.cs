using System;

namespace Didimo.Utils.Serialization
{
    /// <summary>
    /// Name of the json attribute. This name will remain when de-serialized into a dictionary.
    /// 
    /// E.g.:
    /// 
    ///  [JsonName("customSerializationName")]
    ///  public string str;
    /// 
    /// When in a json string (serialized), the property will have the name "customSerializationName".
    /// When deserialized into a dictionary with <see cref="MiniJSON.Deserialize(string)"/>, the property name will remain as "customSerializationName".
    /// When deserialized into T with <see cref="MiniJSON.Deserialize{T}(string)"/>, the property "customSerializationName" will deserialize into the field name "str".
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class JsonNameAttribute : Attribute
    {
        private string name;

        public string Name { get { return name; } }

        public JsonNameAttribute(string name)
        {
            this.name = name;
        }
    }
}