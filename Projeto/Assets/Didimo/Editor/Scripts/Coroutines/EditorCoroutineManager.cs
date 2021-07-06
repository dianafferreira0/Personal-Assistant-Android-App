using Didimo.Utils.Coroutines;
using System;
using System.Collections;
using UnityEngine;

namespace Didimo.Editor.Utils.Coroutines
{
    /// <summary>
    /// Coroutine manager that will work while in editor and play mode.
    /// Uses <see cref="CustomYieldInstruction"/>.
    /// </summary>
    public class EditorCoroutineManager : CoroutineManager
    {
        override protected object StartCoroutineImpl(IEnumerator coroutine)
        {
            return EditorYieldInstruction.Start(coroutine);
        }

        public override object WaitForSecondsRealtime(float seconds)
        {
            return new WaitForSecondsRealtime(seconds);
        }

        protected override void StopCoroutineImpl(object routine)
        {
            EditorYieldInstruction yieldInstruction = routine as EditorYieldInstruction;

            yieldInstruction.Stop();
        }

        public override Type SupportedType()
        {
            return typeof(EditorYieldInstruction);
        }
    }
}