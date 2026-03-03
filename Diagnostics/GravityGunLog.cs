using BepInEx.Logging;

namespace GravityGunMod.Diagnostics
{
    internal static class GravityGunLog
    {
        private static ManualLogSource? _logger;
        private static bool _debugEnabled;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void SetDebugEnabled(bool enabled)
        {
            _debugEnabled = enabled;
        }

        public static void Info(string message)
        {
            if (_logger != null && _debugEnabled)
            {
                _logger.LogInfo("[GravityGun] " + message);
            }
        }

        public static void Warn(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning("[GravityGun] " + message);
            }
        }
    }
}