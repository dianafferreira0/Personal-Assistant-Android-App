using Didimo.Utils.Serialization;
using System;

namespace Didimo.Networking.DataObjects
{
    [Serializable]
    public abstract class DidimoModelDataObject : BaseResponseDataObject
    {
        public static float DEFAULT_JSON_UNITS_PER_METER = 100;
    
        public float unitsPerMeter = DEFAULT_JSON_UNITS_PER_METER;

        public abstract Mesh[] GetMeshes();

        [Serializable]
        public class Mesh : DataObject
        {
            public string name;

            public float[] vertices;

            public float[] normals;

            public float[][] uvs;

            public int[] faces;
        }

   
        [Serializable]
        public class Node : DataObject
        {
            [JsonName("scl")]
            public float[] scale;

            public string name;

            /// <summary>
            /// Quaternion for the ratation
            /// </summary>
            [JsonName("rotq")]
            public float[] rotation;

            [JsonName("pos")]
            public float[] position;
        }
    }
}