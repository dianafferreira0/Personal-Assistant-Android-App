
using Didimo.Utils.Serialization;

namespace Didimo.Networking.DataObjects
{
    public class CreateDidimoDataObject : BaseResponseDataObject
    {
        [JsonName("key")]
        public string didimoCode;
    }
}