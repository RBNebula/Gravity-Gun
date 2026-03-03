using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GravityGunMod.Audio
{
    internal sealed class UnityWebRequestAudioClipDecoder : IAudioClipDecoder
    {
        public IEnumerator Decode(byte[] bytes, string extension, string clipName, Action<AudioClip> onLoaded, Action<string> onFailed)
        {
            if (bytes == null || bytes.Length == 0)
            {
                onFailed?.Invoke("Audio bytes were empty.");
                yield break;
            }

            AudioType audioType = GetAudioType(extension);
            if (audioType == AudioType.UNKNOWN)
            {
                onFailed?.Invoke("Unsupported audio extension: " + extension);
                yield break;
            }

            string tmpPath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + extension);
            File.WriteAllBytes(tmpPath, bytes);

            try
            {
                string url = "file:///" + tmpPath.Replace("\\", "/");
                using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
                {
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                    {
                        onFailed?.Invoke(req.error);
                        yield break;
                    }

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip == null)
                    {
                        onFailed?.Invoke("Decode returned null AudioClip.");
                        yield break;
                    }

                    clip.name = clipName;
                    onLoaded?.Invoke(clip);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tmpPath))
                    {
                        File.Delete(tmpPath);
                    }
                }
                catch
                {
                }
            }
        }

        private static AudioType GetAudioType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return AudioType.UNKNOWN;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".wav":
                    return AudioType.WAV;
                case ".mp3":
                    return AudioType.MPEG;
                default:
                    return AudioType.UNKNOWN;
            }
        }
    }
}