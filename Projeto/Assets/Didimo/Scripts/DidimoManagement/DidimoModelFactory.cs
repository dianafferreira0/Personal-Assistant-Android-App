using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils.Serialization;
using UnityEngine;

namespace Didimo.DidimoManagement
{
    class DidimoModelVersion : DataObject
    {
        public int version = 0;
    }


    /// <summary>
    /// Factory for creating <see cref="DidimoModel"/> instances, used in <see cref="Didimo.DidimoManagement.DidimoLoader"/>.
    /// Currently supporting our json format, which started as an implementation of the ThreeJS model format, but has since diverged. Other formats could be added at a later time.
    /// </summary>
    public class DidimoModelFactory
    {
        /// <summary>
        /// Create a <see cref="DidimoModel"/>, given a source <see cref="DidimoModelDataObject"/>.
        /// Expects the mesh to be triangular.
        /// Will download required textures automatically.
        /// </summary>
        /// <param name="source">The <see cref="DidimoModel"/> object to convert into a <see cref="DidimoModel"/>.</param>
        /// <returns></returns>
        public static IDidimoModel CreateDidimoModel(string jsonText)
        {
            DidimoModelVersion version;
            try
            {
                // Some versions (older versions) have a string for the version number. We use an integer now. We can regard these versions as version 0
                version = MiniJSON.Deserialize<DidimoModelVersion>(jsonText);
            }
            catch (System.Exception)
            {
                version = new DidimoModelVersion();
                version.version = 0;
            }

            IDidimoModel result = null;

            switch (version.version)
            {
                // We're not supporting these versions (0 and 1) anymore
                case 0:
                case 1:
                    DidimoModelDataObject1 dataObject1 = MiniJSON.Deserialize<DidimoModelDataObject1>(jsonText);
                    DidimoModel1 model1 = new DidimoModel1(dataObject1);
                    result = model1;
                    break;
                case 2:
                    DidimoModelDataObject2 dataObject2 = MiniJSON.Deserialize<DidimoModelDataObject2>(jsonText);
                    DidimoModel2 model2 = new DidimoModel2(dataObject2);
                    result = model2;
                    break;
                default:
                    Debug.LogError("Unsuported DidimoModel version: " + version.version);
                    break;
            }

            return result;
        }

        public static bool IsVersionSupported(int version)
        {
            return version == 2;
        }
    }
}
