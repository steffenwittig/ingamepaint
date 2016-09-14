using UnityEngine;
using System.Collections.Generic;

namespace InGamePaint
{
    public abstract class Brush : MonoBehaviour
    {

        /// <summary>
        /// How far away can Paintable GameObjects be painted
        /// </summary>
        protected float paintDistance = 100f;

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

        protected virtual void Start()
        {
            if (brushTip == null)
            {
                brushTip = new Texture2D(2, 2); // 1x1 pixel texture causes errors in TextureScale
                brushTip.SetPixels(new Color[] { color, color, color, color });
            }

            brushAlphaOriginal = brushTip;
        }

        protected virtual void ApplyBrushSettings()
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
        }

        protected void UpdatePaintableCoords()
        {
            lastPaintable = currentPaintable;
            lastPaintableCoords = currentPaintableCoords;

            RaycastHit hit;
            Ray ray = GetRay();
            Debug.DrawRay(transform.position, ray.direction * paintDistance, Color.yellow, 0.2f, false);

            Paintable hitPaintable = null;

            if (Physics.Raycast(ray, out hit, paintDistance))
            {
                hitPaintable = hit.collider.gameObject.GetComponent<Paintable>();
            }

            if (hitPaintable != null)
            {
                currentPaintable = hitPaintable;
                currentPaintableCoords = currentPaintable.Uv2Pixel(hit.textureCoord);
            }
            else
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
                        PaintTexture(Vector2.Lerp(lastPaintableCoords, currentPaintableCoords, (float)i / paintTips));
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
                color.a = Mathf.Max(0, Mathf.Min(value, 1));
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

        public int PaintBrushSize
        {
            get
            {
                return brushSize;
            }
        }

        abstract protected Ray GetRay();
    }
}