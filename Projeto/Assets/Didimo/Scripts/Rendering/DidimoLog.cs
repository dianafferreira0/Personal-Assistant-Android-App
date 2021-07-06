
using UnityEngine;

namespace Didimo.Rendering
{
    public class Log
    {
        static public void info(string s, params object[] list)
        {
            Debug.Log(string.Format(s, list));
        }

        static public void warning(string s, params object[] list)
        {
            Debug.LogWarning(string.Format(s, list));
        }

        static public void error(string s, params object[] list)
        {
            Debug.LogError(string.Format(s, list));
        }
    }
}
