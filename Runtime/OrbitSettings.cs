using System;
using UnityEngine;

namespace VK.OrbitCamera
{
    /// <summary>
    /// Plain serializable config. Use it three ways:
    ///   - serialize it on the behaviour (default),
    ///   - wrap it in a ScriptableObject for shared presets,
    ///   - register it with your DI container (Reflex) and inject it.
    /// </summary>
    [Serializable]
    public sealed class OrbitSettings
    {
        [Header("Orbit")] [Tooltip("Degrees of rotation per full-screen-height swipe of normalized input.")]
        public float OrbitSpeed = 270f;

        public bool InvertY = false;

        [Header("Pitch limits (degrees)")] public float MinPitch = -20f;
        public float MaxPitch = 80f;

        [Header("Yaw limits (degrees)")] public bool ClampYaw = false;
        public float MinYaw = -180f;
        public float MaxYaw = 180f;

        [Header("Zoom / distance")]
        [Tooltip("Fraction of current distance removed per normalized zoom unit (per wheel notch).")]
        public float ZoomSpeed = 0.15f;

        public float MinDistance = 1.5f;
        public float MaxDistance = 25f;

        [Header("Pan (often off for showcases)")]
        public bool EnablePan = false;

        public float PanSpeed = 1f;

        [Header("Smoothing (seconds to ~target)")]
        public float RotationSmoothTime = 0.08f;

        public float ZoomSmoothTime = 0.10f;
        public float PivotSmoothTime = 0.12f;

        [Header("Idle auto-rotate (turntable)")]
        public bool AutoRotate = false;

        public float AutoRotateDelay = 3f;
        public float AutoRotateSpeed = 12f; // degrees / second

        [Header("Time")]
        [Tooltip("Use unscaled time so the camera keeps working while the game is paused (Time.timeScale = 0).")]
        public bool UseUnscaledTime = false;
    }
}