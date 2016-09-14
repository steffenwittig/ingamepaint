using UnityEngine;
using VRTK;

namespace InGamePaint
{
    [RequireComponent(typeof(SteamVR_TrackedObject))]
    [RequireComponent(typeof(VRTK_ControllerEvents))]
    /// <summary>
    /// Interacts with a Paintable GameObject like a traditional mouse-based brush
    /// </summary>
    public class VrBrush : Brush
    {

        /// <summary>
        /// How far away can Paintable GameObjects be painted
        /// </summary>
        new protected float paintDistance = 0.5f;

        protected int paintBrushSize;

        protected float currentPaintableDistance;

        protected LineRenderer lineRenderer;

        public Material tipMaterial;

        new public int PaintBrushSize
        {
            get
            {
                return Mathf.Max(1, Mathf.RoundToInt((1 - currentPaintableDistance / paintDistance) * BrushSize));
            }
        }

        new protected void Start()
        {

            brushSize = 128;

            if (GetComponent<VRTK_ControllerEvents>() == null)
            {
                Debug.LogError("VRTK_ControllerEvents_ListenerExample is required to be attached to a SteamVR Controller that has the VRTK_ControllerEvents script attached to it");
                return;
            }

            //Setup controller event listeners
            GetComponent<VRTK_ControllerEvents>().TouchpadPressed += new ControllerInteractionEventHandler(TouchpadPressed);

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = tipMaterial;
            lineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.forward * paintDistance });
            lineRenderer.SetWidth(0.05f, 0);
            lineRenderer.useWorldSpace = false;

            ApplyBrushSettings();


        }

        protected void Update()
        {

            UpdatePaintableCoords();

            if(GetComponent<VRTK_ControllerEvents>().GetTriggerAxis() >= 0.1f)
            {
                BrushOpacity = GetComponent<VRTK_ControllerEvents>().GetTriggerAxis();
                Paint();
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
                    BrushSize += Mathf.RoundToInt(scroll*25);
                }
            }
        }

        new protected void ApplyBrushSettings()
        {
            base.ApplyBrushSettings();
            lineRenderer.SetColors(color, color);
        }

        override protected Ray GetRay()
        {
            GameObject source = GameObject.Find("Model");
            return new Ray(source.transform.position, source.transform.forward);
        }

        private void TouchpadPressed(object sender, ControllerInteractionEventArgs e)
        {
            BrushColor = currentPaintable.PickColor(currentPaintableCoords, 1f);
        }
        

    }
}