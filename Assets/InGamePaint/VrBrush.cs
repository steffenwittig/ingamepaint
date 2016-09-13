using UnityEngine;
using VRTK;

namespace InGamePaint
{
    [RequireComponent(typeof(SteamVR_TrackedObject))]
    [RequireComponent(typeof(VRTK_ControllerEvents))]
    /// <summary>
    /// Interacts with a Paintable GameObject like a traditional mouse-based brush
    /// </summary>
    public class VrBrush : MonoBehaviour
    {

        /// <summary>
        /// How far away can Paintable GameObjects be painted
        /// </summary>
        public float paintDistance = 0.5f;

        /// <summary>
        /// Alpha texture of the brush
        /// </summary>
        public Texture2D brushTip;

        protected int brushSize = 128, paintBrushSize;
        protected float brushSpacing = 0.3f;
        protected Color color = Color.black;
        protected Texture2D brushColorTexture, brushAlphaOriginal;
        protected Paintable currentPaintable, lastPaintable;
        protected Vector2 currentPaintableCoords, lastPaintableCoords;
        protected Vector3 shapeDisplayInitScale;
        protected Renderer colorDisplayRenderer, shapeDisplayRenderer;
        protected bool paintedLastFrame = false, showHelp = true;
        protected float currentPaintableDistance;

        protected LineRenderer lineRenderer;

        public Material tipMaterial;

        public Color BrushColor
        {
            get
            {
                return color;
            }
            set
            {
                color = new Color(
                    value.r,
                    value.g,
                    value.b,
                    color.a);
                brushColorTexture = null;
                ApplyBrushSettings();
            }
        }

        public float BrushOpacity
        {
            get
            {
                return BrushColor.a;
            }
            set
            {
                color.a = Mathf.Max(0, Mathf.Min(value,1));
                ApplyBrushSettings();
            }
        }

        public int BrushSize
        {
            get
            {
                return brushSize;
            }
            set
            {
                brushSize = Mathf.Max(value, 1);
            }
        }

        public int PaintBrushSize
        {
            get
            {
                return Mathf.Max(1, Mathf.RoundToInt((1 - currentPaintableDistance / paintDistance) * BrushSize));
            }
        }

        protected void Start()
        {
            if (brushTip == null)
            {
                brushTip = new Texture2D(2, 2); // 1x1 pixel texture causes errors in TextureScale
                brushTip.SetPixels(new Color[] { color, color, color, color });
            }

            brushAlphaOriginal = brushTip;

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

        protected void ApplyBrushSettings()
        {
            // Clone and scale texture
            if (brushTip.width != PaintBrushSize)
            {
                brushTip = new Texture2D(brushAlphaOriginal.width, brushAlphaOriginal.height);
                brushTip.SetPixels(brushAlphaOriginal.GetPixels());
                TextureScale.Bilinear(brushTip, PaintBrushSize, PaintBrushSize);
            }

            brushColorTexture = new Texture2D(PaintBrushSize, PaintBrushSize);
            Color[] pixels = new Color[PaintBrushSize * PaintBrushSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            brushColorTexture.SetPixels(pixels);
            brushColorTexture.Apply();
            Debug.Log("new color");
            lineRenderer.SetColors(color, color);

            if (colorDisplayRenderer != null)
            {
                colorDisplayRenderer.material.mainTexture = brushColorTexture;
            }
            if (shapeDisplayRenderer != null)
            {
                shapeDisplayRenderer.material.mainTexture = brushTip;
                shapeDisplayRenderer.transform.localScale = PaintBrushSize * shapeDisplayInitScale / 128;
            }

        }

        protected void UpdatePaintableCoords()
        {
            lastPaintable = currentPaintable;
            lastPaintableCoords = currentPaintableCoords;

            RaycastHit hit;
            GameObject source = GameObject.Find("Model");
            Ray ray = new Ray(source.transform.position, source.transform.forward);
            Debug.DrawRay(transform.position, ray.direction * paintDistance, Color.yellow, 0.2f, false);

            Paintable hitPaintable = null;

            if (Physics.Raycast(ray, out hit, paintDistance))
            {
                hitPaintable = hit.collider.gameObject.GetComponent<Paintable>();
            }

            if (hitPaintable != null) {
                currentPaintableDistance = hit.distance;
                currentPaintable = hitPaintable;
                currentPaintableCoords = currentPaintable.Uv2Pixel(hit.textureCoord);
            } else
            {
                currentPaintable = null;
            }
        }

        protected void Paint()
        {

            bool painted = false;

            if (paintedLastFrame && currentPaintable == lastPaintable)
            {
                // paint interpolated brush tips between the last painted coords and the current cords
                float distance = Vector2.Distance(lastPaintableCoords, currentPaintableCoords);
                int paintTips = Mathf.RoundToInt(distance / PaintBrushSize / brushSpacing);
                if (paintTips > 0)
                {
                    for (int i = 1; i <= paintTips; i++)
                    {
                        PaintTexture(Vector2.Lerp(lastPaintableCoords, currentPaintableCoords, (float)i/paintTips));
                        painted = true;
                    }
                }
            }

            if (!painted && (!paintedLastFrame || currentPaintableCoords != lastPaintableCoords))
            {
                // paint a single brush tip at the current coords if we didn't paint at that position last frame
                PaintTexture();
                painted = true;
            }

            paintedLastFrame = painted;
        }

        protected void PaintTexture()
        {
            PaintTexture(currentPaintableCoords);
        }

        protected void PaintTexture(Vector2 coords)
        {
            currentPaintable.PaintTexture(coords, brushTip, brushColorTexture);
        }

        private void TouchpadPressed(object sender, ControllerInteractionEventArgs e)
        {
            BrushColor = currentPaintable.PickColor(currentPaintableCoords, 1f);
        }
        

    }
}