using System;
using System.Collections;
using UnityEngine;

namespace Didimo.Utils.Coroutines
{
    /// <summary>
    /// Coroutine manager  that will work in a standalone (and editor) game.
    /// Uses Unity Coroutines.
    /// </summary>
    public class GameCoroutineManager : CoroutineManager
    {
        override protected object StartCoroutineImpl(IEnumerator coroutine)
        {
            return BasicMonoBehaviourSingleton.Instance.StartCoroutine(coroutine);
        }

        public override object WaitForSecondsRealtime(float seconds)
        {
            return new WaitForSecondsRealtime(seconds);
        }

        protected override void StopCoroutineImpl(object routine)
        {
            Coroutine coroutine = routine as Coroutine;

            if (BasicMonoBehaviourSingleton.Instance != null)
            {
                BasicMonoBehaviourSingleton.Instance.StopCoroutine(coroutine);
            }
        }

        public override Type SupportedType()
        {
            return typeof(Coroutine);
        }
    }
}
