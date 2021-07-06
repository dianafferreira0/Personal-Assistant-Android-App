using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils.Serialization;
using System;
using System.Collections.Generic;


namespace Didimo.Networking.DataObjects
{

    [Serializable]
    public class ThreeJsModelDataObject : BaseResponseDataObject
    {
        [Flags]
        public enum TypeBitmask
        {
            Triangle = 0,
            Quad = 1,
            Face_Material = 1 << 1,
            Face_UV = 1 << 2,
            Face_Vertex_UV = 1 << 3,
            Face_Normal = 1 << 4,
            Face_Vertex_Normal = 1 << 5,
            Face_Color = 1 << 6,
            Face_Vertex_Color = 1 << 7
        }

        public Metadata metadata;

        public Material[] materials;

        public int influencesPerVertex;

        public List<int> skinIndices;
        public List<float> skinWeights;

        public float[] vertices;

        public float[] normals;

        public int[] Triangles;

        public float[] colors;

        public float[][] uvs;

        public int[] faces;

        public Bone[] bones;

        [Serializable]
        public class Metadata : DataObject
        {
            public int formatVersion;
        }

        [Serializable]
        public class Material : DataObject
        {
            [JsonName("DbgColor")]
            public int dbgColor;

            [JsonName("DbgIndex")]
            public int dbgIndex;

            [JsonName("DbgName")]
            public string dbgName;

            /// <summary>
            /// The diffuse color, in RGB
            /// </summary>
            public int[] colorDiffuse;
        }

        [Serializable]
        public class Bone : DataObject
        {
            [JsonName("scl")]
            public float[] scale;

            public string name;

            [JsonName("parent")]
            public int parentIndex;

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