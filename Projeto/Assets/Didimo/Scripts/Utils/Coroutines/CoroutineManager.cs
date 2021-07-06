using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Utils.Coroutines
{
    /// <summary>
    /// An abstract class that will manage coroutines. It can start coroutines, stop a single coroutine, and stop all of the running coroutines.
    /// Can also yield for a given amount of seconds <see cref="WaitForSecondsRealtime(float)"/>.
    /// This abstract class will be implemented by suclasses that will specify how exactly the coroutines should be ran.
    /// </summary>
    public abstract class CoroutineManager : CustomYieldInstruction
    {
        private List<object> coroutines;
        private Dictionary<object, object> mainCoroutineToAutoDelete;
        private Dictionary<object, object> autoDeleteToMainCoroutine;

        public CoroutineManager()
        {
            coroutines = new List<object>();
            mainCoroutineToAutoDelete = new Dictionary<object, object>();
            autoDeleteToMainCoroutine = new Dictionary<object, object>();
        }

        /// Override for the abstract class CustomYieldInstruction
        public override bool keepWaiting
        {
            get
            {
                return coroutines.Count != 0;
            }
        }

        /// <summary>
        /// We need to keep track of which coroutines we started so that we can implement <see cref="StopAllCoroutines"/>.
        /// To free up resources (references to the started coroutines) when they finish, we yield until their work is done, and remove the resources then.
        /// </summary>
        /// <param name="coroutine">The main coroutine we will yield until finished.</param>
        /// <returns>A IEnumerator that can also be yielded.</returns>
        private IEnumerator AddCoroutineWithAutoDelete(object coroutine)
        {
            coroutines.Add(coroutine);

            //Yield until done, then free up resources.
            yield return coroutine;

            coroutines.Remove(coroutine);
            object autoDeleteCoroutine = mainCoroutineToAutoDelete[coroutine];
            mainCoroutineToAutoDelete.Remove(coroutine);
            autoDeleteToMainCoroutine.Remove(autoDeleteCoroutine);
            //UnityEngine.Debug.Log(string.Format("Removed. list: {0}, mainDict: {1}, inverseDict: {2}", coroutines.Count, mainCoroutineToAutoDelete.Count, autoDeleteToMainCoroutine.Count));
        }

        /// <summary>
        /// Starts a Coroutine.
        /// </summary>
        /// <param name="routine">The routine to start.</param>
        /// <returns>An object you can yield, and pass to <see cref="StopCoroutine(object)"/> to stop it.</returns>
        public object StartCoroutine(IEnumerator routine)
        {
            object coroutine = StartCoroutineImpl(routine);

            // If the first thing a IEnumerator block does is "yield break", calling StartCoroutine on it will return null
            if (coroutine == null)
            {
                return null;
            }

            CheckIfTypeIsSupported(coroutine);

            object autoDeleteCoroutine = StartCoroutineImpl(AddCoroutineWithAutoDelete(coroutine));

            mainCoroutineToAutoDelete.Add(coroutine, autoDeleteCoroutine);
            autoDeleteToMainCoroutine.Add(autoDeleteCoroutine, coroutine);

            //UnityEngine.Debug.Log(string.Format("Added. list: {0}, mainDict: {1}, inverseDict: {2}", coroutines.Count, mainCoroutineToAutoDelete.Count, autoDeleteToMainCoroutine.Count));

            //Return the autodelete coroutine. It will terminate right after the main coroutine, so it can be yielded.
            //We must do this because we need to yield until the end of the main coroutine to then do cleanup (remove it from the coroutines list, etc).
            //When removing the coroutine ( public void StopCoroutine(object routine)) we will stop both the main and the autodelete coroutines.
            return autoDeleteCoroutine;
        }

        /// <summary>
        /// The actual implementation of <see cref="StartCoroutine(IEnumerator)"/>.
        /// </summary>
        /// <param name="coroutine"></param>
        /// <returns></returns>
        abstract protected object StartCoroutineImpl(IEnumerator coroutine);

        /// <summary>
        /// Wait for seconds in real time (independant from game time and time scale).
        /// </summary>
        /// <param name="seconds">The number of seconds after which control should be returned to the routine.</param>
        /// <returns>A yieldable object.</returns>
        abstract public object WaitForSecondsRealtime(float seconds);

        /// <summary>
        /// The actual implementation of <see cref="StopCoroutine(object)"/>.
        /// </summary>
        /// <param name="routine"></param>
        abstract protected void StopCoroutineImpl(object routine);

        /// <summary>
        /// The supported type of the yield object that is returner from <see cref="StartCoroutine(IEnumerator)"/>.
        /// </summary>
        /// <returns>The supported type.</returns>
        abstract public System.Type SupportedType();

        /// <summary>
        /// Stop a given coroutine.
        /// </summary>
        /// <param name="routine">The routine to stop.</param>
        public void StopCoroutine(object routine)
        {
            CheckIfTypeIsSupported(routine);

            //The routine parameter is the auto delete coroutine, generated by IEnumerator AddCoroutineWithAutoDelete(object coroutine). Get the main coroutine that is actually doing the work.
            object mainCoroutine = autoDeleteToMainCoroutine[routine];

            StopCoroutineImpl(routine);
            StopCoroutineImpl(mainCoroutine);
            mainCoroutineToAutoDelete.Remove(routine);
            autoDeleteToMainCoroutine.Remove(mainCoroutine);
            coroutines.Remove(mainCoroutine);
        }

        /// <summary>
        /// Stop all the coroutines started by this manager.
        /// </summary>
        public void StopAllCoroutines()
        {
            foreach (object routine in coroutines)
            {
                StopCoroutineImpl(routine);
            }

            foreach (object routine in mainCoroutineToAutoDelete.Keys)
            {
                StopCoroutineImpl(mainCoroutineToAutoDelete[routine]);
            }

            coroutines.Clear();
            mainCoroutineToAutoDelete.Clear();
            autoDeleteToMainCoroutine.Clear();
        }

        private void CheckIfTypeIsSupported(object obj)
        {

            if (!obj.GetType().Equals(SupportedType()))
            {
                throw new System.Exception("Unsupported type: " + obj.GetType().ToString());
            }
        }
    }
}