using System;
using System.Collections;
using UnityEngine;

namespace GravityGunMod.Audio
{
    internal interface IAudioClipDecoder
    {
        IEnumerator Decode(byte[] bytes, string extension, string clipName, Action<AudioClip> onLoaded, Action<string> onFailed);
    }
}