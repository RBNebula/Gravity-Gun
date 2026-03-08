using GravityGun.Audio;
using GravityGun.Core;
using MineMogul.ToolImportApi.Game;
using UnityEngine;

namespace GravityGun.Services
{
    internal sealed class GravityGunRuntimeService
    {
        private GravityGunAudioCatalog? _audioCatalog;

        public static GravityGunRuntimeService? Instance { get; private set; }

        public string RegisteredFullToolId { get; private set; } = string.Empty;

        public void Activate()
        {
            Instance = this;
        }

        public void Deactivate()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        public void SetRegisteredToolId(string fullToolId)
        {
            RegisteredFullToolId = fullToolId ?? string.Empty;
        }

        public void SetAudioCatalog(GravityGunAudioCatalog audioCatalog)
        {
            _audioCatalog = audioCatalog;
            ApplyAudioToLiveTools();
        }

        public void EnsureBehaviourAttached(RuntimeImportedTool tool)
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

        public bool TryGetBehaviour(BaseHeldTool? tool, out GravityGunBehaviour behaviour)
        {
            behaviour = null!;
            if (!(tool is RuntimeImportedTool importedTool) || !IsGravityGunTool(importedTool))
            {
                return false;
            }

            EnsureBehaviourAttached(importedTool);
            GravityGunBehaviour? resolved = importedTool.GetComponent<GravityGunBehaviour>();
            if (resolved == null)
            {
                return false;
            }

            behaviour = resolved;
            return true;
        }

        private bool IsGravityGunTool(RuntimeImportedTool? tool)
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
    }
}
