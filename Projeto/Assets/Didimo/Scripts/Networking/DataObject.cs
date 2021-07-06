using Didimo.Utils.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Networking
{
    /// <summary>
    /// Class with a static method to load it from a json string.
    /// </summary>
    public class DataObject
    {
        public static T LoadFromJson<T>(string jsonText) where T : DataObject
        {
            return MiniJSON.Deserialize<T>(jsonText) as T;

        }

        public static string Serialize(object obj)
        {
            return MiniJSON.Serialize(obj);
        }

        public WWWForm GetForm()
        {
            WWWForm form = new WWWForm();
            string json = MiniJSON.Serialize(this);
            Dictionary<object, object> dict = MiniJSON.Deserialize(json) as Dictionary<object, object>;

            foreach (string key in dict.Keys)
            {
                string value = MiniJSON.Serialize(dict[key]);
                if (value == null)
                {
                    value = "";
                }
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.TrimStart('\"');
                    value = value.TrimEnd('\"');
                }
                //Debug.Log(key + ": " + value);
                form.AddField(key, value);
            }

            return form;
        }
    }
}