
using Didimo.Utils.Serialization;
using System.Collections.Generic;

namespace Didimo.Networking.DataObjects
{
    public class PaginatedDidimoListDataObject : BaseResponseDataObject
    {
        [JsonName("total_list_size")]
        public int totalSize;
        public string previous;
        [JsonName("next")]
        public string nextCursor;
        [JsonName("update")]
        public string updateCursor;
        public List<DidimoAvatarDataObject> models;
        public List<DidimoAvatarDataObject> updated;
        public List<string> removed;

        public float ttl;


    }
}