using Didimo.Networking;
using Didimo.Utils.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Utils.Serialization.Editor
{
    public class MiniJSONTest
    {
        public class DataObject1 : DataObject
        {
            public string DontSerialize { get { return dontSerialize; } }
            public string SerializeThis { get { return serializeThis; } }
            public int[] ints;

            private string dontSerialize;
            [SerializeField]
            private string serializeThis;
            [JsonName("customSerializationName")]
            public string str;
            public Dictionary<string, int> dict1;
            public Dictionary<DataObject2, float> dict2;
            public Dictionary<Dictionary<string, int>, string> dict3;

            public void Init()
            {
                dontSerialize = "Hello1";
                serializeThis = "Private is still serialized";
                str = "str";

                dict1 = new Dictionary<string, int> { { "hello2", 1 }, { "world2", 2 } };

                dict2 = new Dictionary<DataObject2, float>();
                dict2.Add(new DataObject2(0.4f), 1f);
                dict2.Add(new DataObject2(1.2f), 24f);

                dict3 = new Dictionary<Dictionary<string, int>, string>();
                dict3.Add(new Dictionary<string, int> { { "hello3", 1 }, { "world3", 2 } }, "Greetings3");
                dict3.Add(new Dictionary<string, int> { { "Fox4", 3 }, { "Dog4", 4 } }, "Over4");
                dict3.Add(new Dictionary<string, int> { { "Quick5", 5 }, { "Lazy5", 6 } }, "Jump5");

                ints = new int[] { 1, 2, 3 };
            }
        }

        public class DataObject2 : DataObject
        {
            public float theFloat;
            public decimal theDecimal;
            public double theDouble;
            public int theInt;
            public uint theUint;

            public DataObject2(float f)
            {
                theFloat = f;
                theDecimal = Convert.ToDecimal(f);
                theDouble = Convert.ToDouble(f);
                theInt = Convert.ToInt32(f);
                theUint = Convert.ToUInt32(f);
            }
        }

        [Test]
        public void ListTest()
        {
            List<int> testList = new List<int> { 1, 2, 3 };
            string listJson = "[1,2,3]";
            Assert.AreEqual(listJson, MiniJSON.Serialize(testList), "Should serialize lists.");
            Assert.AreEqual(testList, MiniJSON.Deserialize<List<int>>(listJson), "Should deserialzie lists.");

            List<List<int>> jaggedList = new List<List<int>>();
            jaggedList.Add(new List<int> { 1, 2, 3 });
            jaggedList.Add(new List<int> { 4, 5, 6 });
            string jaggedListJson = "[[1,2,3],[4,5,6]]";

            Assert.AreEqual(jaggedListJson, MiniJSON.Serialize(jaggedList), "Should serialize jagged lists.");
            Assert.AreEqual(jaggedList, MiniJSON.Deserialize<List<List<int>>>(jaggedListJson), "Should deserialzie jagged lists.");
        }

        [Test]
        public void ArrayTest()
        {
            string[] testArray = new string[] { "a\\rray", "one", "two", "three" };
            string arrayJson = "[\"a\\rray\",\"one\",\"two\",\"three\"]";
            Assert.AreEqual(arrayJson, MiniJSON.Serialize(testArray), "Should serialize arrays.");
            Assert.AreEqual(testArray, MiniJSON.Deserialize<string[]>(arrayJson), "Should deserialzie arrays.");

            int[][] jaggedArray = new int[][] {
            new int[] {1,3,5,7,9},
            new int[] {0,2,4,6},
            new int[] {11,22}
            };
            string jaggedArrayJson = "[[1,3,5,7,9],[0,2,4,6],[11,22]]";
            Assert.AreEqual(jaggedArrayJson, MiniJSON.Serialize(jaggedArray), "Should serialize jagged dimensional arrays.");
            Assert.AreEqual(jaggedArray, MiniJSON.Deserialize<int[][]>(jaggedArrayJson), "Should deserialzie jagged dimensional arrays.");

            int[][][] jaggedArray2 = new int[][][] {
                new int[][] {
            new int[] {1,3,5,7,9},
            new int[] {0,2,4,6},
            new int[] {11,22}
                }
            };
            string jaggedArrayJson2 = "[[[1,3,5,7,9],[0,2,4,6],[11,22]]]";
            Assert.AreEqual(jaggedArrayJson2, MiniJSON.Serialize(jaggedArray2), "Should serialize jagged dimensional arrays.");
            Assert.AreEqual(jaggedArray2, MiniJSON.Deserialize<int[][][]>(jaggedArrayJson2), "Should deserialzie jagged dimensional arrays.");

            int[,] multiDimensionalArray = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            string multiDimensionalArrayJson = "[[1,2,3],[4,5,6]]";

            Assert.AreEqual(multiDimensionalArrayJson, MiniJSON.Serialize(multiDimensionalArray), "Should serialize multi-dimensional arrays.");
            Assert.AreEqual(multiDimensionalArray, MiniJSON.Deserialize<int[,]>(multiDimensionalArrayJson), "Should deserialzie multi-dimensional arrays.");

            int[,,] multiDimensionalArray2 = new int[,,] { { { 1, 2, 3 }, { 4, 5, 6 } } };
            string multiDimensionalArrayJson2 = "[[[1,2,3],[4,5,6]]]";

            Assert.AreEqual(multiDimensionalArrayJson2, MiniJSON.Serialize(multiDimensionalArray2), "Should serialize multi-dimensional arrays.");
            Assert.AreEqual(multiDimensionalArray2, MiniJSON.Deserialize<int[,,]>(multiDimensionalArrayJson2), "Should deserialzie multi-dimensional arrays.");
        }

        [Test]
        public void DictionaryTest()
        {
            Dictionary<string, int> testDictionary = new Dictionary<string, int>() { { "one", 1 }, { "two", 2 }, { "three", 3 } };
            string dictionaryJson = "{\"one\":1,\"two\":2,\"three\":3}";
            Assert.AreEqual(dictionaryJson, MiniJSON.Serialize(testDictionary), "Should serialize dictionaries.");
            Assert.AreEqual(testDictionary, MiniJSON.Deserialize<Dictionary<string, int>>(dictionaryJson), "Should deserialzie dictionaries.");
        }

        [Test]
        public void StringTest()
        {
            string json = "\\backslash";

            Assert.AreEqual(json, MiniJSON.Serialize(json), "Should serialize strings.");
            Assert.AreEqual(json, MiniJSON.Deserialize<string>(json), "Should deserialize strings.");

            json = "a\nb";
            Assert.AreEqual(json, MiniJSON.Serialize(json), "Should serialize strings with \n.");
            Assert.AreEqual(json, MiniJSON.Deserialize<string>(json), "Should deserialize strings with \n.");
        }

        [Test]
        public void IntTest()
        {
            int i = 23;
            string json = "23";

            Assert.AreEqual(json, MiniJSON.Serialize(i), "Should serialize ints.");
            Assert.AreEqual(i, MiniJSON.Deserialize<int>(json), "Should deserialize ints.");
        }

        [Test]
        public void OtherObjectsTest()
        {
            DataObject1 obj1 = new DataObject1();
            obj1.Init();
            string s = MiniJSON.Serialize(obj1);

            Assert.IsFalse(s.Contains("dontSerialize"), "Private fields shouldn't be serialized.");
            Assert.IsTrue(s.Contains("serializeThis"), "Private fields with the SerializeField tag should be serialized.");
            Assert.IsTrue(s.Contains("customSerializationName"), "Should serialize with the right JSON names");

            string expectedString = "{\"ints\":[1,2,3],\"serializeThis\":\"Private is still serialized\",\"customSerializationName\":\"str\",\"dict1\":{\"hello2\":1,\"world2\":2},\"dict2\":{{\"theFloat\":0.4,\"theDecimal\":0.4,\"theDouble\":0.400000005960464,\"theInt\":0,\"theUint\":0}:1,{\"theFloat\":1.2,\"theDecimal\":1.2,\"theDouble\":1.20000004768372,\"theInt\":1,\"theUint\":1}:24},\"dict3\":{{\"hello3\":1,\"world3\":2}:\"Greetings3\",{\"Fox4\":3,\"Dog4\":4}:\"Over4\",{\"Quick5\":5,\"Lazy5\":6}:\"Jump5\"}}";

            Assert.AreEqual(expectedString, s, "Should serialize other objects neatly.");

            DataObject1 deserialized = MiniJSON.Deserialize<DataObject1>(s);

            Assert.IsNull(deserialized.DontSerialize, "Private fields shouldn't be deserialized.");
            Assert.AreEqual(obj1.SerializeThis, deserialized.SerializeThis, "Private fields with the SerializeField tag should be deserialized.");
            Assert.AreEqual(s, MiniJSON.Serialize(deserialized), "Should serialize a deserialized object neatly.");
        }
    }
}