using System;
using UnityEngine;
using VRTK;

namespace InGamePaint
{
    [RequireComponent(typeof(SteamVR_TrackedObject))]
    [RequireComponent(typeof(VRTK_ControllerEvents))]
    [RequireComponent(typeof(VRTK_ControllerActions))]
    [RequireComponent(typeof(VRTK_InteractGrab))]
    /// <summary>
    /// Interacts with a Paintable GameObject through Vive Controllers
    /// </summary>
    public class VrBrush : Brush
    {

            
        /// <summary>
        /// Strength of the vibration
        /// </summary>
        protected int maxPulseStrength = 1000;

        /// <summary>
        /// LineRenderer component to visualize the brush tip
        /// </summary>
        protected LineRenderer lineRenderer;

        override public int DynamicBrushSize
        {
            get
            {
                return Mathf.Max(1, Mathf.RoundToInt((1 - currentPaintableDistance / RayDistance) * MaxBrushSize));
            }
        }

        new protected void Start()
        {

            brushSize = 128;

            base.Start();

            if (GetComponent<VRTK_ControllerEvents>() == null)
            {
                Debug.LogError("VRTK_ControllerEvents_ListenerExample is required to be attached to a SteamVR Controller that has the VRTK_ControllerEvents script attached to it");
                return;
            }

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.SetPositions(new Vector3[] { Vector3.zero, GetRay().direction * RayDistance });
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0;
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = new Material(Shader.Find("Unlit/TransparentColor"));
            lineRenderer.material.color = BrushColor;

            ApplyBrushSettings();

        }

        override protected void HandleInput()
        {

            if (/*buttonPressure >= 0.1f && */currentPaintable != null)
            {
                float pulseStrength = (1 - currentPaintableDistance / RayDistance) * maxPulseStrength;
                GetComponent<VRTK_ControllerActions>().TriggerHapticPulse((ushort)pulseStrength);
                //BrushOpacity = buttonPressure;
                if (lastPaintable == null)
                {
                    UpdateBrushTextureToSize();
                }
                Paint();
            }
            if (currentClickable != null)
            {
                ClickClickable();
                GetComponent<VRTK_ControllerActions>().TriggerHapticPulse((ushort)maxPulseStrength);
            }

        }

        /// <summary>
        /// Apply brush Settings and change lineRenderer accordingly
        /// </summary>
        override protected void ApplyBrushSettings()
        {
            base.ApplyBrushSettings();
            lineRenderer.material.color = color;
        }

        protected override float RayDistance
        {
            get
            {
                return 0.2f;
            }
        }

        override protected Ray GetRay()
        {
            GameObject source = GameObject.Find("Model");
            return new Ray(source.transform.position, -source.transform.up);
        }

    }
}