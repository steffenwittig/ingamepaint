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
        /// Alpha texture of the brush
        /// </summary>
        public Texture2D brushAlpha;

        /// <summary>
        /// Color of the brush
        /// </summary>
        public Color color = Color.black;

        protected Texture2D brushColorTexture; // TODO: make editable

        protected void Update()
        {

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);

            if (leftClick || rightClick)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                Debug.DrawRay(transform.position, ray.direction * paintDistance, Color.yellow, 0.2f, false);

                if (Physics.Raycast(ray, out hit, paintDistance))
                {
                    Paintable paintable = hit.collider.gameObject.GetComponent<Paintable>();
                    // Left click: paint
                    if (paintable != null)
                    {
                        if (leftClick)
                        {
                            // Left click: paint
                            if (brushAlpha == null)
                            {
                                paintable.PaintPixel(hit.textureCoord, Color.black);
                            }
                            else
                            {
                                // Scale texture TODO: clone original texture, because right now the same texture gets scaled each time
                                if (brushAlpha.width != brushSize)
                                {
                                    TextureScale.Bilinear(brushAlpha, brushSize, brushSize);
                                }

                                // If no color texture is set, we use the "color" attribute
                                if (brushColorTexture == null)
                                {
                                    brushColorTexture = new Texture2D(brushSize, brushSize);
                                    Color[] pixels = new Color[brushSize * brushSize];
                                    for (int i = 0; i < pixels.Length; i++)
                                    {
                                        pixels[i] = color;
                                    }
                                    brushColorTexture.SetPixels(pixels);
                                }

                                paintable.PaintTexture(hit.textureCoord, brushAlpha, brushColorTexture);
                            }
                        }
                        else
                        {
                            // Rick click: pick color
                            SetColor(paintable.PickColor(hit.textureCoord, 1f));
                        }
                    }
                    
                }
            }
        }

        protected void SetColor(Color color)
        {
            this.color = color;
            brushColorTexture = null;
        }
    }
}