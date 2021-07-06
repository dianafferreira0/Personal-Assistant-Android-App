using Didimo.Utils.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Networking.DataObjects
{
    [System.Serializable]
    public class DidimoAvatarDataObject : BaseResponseDataObject
    {
        [JsonName("is_public")]
        public bool isPublic;
        [JsonName("is_sample")]
        public bool isSample;
        public string key;
        [JsonName("meta")]
        public List<DidimoMetadataDataObject> metadata;
        public float percent;
        public bool done { get { return status.CompareTo("done") == 0; } }
        [JsonName("error_code")]
        public int errorCode;

        [SerializeField]
        [JsonName("type")]
        private string pipelineTypeValue;

        [JsonName("template_version")]
        public string templateVersion;

        [JsonName("optional_features")]
        public string optionalFeatures;
        [JsonName("created_by")]
        public string createdBy;

        [JsonName("created_at")]
        public string createdAt;
        [JsonName("updated_at")]
        public string updatedAt;
        public string status;

        /*
         *  Metadata keys: supports-expressions, supports-tts, supports-deformation, supports-rtr
         */

        //static methods
        public static bool IsCapabilitySupported(string capability, List<DidimoMetadataDataObject> metadata)
        {
            if (metadata != null)
            {
                foreach (DidimoMetadataDataObject metadata_item in metadata)
                {
                    if (metadata_item.name.CompareTo(capability) == 0)
                        return true;
                }
            }
            return false;
        }
        public static bool IsExpressionSupported(List<DidimoMetadataDataObject> metadata)
        {
            return IsCapabilitySupported("supports-expressions", metadata); 
        }
        public static bool IsBasicRigSupported(List<DidimoMetadataDataObject> metadata)
        {
            return IsCapabilitySupported("supports-basic", metadata);
        }
        public static bool IsTextToSpeechSupported(List<DidimoMetadataDataObject> metadata)
        {
            return IsCapabilitySupported("supports-visemes", metadata);
        }
        public static bool IsDeformationSupported(List<DidimoMetadataDataObject> metadata)
        {
            return IsCapabilitySupported("supports-deformation", metadata);
        }
        public static bool IsRealtimeRigSupported(List<DidimoMetadataDataObject> metadata)
        {
            return IsCapabilitySupported("supports-expressions", metadata);
        }
    }
}