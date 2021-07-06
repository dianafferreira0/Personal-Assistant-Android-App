using System;
using System.Collections.Generic;

namespace Didimo.Networking.DataObjects
{
    [Serializable]
    public class DidimoModelDataObject2 : DidimoModelDataObject
    {
        public string realtime_rig;
        public Mesh2[] meshes;

        public override Mesh[] GetMeshes()
        {
            return meshes;
        }
        public Node2 root_node;
        // Names of the nodes that are skinned. The Meshe's skinning info (namely skin indices), will point into the indices of this array
        public List<string> bones;

        public MaterialState material_state;
        public Constraint[] constraints;

        [Serializable]
        public class Constraint
        {
            public string constrainedObj;
            public string constraintSrc;
            public string type;
        }

        [Serializable]
        public class Mesh2 : DidimoModelDataObject.Mesh
        {
            public List<List<int>> skin_indices;
            public List<List<float>> skin_weights;
        }

        [Serializable]
        public class Node2 : DidimoModelDataObject.Node
        {
            public Node2[] children;
        }
    }
}