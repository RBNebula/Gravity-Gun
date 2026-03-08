using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using GravityGun.Audio;
using GravityGun.Diagnostics;
using GravityGun.Services;
using HarmonyLib;
using MineMogul.ToolImportApi.Api;

namespace GravityGun
{
    [BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
    [BepInDependency("com.main.toolimportapi", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class GravityGun : BaseUnityPlugin
    {
        private const string WorldModelResourceName = "GravityGun.Assets.Models.gravity-gun-world.glb";
        private const string ViewModelResourceName = "GravityGun.Assets.Models.gravity-gun-view.glb";
        private const string IconResourceName = "GravityGun.Assets.Images.gravity-gun-icon.png";

        private readonly Harmony _harmony = new Harmony(ModInfo.HARMONY_ID);
        private readonly IAudioClipDecoder _audioDecoder = new UnityWebRequestAudioClipDecoder();

        private ToolRegistrationService? _registrationService;
        private GravityGunRuntimeService? _runtimeService;
        private GravityGunAudioCatalog? _audioCatalog;
        private ConfigEntry<bool>? _enableDebugLogging;

        private void Awake()
        {
            _runtimeService = new GravityGunRuntimeService();
            _runtimeService.Activate();

            _enableDebugLogging = Config.Bind("Debug", "EnableDebugLogging", false, "Enable verbose Gravity Gun info logging in BepInEx LogOutput.");
            GravityGunLog.Initialize(Logger);
            GravityGunLog.SetDebugEnabled(_enableDebugLogging.Value);

            _audioCatalog = new GravityGunAudioCatalog();
            _registrationService = new ToolRegistrationService(this, WorldModelResourceName, ViewModelResourceName, IconResourceName);
            _registrationService.BindConfig();

            ToolRegistrationResult result = _registrationService.RegisterTool();
            _runtimeService.SetRegisteredToolId(_registrationService.FullToolId);
            if (!result.Success)
            {
                Logger.LogWarning($"{ModInfo.LOG_PREFIX} Tool registration failed: {result.Message}");
            }
            else
            {
                Logger.LogInfo($"{ModInfo.LOG_PREFIX} Tool registration ok: {result.FullToolId} ({result.Message})");
            }

            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"{ModInfo.LOG_PREFIX} Initialized");
        }

        private IEnumerator Start()
        {
            if (_audioCatalog == null || _runtimeService == null)
            {
                yield break;
            }

            yield return _audioCatalog.LoadFromResources(Assembly.GetExecutingAssembly(), _audioDecoder, Logger);
            _runtimeService.SetAudioCatalog(_audioCatalog);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
            _runtimeService?.Deactivate();
        }
    }
}
