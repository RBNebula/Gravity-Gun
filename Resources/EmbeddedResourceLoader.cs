using System;
using System.IO;
using System.Reflection;

namespace GravityGun.Resources
{
    internal static class EmbeddedResourceLoader
    {
        public static bool TryReadAllBytes(Assembly assembly, string resourceName, out byte[]? bytes)
        {
            bytes = null;
            if (assembly == null || string.IsNullOrEmpty(resourceName))
            {
                return false;
            }

            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return false;
            }

            using (stream)
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            return true;
        }

        public static bool TryFindResourceName(Assembly assembly, string containsToken, out string? resourceName)
        {
            resourceName = null;
            if (assembly == null || string.IsNullOrEmpty(containsToken))
            {
                return false;
            }

            string[] names = assembly.GetManifestResourceNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].IndexOf(containsToken, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    resourceName = names[i];
                    return true;
                }
            }

            return false;
        }
    }
}