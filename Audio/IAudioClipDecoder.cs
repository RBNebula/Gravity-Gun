using System;
using System.Collections;
using UnityEngine;

namespace GravityGun.Audio
{
    internal interface IAudioClipDecoder
    {
        IEnumerator Decode(byte[] bytes, string extension, string clipName, Action<AudioClip> onLoaded, Action<string> onFailed);
    }
}