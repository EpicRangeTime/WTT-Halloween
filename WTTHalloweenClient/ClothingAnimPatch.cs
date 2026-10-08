using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace WTTHalloweenClient
{
    /// <summary>
    /// Attach to the wearable prefab and explicitly assign its cosmetic Animator.
    /// The client plugin re-enables it after Tarkov attaches the equipment model.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("WTT Halloween/Animation Patch")]
    public sealed class ClothingAnimPatch : MonoBehaviour
    {
        [Tooltip("The cosmetic Animator to enable after this wearable is attached. Only this Animator is affected.")]
        public Animator? targetAnimator;

        // Both equipment and inspection use the same serialized Animator assignment.
        internal static void EnableForModel(GameObject model)
        {
            if (model == null)
                return;

            foreach (var marker in model.GetComponentsInChildren<ClothingAnimPatch>(true))
            {
                try
                {
                    marker.EnableAssignedAnimator();
                }
                catch (Exception exception)
                {
                    // Cosmetic animation must not interrupt equipment or preview loading.
                    Debug.LogException(exception, marker);
                }
            }
        }
        private void OnEnable()
        {
            // Allows the assigned Animator to run in Unity Play mode as well.
            // In-game, CreateAndParent may disable it afterward; the postfix restores it.
            if (Application.isPlaying)
                EnableAssignedAnimator();
        }

        public void EnableAssignedAnimator()
        {
            if (!isActiveAndEnabled || targetAnimator == null)
                return;

            // Keep this marker scoped to the wearable's own hierarchy.
            if (targetAnimator.transform != transform
                && !targetAnimator.transform.IsChildOf(transform))
            {
                Debug.LogWarning("[WTT Halloween] Animation Patch must reference an Animator on this object or a child.", this);
                return;
            }

            if (!targetAnimator.enabled)
                targetAnimator.enabled = true;
        }
    }

    internal static class EquipmentAnimationPatch
    {
        internal static void Install(Harmony harmony)
        {
            var type = AccessTools.TypeByName("EFT.PlayerBody+SlotView")
                ?? throw new TypeLoadException("EFT.PlayerBody+SlotView was not found.");
            var target = AccessTools.DeclaredMethod(type, "CreateAndParent")
                ?? throw new MissingMethodException(type.FullName, "CreateAndParent");
            var postfix = AccessTools.DeclaredMethod(typeof(EquipmentAnimationPatch), nameof(AfterEquipmentAttached));
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void AfterEquipmentAttached(GameObject model)
        {
            ClothingAnimPatch.EnableForModel(model);
            EyeTrackingPatch.BindForModel(model, false);
        }
    }
}