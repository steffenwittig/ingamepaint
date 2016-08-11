using UnityEngine;
using System.Collections;

namespace InGamePaint
{
    public class MouseBrush : MonoBehaviour
    {

        public float paintDistance = 100f;
        public int brushSize = 32;
        public Texture2D brushTexture;
        public Color color = Color.black;

        protected Texture2D brushColorTexture;

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                Debug.DrawRay(transform.position, ray.direction * paintDistance, Color.yellow, 0.2f, false);

                if (Physics.Raycast(ray, out hit, paintDistance))
                {
                    Paintable paintable = hit.collider.gameObject.GetComponent<Paintable>();
                    if (paintable != null)
                    {
                        if (brushTexture == null)
                        {
                            paintable.PaintPixelUv(hit.textureCoord, Color.black);
                        }
                        else
                        {
                            if (brushTexture.width > brushSize)
                            {
                                TextureScale.Bilinear(brushTexture, brushSize, brushSize);
                            }

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

                            paintable.PaintTextureUv(hit.textureCoord, brushTexture, brushColorTexture);
                        }

                    }
                }
            }
        }
    }
}