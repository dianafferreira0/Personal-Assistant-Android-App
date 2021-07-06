using Didimo.Utils.Serialization;

namespace Didimo.Networking.DataObjects
{
    public class SpeechDataObject : BaseResponseDataObject
    {
    	[JsonName("audio_location")]
        public string audioURL;
        [JsonName("visemes_location")]
        public string marksURL;
    }
}