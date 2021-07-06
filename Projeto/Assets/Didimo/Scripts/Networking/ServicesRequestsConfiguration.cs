using Didimo.Networking.DataObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Networking
{

    [CreateAssetMenu(fileName = "servicesRequestsConfig", menuName = "Didimo/ServicesRequestsConfiguration")]
    public partial class ServicesRequestsConfiguration : ServicesRequestsConfigurationBase
    {
    }

    public partial class ServicesRequestsConfigurationBase : ScriptableObject
    {
        private string overrideDidimoPlatform = "";

        public void OverrideDidimoPlatform(string platformName)
        {
            overrideDidimoPlatform = platformName;
        }

        private Dictionary<string, string> _additionalHeaders = new Dictionary<string, string>();

        public void SetAdditionalHeader(string key, string value)
        {
            _additionalHeaders[key] = value;
        }

        public Dictionary<string, string> AdditionalHeaders
        {
            get
            {
                Dictionary<string, string> headers = new Dictionary<string, string>(_additionalHeaders);
                headers["Didimo-Platform"] = GetDidimoPlatform();

                return headers;
            }
        }

        public string GetDidimoPlatform()
        {
            if (!string.IsNullOrEmpty(overrideDidimoPlatform))
            {
                return overrideDidimoPlatform;
            }
            else
            {
                if (Application.isPlaying)
                {
                    return "UnitySDK";
                }
                else
                {
                    return "UnityPlugin";
                }
            }
        }

        protected static string kDefaultConfigFileFromResources = "servicesRequestsConfig";

        public static string DefaultConfigFileFromResources
        {
            get
            {
                return kDefaultConfigFileFromResources;
            }
        }

        public const string kDefaultConfigFileExtension = "asset";

        public string CustomerPortalUrl;
        public string baseDidimoUrl;
        public string apiVersion;
        public string template;

        protected static ServicesRequestsConfiguration _defaultConfig;

        public static ServicesRequestsConfiguration DefaultConfig
        {
            get
            {
                if (_defaultConfig == null)
                {
                    _defaultConfig = Resources.Load<ServicesRequestsConfiguration>(kDefaultConfigFileFromResources);
                    if (_defaultConfig == null)
                    {
                        Debug.LogError("Missing services requests config file. Please create a ServicesRequestsConfiguration asset at 'Resources/servicesRequestsConfig.asset'.");
                    }
                }
                return _defaultConfig;
            }
        }

        public static UserProfileDataObject CurrentUserProfileDataObject { get; set; }

        //User Account
        public string apiUrl { get { return baseDidimoUrl + "/"; } } //public string apiUrl { get { return baseDidimoUrl + "/api/" + apiVersion + "/"; } }
        public string loginUrl { get { return apiUrl + "login"; } }
        public string profileLoginUrl { get { return apiUrl + "profile/login"; } }
        public string logoutUrl { get { return apiUrl + "logout"; } }
        //public string regissterUrl { get { return apiUrl + "register"; } }
        //public string recoverPasswordUrl { get { return apiUrl + "recoverpassword"; } }
        public string profileUrl { get { return apiUrl + "profile"; } }
        //Didimo Creation
        public string simpleDownloadUrl { get { return apiUrl + "didimo/{0}/download"; } }
        public string baseFileUrl { get { return simpleDownloadUrl + "/{1}"; } }
        public string statusUrl { get { return apiUrl + "didimo/{0}/status"; } }
        private string uploadPhotoUrl { get { return apiUrl + "didimo/new/photo/maya,unity/{0}/{1}"; } } ///v2/didimo/new/:input_type/:output_type/:template/(:features)
        private string uploadLoFiMeshUrl { get { return apiUrl + "didimo/new/lofimesh_texture/maya,unity/{0}/{1}"; } }
        private string uploadHiFiMeshUrl { get { return apiUrl + "didimo/new/hifimesh_texture_photo/maya,unity/{0}/{1}"; } }
        public string didimoPreviewUrl { get { return apiUrl + "didimo/{0}/preview/{1}"; } }
        public string deleteDidimoUrl { get { return apiUrl + "didimo/{0}/delete"; } }
        //public string buyDidimoUrl { get { return apiUrl + "buy/{0}"; } }
        public string setDidimoPropertiesUrl { get { return apiUrl + "didimo/{0}/meta/set"; } }
        //Didimo preview
        const string thumbnailPreviewFile = "thumb";
        const string frontPreviewFile = "front";
        const string sidePreviewFile = "side";
        const string cornerPreviewFile = "corner";
        //Didimo Files
        const string modelThreeJSFile = "avatar_model.js";
        const string modelFbxFile = "fbx";
        const string skinTextureFile = "model_retopology.jpg";
        const string normalMapFile = "model_retopology_normalmap.png";
        const string irisTextureFile = "coloured_iris.jpg";
        //Hair
        public string deformAssetUrl { get { return apiUrl + "didimo/{0}/execute/vertexdeform"; } } //"didimo/{0}/assetdeform"
        const string tpsMatrix = "tpsmatrix.cereal.bin";
        //Misc
        public string SendFeedbackUrl { get { return apiUrl + "feedback/new"; } }
        //public string didimoForDevelopersUrl { get { return baseDidimoUrl + "/developers"; } }
        public string DidimoZipUrlForDidimoCode(string didimoCode)
        {
            return string.Format(baseFileUrl, didimoCode, "unityzip");
        }
        public string DidimoZipUrlForDidimoCode(string typeOfZip, string didimoCode)
        {
            if (typeOfZip.Equals("unity"))
                return string.Format(baseFileUrl, didimoCode, "unity");
            else return string.Format(baseFileUrl, didimoCode, "maya");
        }
        public string DeformedAssetUrlForKey(string key)
        {
            return string.Format(simpleDownloadUrl, key);
        }

        public string ModelTextureUrlForDidimoCode(string didimoCode, string textureName)
        {
            return string.Format(baseFileUrl, didimoCode, textureName);
        }

        public string ModelJsonUrlForDidimoCode(string didimoCode)
        {
            return string.Format(baseFileUrl, didimoCode, modelThreeJSFile);
        }

        public string FrontPreviewUrlForDidimoCode(string didimoCode)
        {
            return string.Format(didimoPreviewUrl, didimoCode, frontPreviewFile);
        }

        public string SkinUrlForDidimoCode(string didimoCode)
        {
            return string.Format(baseFileUrl, didimoCode, skinTextureFile);
        }

        public string NormalMapUrlForDidimoCode(string didimoCode)
        {

            return string.Format(baseFileUrl, didimoCode, normalMapFile);
        }

        public string ModelFbxUrlForDidimoCode(string didimoCode)
        {

            return string.Format(baseFileUrl, didimoCode, modelFbxFile);
        }

        public string StatusUrlForDidimoCode(string didimoCode)
        {

            return string.Format(statusUrl, didimoCode);
        }

        public string PhotoUrlForDidimoCode(string didimoCode)
        {

            return string.Format(didimoPreviewUrl, didimoCode, thumbnailPreviewFile);
        }

        public string IrisUrlForDidimoCode(string didimoCode)
        {

            return string.Format(didimoPreviewUrl, didimoCode, irisTextureFile);
        }

        public string TpsUrlForDidimoCode(string didimoCode)
        {
            return string.Format(baseFileUrl, didimoCode, tpsMatrix);
        }

        public string DeleteDidimoForDidimoCode(string didimoCode)
        {

            return string.Format(deleteDidimoUrl, didimoCode);
        }

        public string SetPropertiesUrlForDidimoCode(string didimoCode)
        {
            return string.Format(setDidimoPropertiesUrl, didimoCode);
        }

        public string DeformUrlForDidimoCode(string didimoCode, string hairId)
        {
            return string.Format(deformAssetUrl, didimoCode);
        }

        /* Add available features in the current tier level, so that applications can choose the "highest" ones available (basic, visemes, expressions, assetdeform, blendshapes)   "2.0
        ----
        TRIAL: basic
        INDIE: basic, visemes
        PRO: expressions, visemes
        */
        public string GetFeaturesFromTierLevel()
        {
            string tierId = CurrentUserProfileDataObject.tier_name;
            string features = "";
            if (tierId.Equals("mobile-showcase-trial") || tierId.Equals("pro") || tierId.Equals("advanced"))
                features += "expressions";
            else if (tierId.Equals("basic"))
                features += "basic";
            else if (tierId.Equals("trial"))
                features += "basic";

            if (tierId.Equals("mobile-showcase-trial") || tierId.Equals("pro") || tierId.Equals("advanced"))
                features += ",visemes";
            else if (tierId.Equals("basic"))
                features += ",visemes";

            return features;
        }

        public string GetUploadPhotoUrl()
        {
            return string.Format(uploadPhotoUrl, template.ToString(), GetFeaturesFromTierLevel());
        }
        public string GetUploadLoFiMeshUrl()
        {
            return string.Format(uploadLoFiMeshUrl, template.ToString(), GetFeaturesFromTierLevel());
        }
        public string GetUploadHiFiMeshUrl()
        {
            return string.Format(uploadHiFiMeshUrl, template.ToString(), GetFeaturesFromTierLevel());
        }

    }
}
