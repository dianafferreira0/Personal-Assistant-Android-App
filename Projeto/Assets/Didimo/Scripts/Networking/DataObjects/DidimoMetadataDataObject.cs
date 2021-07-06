using Didimo.Utils.Serialization;

namespace Didimo.Networking.DataObjects
{
    public class DidimoMetadataDataObject : BaseResponseDataObject
    {
        [JsonName("name")]
        public string name;
        [JsonName("value")]
        public string value;
    }
}
