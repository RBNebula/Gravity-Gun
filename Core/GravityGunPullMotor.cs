using UnityEngine;

namespace GravityGunMod.Core
{
    internal sealed class GravityGunPullMotor
    {
        public void PullSingleTarget(Rigidbody? targetBody, Transform? cameraTransform, float holdDistance, float acceleration, float maxSpeed)
        {
            if (targetBody == null || cameraTransform == null)
            {
                return;
            }

            Vector3 holdPoint = cameraTransform.position + cameraTransform.forward * holdDistance;
            Vector3 toHoldPoint = holdPoint - targetBody.worldCenterOfMass;

            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
            {
                return;
            }

            Vector3 desiredVelocity = toHoldPoint / dt;
            float sqrMaxSpeed = maxSpeed * maxSpeed;
            if (desiredVelocity.sqrMagnitude > sqrMaxSpeed)
            {
                desiredVelocity = desiredVelocity.normalized * maxSpeed;
            }

            Vector3 velocityDelta = desiredVelocity - targetBody.linearVelocity;
            targetBody.AddForce(velocityDelta * acceleration, ForceMode.Acceleration);
        }

        public void LaunchSingleTarget(Rigidbody? targetBody, Transform? cameraTransform, float launchImpulse)
        {
            if (targetBody == null || cameraTransform == null)
            {
                return;
            }

            targetBody.AddForce(cameraTransform.forward * launchImpulse, ForceMode.Impulse);
        }

        public void ReleaseSingleTarget(Rigidbody? targetBody)
        {
            if (targetBody == null)
            {
                return;
            }
        }
    }
}