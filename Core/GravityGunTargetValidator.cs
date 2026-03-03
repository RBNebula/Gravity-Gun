using UnityEngine;

namespace GravityGunMod.Core
{
    internal sealed class GravityGunTargetValidator
    {
        public bool IsValidTarget(
            PlayerController? owner,
            Rigidbody? body,
            int expectedInstanceId,
            float maxDistance,
            LayerMask grabbableLayer,
            bool disallowKinematicTargets)
        {
            if (owner == null || owner.PlayerCamera == null || body == null)
            {
                return false;
            }

            if (expectedInstanceId != -1 && body.GetInstanceID() != expectedInstanceId)
            {
                return false;
            }

            if (disallowKinematicTargets && body.isKinematic)
            {
                return false;
            }

            if (((1 << body.gameObject.layer) & grabbableLayer.value) == 0)
            {
                return false;
            }

            if (body.CompareTag("MarkedForDestruction"))
            {
                return false;
            }

            float sqrDistance = (body.worldCenterOfMass - owner.PlayerCamera.transform.position).sqrMagnitude;
            return sqrDistance <= maxDistance * maxDistance;
        }
    }
}