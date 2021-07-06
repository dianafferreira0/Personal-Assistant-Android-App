using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Didimo.Editor.Utils.Coroutines
{
    /// <summary>
    /// Used to Start a 'coroutine' that will work the editor. Please note that this is a simple approach and will not work for things that rely on a timer like 'WaitForSeconds'. If you need that functionality, use EditorWaitForSeconds instead.
    /// </summary>
    public class EditorYieldInstruction : CustomYieldInstruction
    {
        public override bool keepWaiting
        {
            get { return running; }
        }

        /// <summary>
        /// Starts a coroutine.
        /// </summary>
        /// <param name="routine">The routine to start.</param>
        /// <returns>An instance of <see cref="EditorYieldInstruction"/> which will run the routine.</returns>
        public static EditorYieldInstruction Start(IEnumerator routine)
        {
            EditorYieldInstruction coroutine = new EditorYieldInstruction(routine);
            coroutine.Start();
            return coroutine;
        }

        /// <summary>
        /// Stop the routine.
        /// </summary>
        public void Stop()
        {
            EditorApplication.update -= Update;
            running = false;
        }

        private bool running;
        readonly IEnumerator routine;

        EditorYieldInstruction(IEnumerator routine)
        {
            this.routine = routine;
        }

        void Start()
        {
            EditorApplication.update += Update;
            running = true;
        }

        void Update()
        {
            if (routine.Current != null)
            {
                if (routine.Current.GetType().IsSubclassOf(typeof(CustomYieldInstruction)))
                {
                    CustomYieldInstruction yieldInstruction = (CustomYieldInstruction)routine.Current;

                    if (yieldInstruction.keepWaiting)
                    {
                        return;
                    }
                }
                else if (routine.Current.GetType().Equals(typeof(UnityWebRequest)))
                {
                    UnityWebRequest yieldInstruction = (UnityWebRequest)routine.Current;
                    if (!yieldInstruction.isDone)
                    {
                        return;
                    }
                }
                /*else if (routine.Current.GetType().Equals(typeof(WWW))) // obsolete
                {
                    WWW yieldInstruction = (WWW)routine.Current;
                    if (!yieldInstruction.isDone)
                    {
                        return;
                    }
                }*/
                else if (typeof(AsyncOperation).IsAssignableFrom(routine.Current.GetType()))
                {
                    AsyncOperation asyncOp = (AsyncOperation)routine.Current;
                    if (!asyncOp.isDone)
                    {
                        return;
                    }
                }
                else if (!typeof(IEnumerator).IsAssignableFrom(routine.Current.GetType()))
                {
                    Debug.LogWarning("Unsupported IENumerator for EditorYieldInstruction: " + routine.Current.GetType());
                }
            }

            if (!running)
            {
                Debug.LogWarning("Routine is still running but shouldn't. Possible bug.");
            }

            if (!routine.MoveNext())
            {
                Stop();
            }
        }
    }
}