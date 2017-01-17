using System;
using UnityEngine;

namespace InGamePaint
{
    /// <summary>
    /// Interacts with a Paintable GameObject like a traditional mouse-based brush
    /// </summary>
    public class MouseBrush : Brush
    {

        /// <summary>
        /// Initialize color and shape display fields
        /// </summary>
        new protected void Start()
        {

            base.Start();

            GameObject colorDisplay = GameObject.Find("BrushColor");
            if (colorDisplay != null)
            {
                colorDisplayRenderer = colorDisplay.GetComponent<Renderer>();
            }
            GameObject shapeDisplay = GameObject.Find("BrushShape");
            if (shapeDisplay != null)
            {
                shapeDisplayRenderer = shapeDisplay.GetComponent<Renderer>();
                shapeDisplayInitScale = shapeDisplay.transform.localScale;
            }

            ApplyBrushSettings();
        }

        /// <summary>
        /// React to mouse input
        /// </summary>
        protected void Update()
        {

            UpdatePaintableCoords();

            if (currentPaintable != null)
            {
                if (Input.GetMouseButton(0))
                {
                    // Left click
                    Paint();
                } else
                {
                    paintedLastFrame = false;
                }

                if (Input.GetMouseButtonDown(1))
                {
                    // Rick click
                    AddColor(currentPaintable.PickColor(currentPaintableCoords, 1), 1f, false);
                }

            }

            if (currentClickable)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    switch(currentClickable.GetType().ToString())
                    {
                        case "BrushPreset": ApplyPreset((BrushPreset)currentClickable); break;
                        case "ColorPreset": Debug.Log(((ColorPreset)currentClickable).color); AddColor(((ColorPreset)currentClickable).color, 1f, false); break;
                    }
                }
            }

            // control opacity or size with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    BrushOpacity += scroll / 2;
                } else
                {
                    MaxBrushSize += Mathf.RoundToInt(scroll*25);
                }
            }
        }

        /// <summary>
        /// Show help
        /// </summary>
        protected void OnGUI()
        {
            if (showHelp)
            {
                GUI.Label(
                    new Rect(0, 0, Screen.width, Screen.height),
                    "InGamePaint Mouse Brush:\nH: Toggle this help\nLeft-Click: Paint, pick brush or color preset\nRight-Click: Pick color from canvas\nMouse-Wheel: Adjust brush size\nMouse-Wheel+Shift:Adjust brush opacity\nS: Save");
            }
        }

        /// <summary>
        /// Update color and shape display fields
        /// </summary>
        override protected void ApplyBrushSettings()
        {
            base.ApplyBrushSettings();

            if (colorDisplayRenderer != null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, color);
                tex.Apply();
                colorDisplayRenderer.material.mainTexture = tex;

            }
            if (shapeDisplayRenderer != null)
            {
                shapeDisplayRenderer.material.mainTexture = brushTip;
                shapeDisplayRenderer.transform.localScale = DynamicBrushSize * shapeDisplayInitScale / 128;
            }
        }

        /// <summary>
        /// Return ray distance
        /// </summary>
        protected override float RayDistance
        {
            get
            {
                return 100f;
            }
        }

        /// <summary>
        /// Return the position of the cursor as a Ray
        /// </summary>
        /// <returns></returns>
        override protected Ray GetRay()
        {
            return Camera.main.ScreenPointToRay(Input.mousePosition);
        }

    }
}