using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WTTHalloweenClient
{
    /// <summary>Client-local cosmetic eye tracking for equipped and inspected wearables.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("WTT Halloween/Eye Tracking Patch")]
    public sealed class EyeTrackingPatch : MonoBehaviour
    {
        [Tooltip("Optional override for Unity testing. Leave empty for automatic in-game cameras.")]
        public Camera? targetCamera;
        [Tooltip("Response speed. Zero snaps immediately; higher positive values follow faster.")]
        [Min(0f)] public float smoothing = 12f;

        private EyeTrackingTarget[] _eyes = new EyeTrackingTarget[0];
        private Camera? _inspectionCamera;
        private bool _inspectionMode;
        private Camera? _characterPreviewCamera;
        private bool _characterPreviewMode;
        private static Type? _playerModelViewType;
        private static Type? _menuPlayerPoserType;
        private static Type? _canvasType;
        private static PropertyInfo? _canvasCamera;
        private static bool _resolvedPreviewTypes;
        private static PropertyInfo? _cameraManagerInstance;
        private static PropertyInfo? _gameplayCamera;
        private static bool _resolvedCameraManager;
        private static Camera? _cachedCamera;
        private static int _cameraFrame = -1;

        private void OnEnable()
        {
            // Discover the individually configured pivots once per activation, including inactive eyes.
            _eyes = GetComponentsInChildren<EyeTrackingTarget>(true);
            RefreshCharacterPreviewCamera();
        }

        private void LateUpdate()
        {
            var camera = targetCamera != null ? targetCamera
                : _inspectionMode ? _inspectionCamera
                : _characterPreviewMode ? _characterPreviewCamera : GetGameplayCamera();
            if (camera == null || !camera.isActiveAndEnabled) return;

            float blend = smoothing <= 0f ? 1f
                : 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            foreach (var eye in _eyes)
            {
                if (eye != null && eye.isActiveAndEnabled)
                    eye.Track(camera.transform.position, blend);
            }
        }

        private void OnDisable()
        {
            foreach (var eye in _eyes)
                if (eye != null) eye.RestoreNeutral();
            _inspectionCamera = null;
            _inspectionMode = false;
            _characterPreviewCamera = null;
            _characterPreviewMode = false;
        }

        private void RefreshCharacterPreviewCamera()
        {
            // The menu loader parents the body into PlayerModelView before activating it.
            // Resolve the owning UI camera here, rather than using an unrelated gameplay camera.
            if (!_resolvedPreviewTypes)
            {
                _resolvedPreviewTypes = true;
                _playerModelViewType = AccessTools.TypeByName("EFT.UI.PlayerModelView");
                _menuPlayerPoserType = AccessTools.TypeByName("MenuPlayerPoser");
                _canvasType = AccessTools.TypeByName("UnityEngine.Canvas");
                if (_canvasType != null) _canvasCamera = AccessTools.Property(_canvasType, "worldCamera");
            }
            var view = _playerModelViewType != null ? GetComponentInParent(_playerModelViewType) : null;
            if (view == null && _menuPlayerPoserType != null)
                view = GetComponentInParent(_menuPlayerPoserType);
            _characterPreviewMode = view != null;
            _characterPreviewCamera = null;
            if (!_characterPreviewMode) return;

            int renderLayers = 0;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) renderLayers |= 1 << renderer.gameObject.layer;
            if (renderLayers == 0) renderLayers = 1 << gameObject.layer;

            // A canvas camera may render only the menu background, not the character.
            // Accept it only when it can see the mask's layers and position.
            for (var parent = view!.transform; parent != null; parent = parent.parent)
            {
                var canvas = _canvasType != null ? parent.GetComponent(_canvasType) : null;
                if (canvas == null) continue;
                var camera = _canvasCamera?.GetValue(canvas) as Camera;
                if (CanRenderPreview(camera, renderLayers))
                {
                    _characterPreviewCamera = camera;
                    return;
                }
            }

            // Choose the nearest visible camera that renders the mask. In the main menu,
            // Camera_inventory renders the character; MainMenuCamera renders the background.
            // Search only at activation/attachment, never each frame.
            float nearestDistance = float.PositiveInfinity;
            foreach (var camera in Camera.allCameras)
            {
                if (!CanRenderPreview(camera, renderLayers)) continue;
                float distance = (camera.transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                _characterPreviewCamera = camera;
            }
            if (_characterPreviewCamera == null)
                Debug.LogWarning("[WTT Halloween] No camera found that renders the menu character's mask.", this);
        }

        private bool CanRenderPreview(Camera? camera, int renderLayers)
        {
            if (camera == null || !camera.isActiveAndEnabled
                || (camera.cullingMask & renderLayers) == 0) return false;
            Vector3 viewport = camera.WorldToViewportPoint(transform.position);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f
                && viewport.y >= 0f && viewport.y <= 1f;
        }

        private static Camera? GetGameplayCamera()
        {
            // Resolve the game camera without referencing Assembly-CSharp in the authoring DLL.
            // All masks share one camera lookup per frame; Unity authoring falls back to Camera.main.
            if (_cameraFrame == Time.frameCount) return _cachedCamera;
            _cameraFrame = Time.frameCount;
            if (!_resolvedCameraManager)
            {
                _resolvedCameraManager = true;
                var type = AccessTools.TypeByName("EFT.CameraControl.CameraManager");
                if (type != null)
                {
                    _cameraManagerInstance = AccessTools.Property(type, "Instance");
                    _gameplayCamera = AccessTools.Property(type, "Camera");
                }
            }
            var manager = _cameraManagerInstance?.GetValue(null);
            _cachedCamera = manager != null ? _gameplayCamera?.GetValue(manager) as Camera : null;
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            return _cachedCamera;
        }

        internal static void BindForModel(GameObject model, bool inspection, Camera? camera = null)
        {
            if (model == null) return;
            foreach (var tracker in model.GetComponentsInChildren<EyeTrackingPatch>(true))
            {
                tracker._inspectionMode = inspection;
                tracker._inspectionCamera = camera;
                if (!inspection) tracker.RefreshCharacterPreviewCamera();
            }
        }

        internal static void BindInspection(GameObject model, object preview)
        {
            var property = AccessTools.Property(preview.GetType(), "WeaponPreviewCamera");
            BindForModel(model, true, property?.GetValue(preview) as Camera);
        }
    }
}