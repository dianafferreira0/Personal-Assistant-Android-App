using Didimo.Networking;
using Didimo.Networking.DataObjects;
using System.Collections.Generic;

namespace Didimo.Animation.DataObjects
{
    public class RealtimeRigDataObject : DataObject
    {
        public string version;
        public float unitsPerMeter = DidimoModelDataObject.DEFAULT_JSON_UNITS_PER_METER;
        public Dictionary<string, BlendShapedMeshDataObject> blendshaped_meshes;
        public Dictionary<string, Dictionary<string, BoneOffsets>> poses;

        public class Blendshape : DataObject
        {
            public List<float> vertOffset;
            public List<float> normalOffset;
        }

        public class BoneOffsets : DataObject
        {
            public float[] pos;
            public float[] rotq;
        }

        public class BlendShapedMeshDataObject : DataObject
        {
            public Dictionary<string, Blendshape> blendshapes;
            public List<string> bone_driver_names;
            public List<int> driven_vertex_indices;
            public Dictionary<string, Dictionary<string, float>> blendshape_relation_drivers;
        }

    }
}