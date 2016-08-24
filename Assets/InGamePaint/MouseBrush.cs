using UnityEngine;
using System.Collections;

namespace InGamePaint
{
    /// <summary>
    /// Interacts with a Paintable GameObject like a traditional mouse-based brush
    /// </summary>
    public class MouseBrush : MonoBehaviour
    {

        /// <summary>
        /// How far away can Paintable GameObjects be painted
        /// </summary>
        public float paintDistance = 100f;

        /// <summary>
        /// Alpha texture of the brush
        /// </summary>
        public Texture2D brushTip;

        protected int brushSize = 32;
        protected float brushSpacing = 0.3f;
        protected Color color = Color.black;
        protected Texture2D brushColorTexture, brushAlphaOriginal;
        protected Paintable currentPaintable, lastPaintable;
        protected Vector2 currentPaintableCoords, lastPaintableCoords;
        protected Vector3 shapeDisplayInitScale;
        protected Renderer colorDisplayRenderer, shapeDisplayRenderer;
        protected bool paintedLastFrame = false, showHelp = true;

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
                ApplyBrushSettings();
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
                    BrushSize += Mathf.RoundToInt(scroll*25);
                }
            }
        }

        protected void OnGUI()
        {
            if (showHelp)
            {
                GUI.Label(
                    new Rect(0, 0, Screen.width, Screen.height),
                    "InGamePaint Mouse Brush:\nH: Toggle Help\nLeft-Click: Paint\nRight-Click: Pick Color\nMouse-Wheel: Opacity\nMouse-Wheel+Shift:Size");
            }
        }

        protected void ApplyBrushSettings()
        {
            // Clone and scale texture
            if (brushTip.width != brushSize)
            {
                brushTip = new Texture2D(brushAlphaOriginal.width, brushAlphaOriginal.height);
                brushTip.SetPixels(brushAlphaOriginal.GetPixels());
                TextureScale.Bilinear(brushTip, brushSize, brushSize);
            }

            brushColorTexture = new Texture2D(brushSize, brushSize);
            Color[] pixels = new Color[brushSize * brushSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            brushColorTexture.SetPixels(pixels);
            brushColorTexture.Apply();

            if (colorDisplayRenderer != null)
            {
                colorDisplayRenderer.material.mainTexture = brushColorTexture;
            }
            if (shapeDisplayRenderer != null)
            {
                shapeDisplayRenderer.material.mainTexture = brushTip;
                shapeDisplayRenderer.transform.localScale = brushSize * shapeDisplayInitScale / 128;
            }

        }

        protected void UpdatePaintableCoords()
        {
            lastPaintable = currentPaintable;
            lastPaintableCoords = currentPaintableCoords;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(transform.position, ray.direction * paintDistance, Color.yellow, 0.2f, false);

            Paintable hitPaintable = null;

            if (Physics.Raycast(ray, out hit, paintDistance))
            {
                hitPaintable = hit.collider.gameObject.GetComponent<Paintable>();
            }

            if (hitPaintable != null) {
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
                int paintTips = Mathf.RoundToInt(distance / brushSize / brushSpacing);
                Debug.Log(paintTips);
                if (paintTips > 0)
                {
                    Vector2[] tipPositions = new Vector2[paintTips - 1]; // -1 because we won't need the first position, as it was already painted last frame
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
            }

            paintedLastFrame = true;
        }

        protected void PaintTexture()
        {
            PaintTexture(currentPaintableCoords);
        }

        protected void PaintTexture(Vector2 coords)
        {
            currentPaintable.PaintTexture(coords, brushTip, brushColorTexture);
        }

    }
}