using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Utils.Coroutines
{
    /// <summary>
    /// Custom version of wait for seconds, that will work in the editor and play mode.
    /// The usual WaitForSeconds yield instruction only works in play mode.
    /// </summary>
    public class EditorWaitForSeconds : CustomYieldInstruction
    {
        public double waitTime;
        public double timeSinceStartup;

        public override bool keepWaiting
        {
            get { return EditorApplication.timeSinceStartup < waitTime; }
        }

        public EditorWaitForSeconds(double time)
        {
            timeSinceStartup = EditorApplication.timeSinceStartup;
            waitTime = EditorApplication.timeSinceStartup + time;
        }
    }
}