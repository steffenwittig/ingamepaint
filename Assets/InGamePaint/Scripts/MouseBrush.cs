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
        /// Toggle help display on or off
        /// </summary>
        public bool showHelp = true;

        override protected void HandleInput()
        {

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
                    MixColor(currentPaintable.PickColor(currentPaintableCoords, 1), 1f, false);
                }

            }

            if (currentClickable)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    ClickClickable();
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

            if (Input.GetKeyDown(KeyCode.H))
            {
                showHelp = !showHelp;
            }
        }

        /// <summary>
        /// Shows help
        /// </summary>
        protected void OnGUI()
        {
            if (showHelp)
            {
                GUI.Label(
                    new Rect(0, 0, Screen.width, Screen.height),
                    "InGamePaint Mouse Brush:\nH: Show/Hide this help\nLeft-Click: Paint, pick brush or color preset\nRight-Click: Pick color from canvas\nMouse-Wheel: Adjust brush size\nMouse-Wheel+Shift:Adjust brush opacity\nS: Save");
            }
        }

        protected override float RayDistance
        {
            get
            {
                return 100f;
            }
        }

        override protected Ray GetRay()
        {
            return Camera.main.ScreenPointToRay(Input.mousePosition);
        }

    }
}