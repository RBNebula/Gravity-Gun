using GravityGun.Core;
using GravityGun.Services;
using HarmonyLib;
using MineMogul.ToolImportApi.Game;

namespace GravityGun.Patches
{
    [HarmonyPatch(typeof(RuntimeImportedTool), "OnEnable")]
    internal static class GravityGunRuntimeImportedToolEnablePatch
    {
        private static void Postfix(RuntimeImportedTool __instance)
        {
            GravityGunRuntimeService.Instance?.EnsureBehaviourAttached(__instance);
        }
    }

    [HarmonyPatch(typeof(BaseHeldTool), nameof(BaseHeldTool.GetControlsText))]
    internal static class GravityGunGetControlsTextPatch
    {
        private static bool Prefix(BaseHeldTool __instance, ref string __result)
        {
            GravityGunRuntimeService? service = GravityGunRuntimeService.Instance;
            if (service == null || !service.TryGetBehaviour(__instance, out GravityGunBehaviour behaviour))
            {
                return true;
            }

            __result = behaviour.GetControlsText();
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseHeldTool), nameof(BaseHeldTool.PrimaryFire))]
    internal static class GravityGunPrimaryFirePatch
    {
        private static bool Prefix(BaseHeldTool __instance)
        {
            GravityGunRuntimeService? service = GravityGunRuntimeService.Instance;
            if (service == null || !service.TryGetBehaviour(__instance, out GravityGunBehaviour behaviour))
            {
                return true;
            }

            behaviour.PrimaryFire();
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseHeldTool), nameof(BaseHeldTool.SecondaryFireHeld))]
    internal static class GravityGunSecondaryFireHeldPatch
    {
        private static bool Prefix(BaseHeldTool __instance)
        {
            GravityGunRuntimeService? service = GravityGunRuntimeService.Instance;
            if (service == null || !service.TryGetBehaviour(__instance, out GravityGunBehaviour behaviour))
            {
                return true;
            }

            behaviour.SecondaryFireHeld();
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseHeldTool), nameof(BaseHeldTool.Reload))]
    internal static class GravityGunReloadPatch
    {
        private static bool Prefix(BaseHeldTool __instance)
        {
            GravityGunRuntimeService? service = GravityGunRuntimeService.Instance;
            if (service == null || !service.TryGetBehaviour(__instance, out GravityGunBehaviour behaviour))
            {
                return true;
            }

            behaviour.Reload();
            return false;
        }
    }

    [HarmonyPatch(typeof(BaseHeldTool), nameof(BaseHeldTool.DropItem))]
    internal static class GravityGunDropItemPatch
    {
        private static void Prefix(BaseHeldTool __instance)
        {
            GravityGunRuntimeService? service = GravityGunRuntimeService.Instance;
            if (service != null && service.TryGetBehaviour(__instance, out GravityGunBehaviour behaviour))
            {
                behaviour.BeforeDropItem();
            }
        }
    }

    [HarmonyPatch(typeof(BaseHeldTool), nameof(BaseHeldTool.UnEquip))]
    internal static class GravityGunUnEquipPatch
    {
        private static void Prefix(BaseHeldTool __instance)
        {
            GravityGunRuntimeService? service = GravityGunRuntimeService.Instance;
            if (service != null && service.TryGetBehaviour(__instance, out GravityGunBehaviour behaviour))
            {
                behaviour.BeforeUnEquip();
            }
        }
    }
}
