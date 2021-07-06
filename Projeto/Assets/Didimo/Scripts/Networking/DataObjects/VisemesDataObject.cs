using Didimo.Utils.Serialization;

namespace Didimo.Networking.DataObjects
{
    public class Viseme
    {
        public long time;
        [JsonName("type")]
        public string typeStr;
        public long start;
        public long end;
        public string value;


        public enum VisemeType
        {
            UNKNOWN, // Default value, in case we fail to convert it from string
            SENTENCE,
            WORD,
            VISEME
        }

        public float timeInSeconds
        {
            get
            {
                return (float)time / 1000f;
            }
        }

        public VisemeType type
        {
            get
            {
                // Unfortunatelly there isn't a case-insensitive way of trying to parse without a try-catch block
                try
                {
                    return (VisemeType)System.Enum.Parse(typeof(VisemeType), typeStr, true);
                }
                catch
                {
                    return VisemeType.UNKNOWN;
                }
            }
        }
    }
}