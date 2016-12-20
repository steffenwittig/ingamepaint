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
            brushSize = 128;

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

                if (Input.GetMouseButton(1))
                {
                    // Rick click
                    BrushColor = currentPaintable.PickColor(currentPaintableCoords, 1f);
                }

            }

            // control opacity or size with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
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
                    "InGamePaint Mouse Brush:\nH: Toggle Help\nLeft-Click: Paint\nRight-Click: Pick Color\nMouse-Wheel: Opacity\nMouse-Wheel+Shift:Size\nS: Save");
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
                colorDisplayRenderer.material.mainTexture = brushColorTexture;
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