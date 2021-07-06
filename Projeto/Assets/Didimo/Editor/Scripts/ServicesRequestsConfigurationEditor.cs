using System.IO;
using UnityEditor;
using UnityEngine;

namespace Didimo.Networking
{
    public class ServicesRequestsConfigurationEditor
    {

        [MenuItem("Window/Didimo/Networking/Services Requests Configuration")]
        static void SelectConfig()
        {
            ServicesRequestsConfiguration defaultConfig = Resources.Load<ServicesRequestsConfiguration>(ServicesRequestsConfiguration.DefaultConfigFileFromResources);
            if (defaultConfig == null)
            {
                if (EditorUtility.DisplayDialog("Config not found", "Default services requests config not found. Create a new config at 'Assets/Resources' ?", "Yes", "Cancel"))
                {
                    var folder = Directory.CreateDirectory("Assets/Resources");
                    defaultConfig = ServicesRequestsConfiguration.CreateInstance<ServicesRequestsConfiguration>();
                    AssetDatabase.CreateAsset(defaultConfig, "Assets/Resources/" + ServicesRequestsConfiguration.DefaultConfigFileFromResources + "." + ServicesRequestsConfiguration.kDefaultConfigFileExtension);
                }
            }

            if (defaultConfig != null)
            {
                Selection.activeObject = defaultConfig;
            }
        }

    }
}