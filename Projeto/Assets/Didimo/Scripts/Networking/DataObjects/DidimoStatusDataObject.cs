using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Didimo.Networking.DataObjects
{
    [System.Serializable]
    public class DidimoStatusDataObject : BaseResponseDataObject
    {
        //Didimo Object Definition
        public string key; //Didimo Key
        public bool is_public; //Is this didimo a sample
        public int error_code; //Internal error code for support purposes
        public string template_version; //The template chosen when the didimo was generated
        public string optional_features; //The extra features chosen when created e.g. expressions, visemes
        public string created_by; //Didimo Key
        public int percent; //Integer value from 0 to 100 representing the percentage of execution
        public string type; //Type of resource uploaded e.g. photo
        public string created_at; //Date of submission
        public string updated_at; //Date of submission
        public string status; //Status can have the values: pending, processing, error, done
        public List<DidimoMetadataDataObject> meta; //List of meta objects containing user provided information and pipeline generated information!

        public bool isDone { get => status?.CompareTo("done") == 0; }
        public bool hasFailed { get => status?.CompareTo("error") == 0; }
        public bool hasUnityTarget
        {
            get => meta.Where(m =>
                       m.name.CompareTo("supports-export_unity") == 0 &&
                       m.value.Contains("export_unity"))
                   .ToList().Count > 0;
        }
    }
}
