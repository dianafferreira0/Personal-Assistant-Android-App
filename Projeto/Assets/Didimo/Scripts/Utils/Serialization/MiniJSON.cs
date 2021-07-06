/*
 * 
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Didimo.Utils.Serialization
{
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
    /// All numbers are parsed to doubles.
    /// </summary>
    public static class MiniJSON
    {
        /// <summary>
        /// Deserialize into a List, dictionary, or primitive type.
        /// </summary>
        /// <param name="json">The json string</param>
        /// <returns>The deserialzied object.</returns>
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return DictionaryParser.Parse(json);
        }

        /// <summary>
        /// Deserialize the json string into the specified object.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize into.</typeparam>
        /// <param name="json">The json string</param>
        /// <returns>The deserialzied object.</returns>
        public static T Deserialize<T>(string json)
        {
            //if the object is a string, just return it
            if (typeof(T).Equals(typeof(string)))
            {
                return (T)Convert.ChangeType(json, typeof(string));
            }
            object dict = Deserialize(json);
            return (T)ObjectParser.DeserializeIntoType(typeof(T), dict);
        }

        /// <summary>
        ///  This parser deserializes into a Dictionary, List, or primitive type.
        /// </summary>
        sealed class DictionaryParser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            public static bool IsWordBreak(char c)
            {
                return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            };

            StringReader json;

            DictionaryParser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new DictionaryParser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            Dictionary<object, object> ParseDictionary()
            {
                Dictionary<object, object> table = new Dictionary<object, object>();

                // ditch opening brace
                json.Read();

                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            return table;
                        default:
                            // name
                            object value = ParseValue();
                            if (value == null)
                            {
                                return null;
                            }

                            // :
                            if (NextToken != TOKEN.COLON)
                            {
                                return null;
                            }
                            // ditch the colon
                            json.Read();

                            // value
                            table[value] = ParseValue();
                            break;
                    }
                }
            }

            List<object> ParseArray()
            {
                List<object> array = new List<object>();

                // ditch opening bracket
                json.Read();

                var parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = NextToken;

                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.SQUARED_CLOSE:
                            parsing = false;
                            break;
                        default:
                            object value = ParseByToken(nextToken);

                            array.Add(value);
                            break;
                    }
                }

                return array;
            }

            object ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING:
                        return ParseString();
                    case TOKEN.NUMBER:
                        return ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return ParseDictionary();
                    case TOKEN.SQUARED_OPEN:
                        return ParseArray();
                    case TOKEN.TRUE:
                        return 1;
                    case TOKEN.FALSE:
                        return 0;
                    case TOKEN.NULL:
                        return null;
                    default:
                        return null;
                }
            }

            string ParseString()
            {
                StringBuilder s = new StringBuilder();
                char c;

                // ditch opening quote
                json.Read();

                bool parsing = true;
                bool ignoreNextQuotes = false;
                while (parsing)
                {
                    if (json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }

                    c = NextChar;
                    switch (c)
                    {
                        case '\\':
                            ignoreNextQuotes = true;
                            s.Append(c);
                            break;
                        case '"':
                            if (ignoreNextQuotes)
                            {
                                s.Append(c);
                            }
                            else
                            {
                                ignoreNextQuotes = false;
                                parsing = false;
                            }
                            break;
                        default:
                            ignoreNextQuotes = false;
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;

                if (number.IndexOf('.') == -1)
                {
                    long parsedInt;
                    Int64.TryParse(number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedInt);
                    return parsedInt;
                }

                double parsedDouble;
                Double.TryParse(number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedDouble);
                return parsedDouble;
            }

            void EatWhitespace()
            {
                while (Char.IsWhiteSpace(PeekChar))
                {
                    json.Read();

                    if (json.Peek() == -1)
                    {
                        break;
                    }
                }
            }

            char PeekChar
            {
                get
                {
                    return Convert.ToChar(json.Peek());
                }
            }

            char NextChar
            {
                get
                {
                    return Convert.ToChar(json.Read());
                }
            }

            string NextWord
            {
                get
                {
                    StringBuilder word = new StringBuilder();

                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);

                        if (json.Peek() == -1)
                        {
                            break;
                        }
                    }

                    return word.ToString();
                }
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();

                    if (json.Peek() == -1)
                    {
                        return TOKEN.NONE;
                    }

                    switch (PeekChar)
                    {
                        case '{':
                            return TOKEN.CURLY_OPEN;
                        case '}':
                            json.Read();
                            return TOKEN.CURLY_CLOSE;
                        case '[':
                            return TOKEN.SQUARED_OPEN;
                        case ']':
                            json.Read();
                            return TOKEN.SQUARED_CLOSE;
                        case ',':
                            json.Read();
                            return TOKEN.COMMA;
                        case '"':
                            return TOKEN.STRING;
                        case ':':
                            return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return TOKEN.NUMBER;
                    }

                    switch (NextWord)
                    {
                        case "false":
                            return TOKEN.FALSE;
                        case "true":
                            return TOKEN.TRUE;
                        case "null":
                            return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }
        }

        sealed class ObjectParser
        {
            public static object DeserializeIntoType(Type objectType, object map)
            {
                if (map == null)
                {
                    return null;
                }

                if (map.GetType() == typeof(object))
                {
                    return map;
                }

                if (objectType.Equals(typeof(bool)))
                {
                    try
                    {
                        return Convert.ToBoolean(map);
                    }
                    catch
                    {
                        return map != null;
                    }
                }

                if (IsConvertibleToString(objectType))
                {
                    return Convert.ChangeType(map, objectType);
                }

                if (objectType.Equals(typeof(string)))
                {
                    return map;
                }

                if (typeof(IDictionary).IsAssignableFrom(objectType))
                {
                    return DeserializeDictionaryIntoType(objectType, map as IDictionary);
                }

                if (objectType.IsArray || objectType.Equals(typeof(Array)))
                {
                    return DeserializeListIntoArray(objectType, map as IList);
                }

                if (typeof(IList).IsAssignableFrom(objectType))
                {
                    return DeserializeListIntoType(objectType, map as IList);
                }

                //object is not list, dictionary, nor directly convertible to string
                return DeserializeIntoType(objectType, map as IDictionary<object, object>);
            }

            static IDictionary DeserializeDictionaryIntoType(Type dictionaryType, IDictionary dictionary)
            {
                Type[] arguments = dictionaryType.GetGenericArguments();
                Type keyType = arguments[0];
                Type valueType = arguments[1];

                IDictionary serializedDict = (IDictionary)Activator.CreateInstance(dictionaryType);
                foreach (DictionaryEntry dictEntry in dictionary)
                {
                    serializedDict.Add(DeserializeIntoType(keyType, dictEntry.Key), DeserializeIntoType(valueType, dictEntry.Value));
                }

                return serializedDict;
            }

            static Array DeserializeListIntoArray(Type listType, IList list)
            {
                Type elementType = listType.GetElementType();

                Array array;
                if (listType.GetArrayRank() > 1)
                {
                    List<int> indices = SerializationUtils.GetDimensionsOfList(list);
                    array = Array.CreateInstance(elementType, indices.ToArray());
                    IList flattenedList;
                    SerializationUtils.FlattenList(list, elementType, out flattenedList);

                    List<int> indexPath = SerializationUtils.CreateIndexPathForArray(array);

                    for (int i = 0; i < flattenedList.Count; i++)
                    {
                        object value = DeserializeIntoType(elementType, flattenedList[i]);
                        array.SetValue(value, indexPath.ToArray());
                        SerializationUtils.IncrementIndexPath(ref indexPath, indices);
                    }
                }
                else
                {
                    try
                    {
                        array = Array.CreateInstance(elementType, list.Count);
                    }
                    catch
                    {
                        array = Array.CreateInstance(elementType, list.Count);
                    }
                    for (int i = 0; i < list.Count; i++)
                    {
                        object value = DeserializeIntoType(elementType, list[i]);
                        array.SetValue(value, i);
                    }
                }

                return array;
            }

            static IList DeserializeListIntoType(Type listType, IList list)
            {
                IList serializedList = (IList)Activator.CreateInstance(listType);
                Type[] arguments = listType.GetGenericArguments();
                Type elementType = arguments[0];

                foreach (object listObj in list)
                {
                    serializedList.Add(DeserializeIntoType(elementType, listObj));
                }

                return serializedList;
            }

            static object DeserializeIntoType(Type type, IDictionary<object, object> dict)
            {
                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                FieldInfo[] fields = type.GetFields(bindingFlags);

                object obj = FormatterServices.GetUninitializedObject(type);

                foreach (FieldInfo field in fields)
                {
                    string fieldName = SerializationUtils.SerilizedFieldName(field);

                    if (dict == null || string.IsNullOrEmpty(fieldName) || !dict.ContainsKey(fieldName))
                    {
                        continue;
                    }

                    if (field.IsPrivate)
                    {
                        var attr = Attribute.GetCustomAttribute(field, typeof(SerializeField));
                        if (attr == null)
                        {
                            continue;
                        }
                    }

                    if (field.FieldType == typeof(object))
                    {
                        field.SetValue(obj, dict[fieldName]);
                    }
                    else
                    {
                        field.SetValue(obj, DeserializeIntoType(field.FieldType, dict[fieldName]));
                    }
                }

                return obj;
            }
        }

        /// <summary>
        /// Converts an object into a JSON string.
        /// </summary>
        /// <param name="obj">An object to be parsed.</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable.</returns>
        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        sealed class Serializer
        {
            StringBuilder builder;

            Serializer()
            {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                if (obj == null)
                {
                    return null;
                }
                //if the object is a string, just return it
                if (obj.GetType().Equals(typeof(string)))
                {
                    return (string)obj;
                }

                var instance = new Serializer();

                instance.SerializeValue(obj);

                return instance.builder.ToString();
            }

            void SerializeValue(object value)
            {
                IList asList;
                IDictionary asDict;
                string asStr;

                if (value == null)
                {
                    builder.Append("null");
                }
                else if ((asStr = value as string) != null)
                {
                    SerializeAsString(asStr);
                }
                else if (value is bool)
                {
                    builder.Append((bool)value ? 1 : 0);
                }
                else if ((asList = value as IList) != null)
                {
                    SerializeArray(asList);
                }
                else if ((asDict = value as IDictionary) != null)
                {
                    SerializeDictionary(asDict);
                }
                else if (IsConvertibleToString(value.GetType()))
                {
                    SerializePrimitive(value);
                }
                else if (value.GetType().Equals(typeof(string)) || value.GetType().Equals(typeof(char)))
                {
                    SerializeAsString(value);
                }
                else
                {
                    SerializeObject(value);
                }
            }

            void SerializeDictionary(IDictionary dictionary)
            {
                bool first = true;

                builder.Append('{');

                foreach (object e in dictionary.Keys)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    SerializeValue(e);
                    builder.Append(':');

                    SerializeValue(dictionary[e]);

                    first = false;
                }

                builder.Append('}');
            }


            void SerializeMultipleDimensionArray(IList array)
            {
                Array multiDimensionalArray = (Array)array;
                IList list = SerializationUtils.ConvertMultiDimensionalArrayIntoList(multiDimensionalArray);
                SerializeArray(list);
            }

            void SerializeArray(IList array)
            {
                //A multiple dimension array is a special case. Lets handle it differently
                if (array.GetType().IsArray && array.GetType().GetArrayRank() >= 2)
                {
                    SerializeMultipleDimensionArray(array);
                    return;
                }

                builder.Append('[');

                bool first = true;

                foreach (object obj in array)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    SerializeValue(obj);

                    first = false;
                }

                builder.Append(']');
            }

            void SerializeObject(object value)
            {
                if (value == null)
                {
                    return;
                }

                builder.Append('{');
                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                FieldInfo[] fields = value.GetType().GetFields(bindingFlags);
                bool first = true;

                foreach (FieldInfo field in fields)
                {
                    if (field.IsPrivate)
                    {
                        var serializeFieldAttr = Attribute.GetCustomAttribute(field, typeof(SerializeField));
                        if (serializeFieldAttr == null)
                        {
                            continue;
                        }
                    }
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    string fieldName = SerializationUtils.SerilizedFieldName(field);

                    first = false;
                    builder.Append(string.Format("\"{0}\":", fieldName));
                    SerializeValue(field.GetValue(value));
                }

                builder.Append('}');
            }

            void SerializePrimitive(object value)
            {
                builder.Append(value.ToString().Replace(',', '.'));
            }

            void SerializeAsString(object value)
            {
                builder.Append('\"');

                builder.Append(value.ToString());

                builder.Append('\"');
            }
        }

        static bool IsConvertibleToString(Type value)
        {
            return value == typeof(int)
            || value == typeof(uint)
            || value == typeof(long)
            || value == typeof(sbyte)
            || value == typeof(byte)
            || value == typeof(short)
            || value == typeof(ushort)
            || value == typeof(ulong)
            || value == typeof(float)
            || value == typeof(double)
            || value == typeof(decimal);
        }
    }
}

