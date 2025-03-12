// This class was taken from https://github.com/sugi-cho/Animation-Texture-Baker
// It has static functions that can be used to convert a RenderTexture to a Texture2D
// These functions are only used in debugging.
//--------------------------------------------------------------------------------------------------//

using UnityEngine;

namespace AnimCooker
{
    public class RenderTextureToTexture2D : MonoBehaviour
    {
        public static Texture2D Convert(RenderTexture rt, bool enableLinear)
        {
            TextureFormat format = TextureFormat.ARGB32;
            switch (rt.format) {
                case RenderTextureFormat.ARGBFloat: format = TextureFormat.RGBAFloat; break;
                case RenderTextureFormat.ARGBHalf: format = TextureFormat.RGBAHalf; break;
                case RenderTextureFormat.ARGBInt: format = TextureFormat.RGBA32; break;
                case RenderTextureFormat.ARGB32: format = TextureFormat.ARGB32; break;
                case RenderTextureFormat.ARGB64: format = TextureFormat.RGBA64; break;
                case RenderTextureFormat.ARGB4444: format = TextureFormat.ARGB4444; break;
                case RenderTextureFormat.ARGB1555: format = TextureFormat.ARGB4444; break;
                case RenderTextureFormat.RGB565: format = TextureFormat.RGB565; break;
                case RenderTextureFormat.RGB111110Float: format = TextureFormat.ARGB32; break;
                case RenderTextureFormat.RInt: format = TextureFormat.ARGB32; break;
                case RenderTextureFormat.RGInt: format = TextureFormat.RGBA64; break;
                case RenderTextureFormat.ARGB2101010: format = TextureFormat.ARGB32; break;
                case RenderTextureFormat.BGRA32: format = TextureFormat.BGRA32; break;
                case RenderTextureFormat.R16: format = TextureFormat.R16; break;
                case RenderTextureFormat.R8: format = TextureFormat.R8; break;
                case RenderTextureFormat.RFloat: format = TextureFormat.RFloat; break;
                case RenderTextureFormat.RG16: format = TextureFormat.RG16; break;
                case RenderTextureFormat.RG32: format = TextureFormat.RG32; break;
                case RenderTextureFormat.RGBAUShort: format = TextureFormat.RGBAHalf; break;
                case RenderTextureFormat.RGFloat: format = TextureFormat.RGFloat; break;
                case RenderTextureFormat.RGHalf: format = TextureFormat.RGHalf; break;
                case RenderTextureFormat.RHalf: format = TextureFormat.RHalf; break;
                default: format = TextureFormat.ARGB32; Debug.LogWarning("Unsuported RenderTextureFormat."); break;
            }
            return Convert(rt, format, enableLinear);
        }

        static Texture2D Convert(RenderTexture rt, TextureFormat format, bool enableLinear)
        {
            var tex2d = new Texture2D(rt.width, rt.height, format, false, enableLinear);
            var rect = Rect.MinMaxRect(0f, 0f, tex2d.width, tex2d.height);
            RenderTexture.active = rt;
            tex2d.ReadPixels(rect, 0, 0);
            RenderTexture.active = null;
            return tex2d;
        }
    }
} // namespace