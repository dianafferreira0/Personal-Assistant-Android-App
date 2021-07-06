using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Didimo.Animation
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorEventManager : MonoBehaviour
    {
        Animator animator;

        private class AnimatorEvent
        {
            public UnityAction action;
            public string trigger;
        }

        List<AnimatorEvent> onAnimEndActions;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void Clear()
        {
            onAnimEndActions = null;
        }

        public void SubscribeToEndofCurrentAnimationAfterTrigger(string trigger, UnityAction action)
        {
            if (onAnimEndActions == null)
            {
                onAnimEndActions = new List<AnimatorEvent>();
            }

            AnimatorEvent e = new AnimatorEvent();
            e.action = action;
            e.trigger = trigger;

            onAnimEndActions.Add(e);
        }

        public void AnimationEnded()
        {
            if (onAnimEndActions != null && onAnimEndActions.Count > 0)
            {
                // Copy the list in case one of the actions subscribes to the end of the animation again
                List<AnimatorEvent> onAnimEndActionsCopy = new List<AnimatorEvent>(onAnimEndActions);

                foreach (AnimatorEvent e in onAnimEndActionsCopy)
                {
                    if (!animator.GetBool(e.trigger))
                    {
                        e.action();
                        onAnimEndActions.Remove(e);
                    }
                }
            }
        }
    }
}