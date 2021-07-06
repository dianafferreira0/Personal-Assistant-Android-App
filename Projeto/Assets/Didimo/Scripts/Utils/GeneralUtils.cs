
using UnityEngine;

namespace Didimo.Utils
{

    public class GeneralUtils
    {
        

        public static bool InternetStatus()
        {
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}