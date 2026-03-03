using UnityEngine;

namespace GravityGunMod.Core
{
    internal sealed class GravityGunAudioController
    {
        public void PlayAcquire(SoundDefinition? sound, Vector3 point)
        {
            Play(sound, point);
        }

        public void PlayRelease(SoundDefinition? sound, Vector3 point)
        {
            Play(sound, point);
        }

        public void PlayShoot(SoundDefinition? sound, Vector3 point)
        {
            Play(sound, point);
        }

        public void PlayFailedShoot(SoundDefinition? sound, Vector3 point)
        {
            Play(sound, point);
        }

        public void TryPlayPullTick(SoundDefinition? sound, Vector3 point, float intervalSeconds, ref float nextTime)
        {
            if (Time.time < nextTime)
            {
                return;
            }

            Play(sound, point);
            nextTime = Time.time + intervalSeconds;
        }

        private static void Play(SoundDefinition? sound, Vector3 point)
        {
            SoundManager soundManager = Singleton<SoundManager>.Instance;
            if (soundManager == null || sound == null)
            {
                return;
            }

            soundManager.PlaySoundAtLocation(sound, point);
        }
    }
}