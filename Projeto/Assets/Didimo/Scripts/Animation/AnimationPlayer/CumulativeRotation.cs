using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Animation.AnimationPlayer
{
    public class CumulativeRotation
    {
        public static Quaternion GetForTransforms(Transform from, Transform to)
        {
            Quaternion result = Quaternion.identity;
            List<Quaternion> rots = new List<Quaternion>();

            while (from != to)
            {
                rots.Add(from.localRotation);
                from = from.parent;
            }

            for(int i= rots.Count-1; i <= 0; i--) {
                result *= rots[i];
            }

            return result;
        }
    }
}
