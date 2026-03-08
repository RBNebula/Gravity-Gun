using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using MineMogul.ToolImportApi.Api;
using UnityEngine;

namespace GravityGun.Services
{
    internal sealed class ToolRegistrationService
    {
        private readonly BaseUnityPlugin _plugin;
        private readonly string _worldModelResourceName;
        private readonly string _viewModelResourceName;
        private readonly string _iconResourceName;

        private ConfigEntry<string>? _modId;
        private ConfigEntry<string>? _toolId;
        private ConfigEntry<string>? _cacheNamespace;
        private ConfigEntry<string>? _displayName;
        private ConfigEntry<string>? _description;
        private ConfigEntry<int>? _price;
        private ConfigEntry<string>? _category;
        private ConfigEntry<int>? _preferredSavableId;
        private ConfigEntry<int>? _shopBlockId;
        private ConfigEntry<string>? _qAction;

        public ToolRegistrationService(
            BaseUnityPlugin plugin,
            string worldModelResourceName,
            string viewModelResourceName,
            string iconResourceName)
        {
            _plugin = plugin;
            _worldModelResourceName = worldModelResourceName;
            _viewModelResourceName = viewModelResourceName;
            _iconResourceName = iconResourceName;
        }

        public string FullToolId { get; private set; } = string.Empty;

        public void BindConfig()
        {
            _modId = _plugin.Config.Bind("ToolImportApi", "ModId", "gravitygunmod", "Tool Import API mod id.");
            _toolId = _plugin.Config.Bind("ToolImportApi", "ToolId", "gravitygun", "Tool Import API tool id.");
            _cacheNamespace = _plugin.Config.Bind("ToolImportApi", "CacheNamespace", "gravitygunmod", "Optional Tool Import API cache namespace.");
            _displayName = _plugin.Config.Bind("ToolImportApi", "DisplayName", "Gravity Gun", "Display name in inventory/shop.");
            _description = _plugin.Config.Bind("ToolImportApi", "Description", "Imported Gravity Gun tool.", "Inventory description.");
            _preferredSavableId = _plugin.Config.Bind("ToolImportApi", "PreferredSavableId", 0, "Preferred SavableObjectID (<=0 = auto).");
            _shopBlockId = _plugin.Config.Bind("Shop", "ShopBlockId", 0, "Shop block id override (<=0 = auto).");
            _price = _plugin.Config.Bind("Shop", "Price", 500, "Shop price.");
            _category = _plugin.Config.Bind("Shop", "Category", "Tools", "Shop category.");
            _qAction = _plugin.Config.Bind("Shop", "QAction", "Rotate", "Q action label shown by shop integration.");
        }

        public ToolRegistrationResult RegisterTool()
        {
            string modId = (_modId!.Value ?? string.Empty).Trim();
            string toolId = (_toolId!.Value ?? string.Empty).Trim();
            string fullToolId = BuildFullToolId(modId, toolId);

            ToolRegistrationRequest request = new ToolRegistrationRequest
            {
                ModId = modId,
                ToolId = toolId,
                CacheNamespace = (_cacheNamespace!.Value ?? string.Empty).Trim(),
                DisplayName = string.IsNullOrWhiteSpace(_displayName!.Value) ? "Gravity Gun" : _displayName.Value,
                Description = _description!.Value,
                IconResourceName = _iconResourceName,
                WorldModelResourceName = _worldModelResourceName,
                ViewModelResourceName = _viewModelResourceName,
                ShopMetadata = new ToolShopMetadata
                {
                    Price = Mathf.Max(0, _price!.Value),
                    Category = string.IsNullOrWhiteSpace(_category!.Value) ? "Tools" : _category.Value,
                    IsLockedByDefault = false,
                    MaxStackSize = 1,
                    QAction = string.IsNullOrWhiteSpace(_qAction!.Value) ? "Rotate" : _qAction.Value,
                    ShopBlockId = _shopBlockId!.Value > 0 ? _shopBlockId.Value : (int?)null
                },
                PreferredSavableId = _preferredSavableId!.Value > 0 ? _preferredSavableId.Value : (int?)null,
                EnableCustomPositioning = true,
                CustomPositioningResourceName = fullToolId + ".arm-pose.cs",
                ArmPose = BuildGravityGunArmPose(),
                ViewModelTransform = new ToolViewTransformRequest
                {
                    TransformPath = ".",
                    LocalPosition = new Vector3(0.175f, 0f, 0.246f),
                    LocalEulerAngles = new Vector3(0f, 0f, 0f),
                    LocalScale = new Vector3(1f, 1f, 1f)
                }
            };

            ToolRegistrationResult result = ToolImportApi.RegisterTool(request);
            FullToolId = !string.IsNullOrWhiteSpace(result.FullToolId)
                ? result.FullToolId
                : BuildFullToolId(request.ModId, request.ToolId);
            return result;
        }

        private static string BuildFullToolId(string modId, string toolId)
        {
            if (string.IsNullOrWhiteSpace(modId))
            {
                return toolId ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(toolId))
            {
                return modId;
            }

            return modId + "." + toolId;
        }

        private static ArmPoseRequest BuildGravityGunArmPose()
        {
            return new ArmPoseRequest
            {
                PreferredArmPair = "Arm_Standard&Shirt.l|Arm_Standard&Shirt.r",
                BoneOverrides = new List<ArmBonePose>
                {
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L",
                        LocalEulerAngles = new Vector3(-9.636f, -9.425f, -131.053f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L",
                        LocalEulerAngles = new Vector3(18.957f, 2.512f, -5.414f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/ForearmTwist.1.L",
                        LocalEulerAngles = new Vector3(-0.003f, -0.421f, -0.002f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/ForearmTwist.1.L/ForearmTwist.2.L",
                        LocalEulerAngles = new Vector3(0.005f, 1.892f, 0.003f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/ForearmTwist.1.L/ForearmTwist.3.L",
                        LocalEulerAngles = new Vector3(0.109f, 2.729f, 0.041f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/ForearmTwist.1.L/ForearmTwist.4.L",
                        LocalEulerAngles = new Vector3(0.005f, 4.298f, 0.003f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L",
                        LocalEulerAngles = new Vector3(-16.348f, -2.154f, -38.501f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Index.1.L",
                        LocalEulerAngles = new Vector3(-67.653f, -34.315f, 22.44f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Index.1.L/Index.2.L",
                        LocalEulerAngles = new Vector3(-72.435f, -3.349f, 3.655f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Index.1.L/Index.2.L/Index.3.L",
                        LocalEulerAngles = new Vector3(-84.374f, 5.126f, -5.377f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Middle.1.L",
                        LocalEulerAngles = new Vector3(-61.623f, -2.57f, 0.83f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Middle.1.L/Middle.2.L",
                        LocalEulerAngles = new Vector3(-88.04f, -22.515f, 22.93f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Middle.1.L/Middle.2.L/Middle.3.L",
                        LocalEulerAngles = new Vector3(-83.392f, 3.636f, -4.062f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Pinky.1.L",
                        LocalEulerAngles = new Vector3(-71.322f, 36.503f, -20.461f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Pinky.1.L/Pinky.2.L",
                        LocalEulerAngles = new Vector3(-85.679f, 11.017f, -11.095f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Pinky.1.L/Pinky.2.L/Pinky.3.L",
                        LocalEulerAngles = new Vector3(-87.286f, -21.103f, 21.111f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Ring.1.L",
                        LocalEulerAngles = new Vector3(-87.611f, 36.036f, -26.135f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Ring.1.L/Ring.2.L",
                        LocalEulerAngles = new Vector3(-83.24f, -6.402f, 6.554f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Ring.1.L/Ring.2.L/Ring.3.L",
                        LocalEulerAngles = new Vector3(-78.309f, 6.655f, -6.87f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Thumb.1.L",
                        LocalEulerAngles = new Vector3(34.897f, -83.85f, 25.316f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Thumb.1.L/Thumb.2.L",
                        LocalEulerAngles = new Vector3(-41.419f, 1.694f, -4.999f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L/Forearm.L/Hand.L/Thumb.1.L/Thumb.2.L/Thumb.3.L",
                        LocalEulerAngles = new Vector3(-42.168f, 0.746f, -0.673f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R",
                        LocalEulerAngles = new Vector3(-8.081f, 89.707f, -111.659f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R",
                        LocalEulerAngles = new Vector3(1.649f, 17.062f, -71.691f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/ForearmTwist.1.R",
                        LocalEulerAngles = new Vector3(3.011f, -3.916f, -1.383f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/ForearmTwist.1.R/ForearmTwist.2.R",
                        LocalEulerAngles = new Vector3(-0.856f, 2.079f, 1.726f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/ForearmTwist.1.R/ForearmTwist.3.R",
                        LocalEulerAngles = new Vector3(0.139f, 6.77f, 2.007f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/ForearmTwist.1.R/ForearmTwist.4.R",
                        LocalEulerAngles = new Vector3(-0.62f, 9.069f, 1.293f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R",
                        LocalEulerAngles = new Vector3(-0.156f, -14.014f, 47.959f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Index.1.R",
                        LocalEulerAngles = new Vector3(-83.233f, -140.332f, 161.622f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Index.1.R/Index.2.R",
                        LocalEulerAngles = new Vector3(-48.062f, -0.18f, 0.417f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Index.1.R/Index.2.R/Index.3.R",
                        LocalEulerAngles = new Vector3(-68.512f, 0.892f, -0.597f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Middle.1.R",
                        LocalEulerAngles = new Vector3(-77.243f, 169.812f, -156.367f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Middle.1.R/Middle.2.R",
                        LocalEulerAngles = new Vector3(-53.461f, -8.793f, 5.506f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Middle.1.R/Middle.2.R/Middle.3.R",
                        LocalEulerAngles = new Vector3(-64.108f, 0.352f, -0.236f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Pinky.1.R",
                        LocalEulerAngles = new Vector3(-57.77f, -172.61f, 177.213f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Pinky.1.R/Pinky.2.R",
                        LocalEulerAngles = new Vector3(-3.357f, -0.306f, 0.185f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Pinky.1.R/Pinky.2.R/Pinky.3.R",
                        LocalEulerAngles = new Vector3(-51.253f, 0.988f, -0.886f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Ring.1.R",
                        LocalEulerAngles = new Vector3(-56.133f, -165.295f, 172.122f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Ring.1.R/Ring.2.R",
                        LocalEulerAngles = new Vector3(-0.274f, 0.472f, 1.049f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Ring.1.R/Ring.2.R/Ring.3.R",
                        LocalEulerAngles = new Vector3(-73.006f, -3.928f, 3.924f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Thumb.1.R",
                        LocalEulerAngles = new Vector3(45.975f, 86.842f, -38.652f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Thumb.1.R/Thumb.2.R",
                        LocalEulerAngles = new Vector3(-32.56f, 0.652f, 2.562f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R/Forearm.R/Hand.R/Thumb.1.R/Thumb.2.R/Thumb.3.R",
                        LocalEulerAngles = new Vector3(-56.719f, 17.158f, -14.243f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.L",
                        BoneName = "UpperArm.L",
                        LocalPosition = new Vector3(-0.225f, 0.027f, 1.094f)
                    },
                    new ArmBonePose
                    {
                        BonePath = "Arms_Root/UpperArm.R",
                        BoneName = "UpperArm.R",
                        LocalPosition = new Vector3(0.418f, 0.203f, 1.442f)
                    }
                }
            };
        }
    }
}
