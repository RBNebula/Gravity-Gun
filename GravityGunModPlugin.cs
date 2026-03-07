using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using GravityGunMod.Audio;
using GravityGunMod.Core;
using GravityGunMod.Diagnostics;
using HarmonyLib;
using MineMogul.ToolImportApi.Api;
using MineMogul.ToolImportApi.Game;
using UnityEngine;

namespace GravityGunMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.main.toolimportapi", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class GravityGunModPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.main.gravitygun";
        public const string PluginName = "Gravity Gun Mod";
        public const string PluginVersion = "0.0.4";

        private const string WorldModelResourceName = "GravityGunMod.Assets.Models.gravity-gun-world.glb";
        private const string ViewModelResourceName = "GravityGunMod.Assets.Models.gravity-gun-view.glb";
        private const string IconResourceName = "GravityGunMod.Assets.Images.gravity-gun-icon.png";

        private static readonly Harmony HarmonyInstance = new Harmony(PluginGuid);

        private ToolRegistrationService? _registration;
        private GravityGunAudioCatalog? _audioCatalog;
        private readonly UnityWebRequestAudioClipDecoder _audioDecoder = new UnityWebRequestAudioClipDecoder();
        private ConfigEntry<bool>? _enableDebugLogging;

        public static GravityGunModPlugin? Instance { get; private set; }
        public static string RegisteredFullToolId { get; private set; } = string.Empty;

        private void Awake()
        {
            Instance = this;

            _enableDebugLogging = Config.Bind("Debug", "EnableDebugLogging", false, "Enable verbose Gravity Gun info logging in BepInEx LogOutput.");
            GravityGunLog.Initialize(Logger);
            GravityGunLog.SetDebugEnabled(_enableDebugLogging.Value);

            _audioCatalog = new GravityGunAudioCatalog();

            _registration = new ToolRegistrationService(this, WorldModelResourceName, ViewModelResourceName, IconResourceName);
            _registration.BindConfig();

            ToolRegistrationResult result = _registration.RegisterTool();
            RegisteredFullToolId = _registration.FullToolId;
            if (!result.Success)
            {
                Logger.LogWarning("[GravityGun] Tool registration failed: " + result.Message);
            }
            else
            {
                Logger.LogInfo("[GravityGun] Tool registration ok: " + result.FullToolId + " (" + result.Message + ")");
            }

            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        private IEnumerator Start()
        {
            if (_audioCatalog == null)
            {
                yield break;
            }

            yield return _audioCatalog.LoadFromResources(Assembly.GetExecutingAssembly(), _audioDecoder, Logger);
            ApplyAudioToLiveTools();
        }

        private void OnDestroy()
        {
            HarmonyInstance.UnpatchSelf();
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        internal void EnsureBehaviourAttached(RuntimeImportedTool tool)
        {
            if (!IsGravityGunTool(tool))
            {
                return;
            }

            GravityGunBehaviour? behaviour = tool.GetComponent<GravityGunBehaviour>();
            if (behaviour == null)
            {
                behaviour = tool.gameObject.AddComponent<GravityGunBehaviour>();
            }

            ApplyAudioToBehaviour(behaviour);
        }

        private void ApplyAudioToLiveTools()
        {
            GravityGunBehaviour[] behaviours = Object.FindObjectsByType<GravityGunBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                ApplyAudioToBehaviour(behaviours[i]);
            }
        }

        private void ApplyAudioToBehaviour(GravityGunBehaviour? behaviour)
        {
            if (behaviour == null || _audioCatalog == null)
            {
                return;
            }

            _audioCatalog.ApplyTo(behaviour);
        }

        private static bool IsGravityGunTool(RuntimeImportedTool? tool)
        {
            if (tool == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RegisteredFullToolId))
            {
                return false;
            }

            return string.Equals(tool.RegistrationKey, RegisteredFullToolId, System.StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryGetBehaviour(BaseHeldTool? tool, out GravityGunBehaviour behaviour)
        {
            behaviour = null!;
            if (!(tool is RuntimeImportedTool importedTool) || !IsGravityGunTool(importedTool))
            {
                return false;
            }

            GravityGunModPlugin? plugin = Instance;
            if (plugin == null)
            {
                return false;
            }

            plugin.EnsureBehaviourAttached(importedTool);
            GravityGunBehaviour? resolved = importedTool.GetComponent<GravityGunBehaviour>();
            if (resolved == null)
            {
                return false;
            }

            behaviour = resolved;
            return true;
        }
    }
}
