using UnityEngine;

namespace GravityGun.Audio
{
    internal static class SoundDefinitionFactory
    {
        public static SoundDefinition? CreateSingleClip(string name, AudioClip? clip, float volume = 1f, float maxRange = 20f, int priority = 180)
        {
            if (clip == null)
            {
                return null;
            }

            SoundDefinition definition = ScriptableObject.CreateInstance<SoundDefinition>();
            definition.name = name + "_SoundDefinition";
            definition.sounds = new AudioClipDescription[1]
            {
                new AudioClipDescription
                {
                    clip = clip,
                    volume = volume,
                    pitch = 1f
                }
            };
            definition.minPitch = 1f;
            definition.maxPitch = 1f;
            definition.maxRange = maxRange;
            definition.Priority = priority;
            definition.hideFlags = HideFlags.HideAndDontSave;

            return definition;
        }
    }
}