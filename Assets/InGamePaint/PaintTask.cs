using UnityEngine;
using System.Collections.Generic;

namespace InGamePaint
{
    /// <summary>
    /// Store necessary data to process a painting operation outside of the Unity main thread
    /// </summary>
    public class PaintTask
    {
        private Color32[] alphaTexturePixels;
        private Color32[] colorTexturePixels;
        private int x, y, baseTextureWidth, baseTextureHeight, alphaTextureWidth, alphaTextureHeight, colorTextureWidth, colorTextureHeight;
        private Paintable paintable;

        /// <summary>
        /// Creates the task
        /// </summary>
        /// <param name="paintable">The InGamePaint.Paintable object of which the texture should be updated</param>
        /// <param name="inputTextureCenterX">Horizontal position of the input texture's center</param>
        /// <param name="inputTextureCenterY">Vertical position of the input texture's center</param>
        /// <param name="colorTexture">Color values to apply to the region</param>
        /// /// <param name="alphaTexture">Alpha values to apply to the region</param>
        public PaintTask(Paintable paintable, int inputTextureCenterX, int inputTextureCenterY, Texture2D colorTexture, Texture2D alphaTexture)
        {
            this.paintable = paintable;

            baseTextureWidth = paintable.Width;
            baseTextureHeight = paintable.Height;

            alphaTexturePixels = alphaTexture.GetPixels32();
            alphaTextureWidth = alphaTexture.width;
            alphaTextureHeight = alphaTexture.height;

            colorTexturePixels = colorTexture.GetPixels32();
            colorTextureWidth = colorTexture.width;
            colorTextureHeight = colorTexture.height;

            this.x = inputTextureCenterX - alphaTextureWidth / 2;
            this.y = inputTextureCenterY - alphaTextureHeight / 2;
        }

        /// <summary>
        /// Performs the operation stored in the task
        /// </summary>
        public void Process()
        {
            Color32[] baseTexturePixels = paintable.Pixels;

            int brushWidth = alphaTextureWidth;
            int brushHeight = alphaTextureHeight;

            int brushSourceX = 0;
            int brushSourceY = 0;
            int brushSourceWidth = brushWidth;
            int brushSourceHeight = brushHeight;

            int textureWidth = baseTextureWidth;
            int textureHeight = baseTextureHeight;

            if (x < 0)
            {
                // brush is going over left border
                brushSourceX = Mathf.Abs(x);
                brushSourceWidth -= brushSourceX;
                x = 0;
            }
            else if (x + brushWidth > textureWidth)
            {
                // bush is going over right border
                brushSourceWidth -= x + brushWidth - textureWidth;
            }
            if (y + brushHeight > textureHeight)
            {
                // brush is going over upper border
                brushSourceHeight -= y + brushHeight - textureHeight;
            }
            else if (y < 0)
            {
                // brush is going over lower border
                brushSourceY = Mathf.Abs(y);
                brushSourceHeight -= brushSourceY;
                y = 0;
            }

            alphaTexturePixels = CropArea(alphaTexturePixels, brushHeight, brushSourceX, brushSourceY, brushSourceWidth, brushSourceHeight);
            colorTexturePixels = CropArea(colorTexturePixels, brushHeight, brushSourceX, brushSourceY, brushSourceWidth, brushSourceHeight);

            Color32[] newPixels = CopyArea(baseTexturePixels, baseTextureWidth, baseTextureHeight, colorTexturePixels, alphaTexturePixels, x, y, brushSourceWidth, brushSourceHeight);
            paintable.Pixels = newPixels;
        }

        /// <summary>
        /// Crop a pixel array
        /// </summary>
        /// <param name="inputPixels">Original pixels of the texture</param>
        /// <param name="inputHeight">Height of the texture</param>
        /// <param name="cropLeft">Amount of pixels to crop from the left</param>
        /// <param name="cropTop">Amount of pixels to crop from the top</param>
        /// <param name="outputWidth">Desired width of the new texture</param>
        /// <param name="outputHeight">Desired height of the new texture</param>
        /// <returns>Pixel array of the new texture</returns>
        protected Color32[] CropArea(Color32[] inputPixels, int inputHeight, int cropLeft, int cropTop, int outputWidth, int outputHeight)
        {
            List<Color32> clippedPixels = new List<Color32>();

            for (int y = cropTop; y < cropTop + outputHeight; y++)
            {
                for (int x = cropLeft; x < cropLeft + outputWidth; x++)
                {
                    int index = y * inputHeight + x;
                    clippedPixels.Add(inputPixels[index]);
                }
            }

            return clippedPixels.ToArray();
        }

        /// <summary>
        /// Copy one area of a texture, provided as a alpha and color pixel array, into another texture
        /// </summary>
        /// <param name="baseTexturePixels">Pixels of the base texture</param>
        /// <param name="baseWidth">Width of the base texture</param>
        /// <param name="baseHeight">Height of the base texture</param>
        /// <param name="sourceTexturePixels">color pixels of the input texture</param>
        /// <param name="sourceTexturePixelsAlpha">alpha pixels of the input texture</param>
        /// <param name="insertX">horizontal (left) starting position of the input textures</param>
        /// <param name="insertY">vertical (top) starting position of the input textures</param>
        /// <param name="insertWidth">width of the input textures</param>
        /// <param name="insertHeight">height of the input textures</param>
        /// <returns>The updated pixels of the base texture</returns>
        protected Color32[] CopyArea(
            Color32[] baseTexturePixels,
            int baseWidth,
            int baseHeight,
            Color32[] sourceTexturePixels,
            Color32[] sourceTexturePixelsAlpha,
            int insertX,
            int insertY,
            int insertWidth,
            int insertHeight)
        {
            baseTexturePixels = (Color32[])baseTexturePixels.Clone();

            int brushIndex = 0;

            for (int y = insertY; y < insertY + insertHeight; y++)
            {
                for (int x = insertX; x < insertX + insertWidth; x++)
                {

                    int baseIndex = (y * baseHeight) + x;

                    if (baseIndex < baseTexturePixels.Length && brushIndex < sourceTexturePixels.Length)
                    {
                        int baseAlpha = baseTexturePixels[baseIndex].a;
                        int alpha = alphaTexturePixels[brushIndex].a * colorTexturePixels[brushIndex].a;
                        float colorMix = alpha;
                        if (alpha > 0 && baseAlpha > 0)
                        {
                            colorMix = Mathf.Min(1, colorMix / baseAlpha);
                        } else if (baseAlpha == 0)
                        {
                            // we're drawing on a fully transparent surface, so the color value should be multiplied to prevent bright or dark edges
                            colorMix = Mathf.Min(1, colorMix*4);
                        }

                        int newAlpha = Mathf.Max(Mathf.Min(baseAlpha + alpha, byte.MinValue), byte.MaxValue);

                        baseTexturePixels[baseIndex].r = System.Convert.ToByte(Mathf.Lerp(baseTexturePixels[baseIndex].r, colorTexturePixels[brushIndex].r, colorMix));
                        baseTexturePixels[baseIndex].g = System.Convert.ToByte(Mathf.Lerp(baseTexturePixels[baseIndex].g, colorTexturePixels[brushIndex].g, colorMix));
                        baseTexturePixels[baseIndex].b = System.Convert.ToByte(Mathf.Lerp(baseTexturePixels[baseIndex].b, colorTexturePixels[brushIndex].b, colorMix));
                        baseTexturePixels[baseIndex].a = System.Convert.ToByte(newAlpha);

                    }
                    brushIndex++;
                }
            }

            return baseTexturePixels;
        }
        
    }
}