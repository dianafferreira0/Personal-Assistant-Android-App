using Didimo.Utils.Serialization;
using System;
using System.Collections.Generic;

namespace Didimo.Networking.DataObjects
{
    [Serializable]
    public class DidimoModelDataObject1 : DidimoModelDataObject
    {

        public Mesh1[] meshes;

        public override Mesh[] GetMeshes()
        {
            return meshes;
        }

        [Serializable]
        public class Texture1 : DataObject
        {
            public string textureType;
            public string textureName;
            [JsonName("texture_alpha")]
            public string textureAlpha;
        }

        public Node1[] bones;

        [Serializable]
        public class Mesh1 : DidimoModelDataObject.Mesh
        {
            public int influencesPerVertex;

            public List<int> skinIndices;
            public List<float> skinWeights;

            public Texture1[] textures;
        }

        // In version 1 of the didimo model format, these actually represent bones, as they are only used for the skeleton hierarchy.
        [Serializable]
        public class Node1 : DidimoModelDataObject.Node
        {
            [JsonName("parent")]
            public int parentIndex;
        }
    }
}