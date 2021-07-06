
using Didimo.Utils.Serialization;

namespace Didimo.Networking.DataObjects
{
    public class VertexDeformDataObject : BaseResponseDataObject
    {
        [JsonName("key")]
        public string deformedAssetKey;
    }
}