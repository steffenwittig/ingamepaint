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
        /// Size of the brush in pixels
        /// </summary>
        protected int paintBrushSize;
            
        /// <summary>
        /// Strength of the vibration
        /// </summary>
        protected int maxPulseStrength = 1000;

        /// <summary>
        /// LineRenderer component to visualize the brush tip
        /// </summary>
        protected LineRenderer lineRenderer;

        private float buttonPressure;

        /// <summary>
        /// Return brush size based on distance to the paintable
        /// </summary>
        override public int DynamicBrushSize
        {
            get
            {
                return Mathf.Max(1, Mathf.RoundToInt((1 - currentPaintableDistance / RayDistance) * MaxBrushSize));
            }
        }

        /// <summary>
        /// Set brush size, initialize lineRenderer
        /// </summary>
        new protected void Start()
        {

            brushSize = 128;

            base.Start();

            if (GetComponent<VRTK_ControllerEvents>() == null)
            {
                Debug.LogError("VRTK_ControllerEvents_ListenerExample is required to be attached to a SteamVR Controller that has the VRTK_ControllerEvents script attached to it");
                return;
            }

            //Setup controller event listeners
            GetComponent<VRTK_ControllerEvents>().TouchpadPressed += new ControllerInteractionEventHandler(TouchpadPressed);
            GetComponent<VRTK_ControllerEvents>().TriggerAxisChanged += new ControllerInteractionEventHandler(TriggerChanged);
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.SetPositions(new Vector3[] { Vector3.zero, GetRay().direction * RayDistance });
            lineRenderer.SetWidth(0.05f, 0);
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = BrushColor;

            ApplyBrushSettings();

        }

        /// <summary>
        /// React to controller input
        /// </summary>
        protected void Update()
        {

            UpdatePaintableCoords();

            if (buttonPressure >= 0.1f && currentPaintable != null)
            {
                float pulseStrength = (1 - currentPaintableDistance / RayDistance) * maxPulseStrength;
                GetComponent<VRTK_ControllerActions>().TriggerHapticPulse((ushort)pulseStrength);
                BrushOpacity = buttonPressure;
                Paint();
            }

        }

        /// <summary>
        /// Apply brush Settings and change lineRenderer accordingly
        /// </summary>
        override protected void ApplyBrushSettings()
        {
            base.ApplyBrushSettings();
            lineRenderer.material.color = BrushColor;
        }

        /// <summary>
        /// Return max ray distance
        /// </summary>
        protected override float RayDistance
        {
            get
            {
                return 0.2f;
            }
        }

        /// <summary>
        /// Returns ray depending on the orientation of the controller
        /// </summary>
        /// <returns></returns>
        override protected Ray GetRay()
        {
            GameObject source = GameObject.Find("Model");
            return new Ray(source.transform.position, -source.transform.up);
        }

        /// <summary>
        /// Pick color if controller is aimed at a Paintable and touchpad is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TouchpadPressed(object sender, ControllerInteractionEventArgs e)
        {
            if (currentPaintable != null)
            {
                BrushColor = currentPaintable.PickColor(currentPaintableCoords, 1f);
                ApplyBrushSettings();
            }
        }

        private void TriggerChanged(object sender, ControllerInteractionEventArgs e)
        {
            if (e.buttonPressure == 0)
            {
                paintedLastFrame = false;
            }
            buttonPressure = e.buttonPressure;
        }

    }
}