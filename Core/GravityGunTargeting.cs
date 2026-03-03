using UnityEngine;

namespace GravityGunMod.Core
{
    internal sealed class GravityGunTargeting
    {
        private static readonly Vector3 CrosshairViewportPoint = new Vector3(0.5f, 0.5f, 0f);

        public bool TryGetSingleTarget(PlayerController? owner, float maxDistance, LayerMask layerMask, out Rigidbody? rb, out Vector3 hitPoint)
        {
            rb = null;
            hitPoint = Vector3.zero;

            if (owner == null || owner.PlayerCamera == null)
            {
                return false;
            }

            Ray ray = owner.PlayerCamera.ViewportPointToRay(CrosshairViewportPoint);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
            {
                return false;
            }

            rb = hit.collider.attachedRigidbody;
            if (rb == null)
            {
                rb = hit.collider.GetComponentInParent<Rigidbody>();
            }

            hitPoint = hit.point;
            return rb != null;
        }
    }
}