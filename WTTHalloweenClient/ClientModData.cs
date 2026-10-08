using System;
using BepInEx;
using HarmonyLib;

namespace WTTHalloweenClient
{
    [BepInPlugin(PluginGuid, "WTT Halloween Client", "0.1.0")]
    public sealed class HalloweenClientPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.wtt.halloween.client";
        private Harmony? _harmony;

        private void Awake()
        {
            var harmony = new Harmony(PluginGuid);
            _harmony = harmony;
            InstallPatch("equipment Animator", () => EquipmentAnimationPatch.Install(harmony));
            InstallPatch("inspection Animator", () => InspectionPatch.Install(harmony));
        }

        private void InstallPatch(string description, Action install)
        {
            try
            {
                install();
                Logger.LogInfo($"WTT Halloween {description} patch enabled.");
            }
            catch (Exception exception)
            {
                // Keep the working patch available if another game hook changes.
                Logger.LogError($"Unable to enable the Halloween {description} patch: {exception}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}