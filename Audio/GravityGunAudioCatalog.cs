using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using GravityGun.Core;
using GravityGun.Resources;
using UnityEngine;

namespace GravityGun.Audio
{
    internal sealed class GravityGunAudioCatalog
    {
        private const string ResourceAudioFolderToken = ".Assets.Audio.";

        public SoundDefinition? Pickup;
        public SoundDefinition? Hold;
        public SoundDefinition? Drop;
        public SoundDefinition? Shoot;
        public SoundDefinition? FailedShoot;

        public IEnumerator LoadFromResources(Assembly assembly, IAudioClipDecoder decoder, ManualLogSource logger)
        {
            yield return LoadOne(assembly, decoder, logger, "gravity-gun-pickup", "pickup", def => Pickup = def);
            yield return LoadOne(assembly, decoder, logger, "gravity-gun-hold", "hold", def => Hold = def);
            yield return LoadOne(assembly, decoder, logger, "gravity-gun-drop", "drop", def => Drop = def);
            yield return LoadOne(assembly, decoder, logger, "gravity-gun-shoot", "shoot", def => Shoot = def);
            yield return LoadOne(assembly, decoder, logger, "gravity-gun-failed-shoot", "failed-shoot", def => FailedShoot = def);
        }

        public void ApplyTo(GravityGunBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            behaviour.SetAudio(Pickup, Hold, Drop, Shoot, FailedShoot);
        }

        private static IEnumerator LoadOne(
            Assembly assembly,
            IAudioClipDecoder decoder,
            ManualLogSource logger,
            string clipBaseName,
            string logicalName,
            Action<SoundDefinition?> setDefinition)
        {
            if (!TryFindAudioResourceName(assembly, clipBaseName, out string? resourceName))
            {
                logger.LogWarning(ModInfo.LOG_PREFIX + " Missing embedded audio resource for " + clipBaseName);
                setDefinition(null);
                yield break;
            }

            if (!EmbeddedResourceLoader.TryReadAllBytes(assembly, resourceName!, out byte[]? bytes) || bytes == null)
            {
                logger.LogWarning(ModInfo.LOG_PREFIX + " Failed to read embedded audio resource " + resourceName);
                setDefinition(null);
                yield break;
            }

            string extension = GetExtensionFromResource(resourceName!);
            AudioClip? clip = null;
            string? decodeError = null;

            yield return decoder.Decode(
                bytes,
                extension,
                "gravity-gun-" + logicalName,
                loaded => clip = loaded,
                err => decodeError = err);

            if (clip == null)
            {
                logger.LogWarning(ModInfo.LOG_PREFIX + " Failed to decode " + resourceName + ". " + decodeError);
                setDefinition(null);
                yield break;
            }

            setDefinition(SoundDefinitionFactory.CreateSingleClip("gravity-gun-" + logicalName, clip));
        }

        private static bool TryFindAudioResourceName(Assembly assembly, string clipBaseName, out string? resourceName)
        {
            resourceName = null;
            string[] names = assembly.GetManifestResourceNames();

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (name.IndexOf(ResourceAudioFolderToken, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (name.IndexOf(clipBaseName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (HasSupportedAudioExtension(name))
                {
                    resourceName = name;
                    return true;
                }
            }

            return false;
        }

        private static bool HasSupportedAudioExtension(string resourceName)
        {
            return resourceName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                   resourceName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                   resourceName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetExtensionFromResource(string resourceName)
        {
            if (resourceName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return ".ogg";
            }
            if (resourceName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return ".wav";
            }
            if (resourceName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return ".mp3";
            }

            return string.Empty;
        }
    }
}
