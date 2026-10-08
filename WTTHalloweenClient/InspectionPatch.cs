using System;
using HarmonyLib;
using UnityEngine;

namespace WTTHalloweenClient
{
    /// <summary>
    /// Restores explicitly assigned cosmetic Animators after the item preview disables them.
    /// Also supplies the preview camera to explicitly assigned EyeTrackingPatch components.
    /// </summary>
    internal static class InspectionPatch
    {
        internal static void Install(Harmony harmony)
        {
            // SetupItemPreview loads bundles asynchronously; PositionGameObject runs
            // once the actual model exists, after the preview disables its Animators.
            var type = AccessTools.TypeByName("EFT.UI.WeaponModding.WeaponPreview")
                ?? throw new TypeLoadException("EFT.UI.WeaponModding.WeaponPreview was not found.");
            var target = AccessTools.DeclaredMethod(type, "PositionGameObject",
                new[] { typeof(GameObject), typeof(Vector3?) })
                ?? throw new MissingMethodException(type.FullName, "PositionGameObject");
            var postfix = AccessTools.DeclaredMethod(typeof(InspectionPatch), nameof(AfterPositionGameObject));
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void AfterPositionGameObject(GameObject itemGameObject, object __instance)
        {
            ClothingAnimPatch.EnableForModel(itemGameObject);
            EyeTrackingPatch.BindInspection(itemGameObject, __instance);
        }
    }
}