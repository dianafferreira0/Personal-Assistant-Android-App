using Didimo.Utils.Serialization;

namespace Didimo.Networking.DataObjects
{
    [System.Serializable]
    public class UserProfileDataObject : BaseResponseDataObject
    {
        public float points;
        [JsonName("tier_code")]
        public string tier_name; //this the "id
        [JsonName("tier_label")]
        public string tier_description;
        public string next_expiration_date;
        private float? next_expiration_points = 0; //FIX: accepts null values

        public float GetNextExpirationPoints()
        {
            if (next_expiration_points == null)
                return 0;
            else return (float)next_expiration_points;

        }

    }
}