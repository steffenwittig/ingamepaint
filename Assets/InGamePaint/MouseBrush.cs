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
        /// Size of the brush in pixels (actual world size determined by the resolution of the Paintable object's texture)
        /// </summary>
        public int brushSize = 32;

        /// <summary>
        /// Controls how dense should brush tips be painted when moving the brush
        /// </summary>
        public float brushFlow = 1;

        /// <summary>
        /// Alpha texture of the brush
        /// </summary>
        public Texture2D brushTip;

        protected Color color = Color.black;
        protected Texture2D brushColorTexture, brushAlphaOriginal;
        protected Paintable currentPaintable, lastPaintable;
        protected Vector2 currentPaintableCoords, lastPaintableCoords;
        protected bool paintedLastFrame = false;
        protected Renderer colorDisplayRenderer;

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

            ApplyColor();
        }

        protected void Update()
        {

            UpdatePaintableCoords();

            if (currentPaintable != null)
            {
                if (Input.GetMouseButton(0))
                {
                    // Left click: paint
                    Paint();
                } else
                {
                    paintedLastFrame = false;
                }

                if (Input.GetMouseButton(1))
                {
                    // Rick click: pick color
                    SetColor(currentPaintable.PickColor(currentPaintableCoords, 1f));
                }

            }

            // control opacity with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                color.a = Mathf.Min(1, Mathf.Max(0, color.a + scroll / 2));
                ApplyColor();
            }
        }

        protected void SetColor(Color color)
        {
            this.color = color;
            brushColorTexture = null;
            ApplyColor();
        }

        protected void ApplyColor()
        {
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
                int paintTips = Mathf.RoundToInt(distance * Mathf.Min(brushFlow,1) / brushSize * 4);
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

            Debug.Log(lastPaintableCoords);

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
            // Clone and scale texture
            if (brushTip.width != brushSize)
            {
                brushTip = new Texture2D(brushAlphaOriginal.width, brushAlphaOriginal.height);
                brushTip.SetPixels(brushAlphaOriginal.GetPixels());
                TextureScale.Bilinear(brushTip, brushSize, brushSize);
            }

            currentPaintable.PaintTexture(coords, brushTip, brushColorTexture);
        }

    }
}