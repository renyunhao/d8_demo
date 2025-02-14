using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace GameFramework
{
    public class SpriteOutlineEffect : MonoBehaviour
    {
        public string displayName;
        //降采样
        public int downSample = 1;
        //描边颜色
        public Color outlineColor = Color.red;
        //描边强度
        [Range(0.0f, 10.0f)]
        public float outlineStrength = 3.0f;
        //描边宽度
        [Range(0.0f, 10.0f)]
        public float outlineWidth = 3.0f;

        private HashSet<SpriteRenderer> spriteRendererSet = new HashSet<SpriteRenderer>(200);

        private Shader outlinePreShader;
        private Shader outlinePostShader;

        private Material outlinePreMaterial = null;
        private Material outlinePostMaterial = null;

        private RenderTexture renderTexture = null;
        private CommandBuffer commandBuffer = null;

        void Awake()
        {
            outlinePreShader = Shader.Find("GameFramwork/Sprite/OutlinePreEffect");
            outlinePostShader = Shader.Find("GameFramwork/Sprite/OutlinePostEffect");
            if (renderTexture == null)
                renderTexture = RenderTexture.GetTemporary(Screen.width >> downSample, Screen.height >> downSample, 0);

            commandBuffer = new CommandBuffer();
            commandBuffer.SetRenderTarget(renderTexture);

            if (outlinePostShader != null)
            {
                outlinePostMaterial = new Material(outlinePostShader);
            }
        }

        public void SetName(string name)
        {
            displayName = name;
        }

        public void ClearSpriteRenderer(bool refreshNow = true)
        {
            spriteRendererSet.Clear();
            if (refreshNow)
            {
                RefreshCommandBuffer();
            }
        }

        public void AddSpriteRenderer(SpriteRenderer newSR, bool refreshNow = true)
        {
            spriteRendererSet.Add(newSR);
            if (refreshNow)
            {
                RefreshCommandBuffer();
            }
        }

        public void RemoveSpriteRenderer(SpriteRenderer newSR, bool refreshNow = true)
        {
            spriteRendererSet.Remove(newSR);
            if (refreshNow)
            {
                RefreshCommandBuffer();
            }
        }

        public void RefreshCommandBuffer()
        {
            commandBuffer.ClearRenderTarget(true, true, Color.black);
            foreach (SpriteRenderer sr in spriteRendererSet)
            {
                if (outlinePreMaterial == null)
                {
                    outlinePreMaterial = new Material(sr.sharedMaterial);
                    outlinePreMaterial.shader = outlinePreShader;
                }
                commandBuffer.DrawRenderer(sr, outlinePreMaterial);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (spriteRendererSet.Count > 0 && outlinePreMaterial && outlinePostMaterial && renderTexture && commandBuffer != null)
            {
                //通过Command Buffer可以设置自定义材质的颜色
                outlinePreMaterial.SetColor("_OutlineColor", outlineColor);
                outlinePostMaterial.SetColor("_OutlineColor", outlineColor);
                outlinePostMaterial.SetFloat("_OutlineWidth", outlineWidth);
                //直接通过Graphic执行Command Buffer
                Graphics.ExecuteCommandBuffer(commandBuffer);

                //对RT进行Blur处理
                RenderTexture temp1 = RenderTexture.GetTemporary(source.width >> downSample, source.height >> downSample, 0);
                RenderTexture temp2 = RenderTexture.GetTemporary(source.width >> downSample, source.height >> downSample, 0);

                Graphics.Blit(renderTexture, temp2, outlinePostMaterial, 0);
                outlinePostMaterial.SetTexture("_BlurTex", temp2);
                Graphics.Blit(temp2, destination);

                //用模糊图和原始图计算出轮廓图
                Graphics.Blit(renderTexture, temp1, outlinePostMaterial, 1);
                outlinePostMaterial.SetTexture("_BlurTex", temp1);

                //轮廓图和场景图叠加
                outlinePostMaterial.SetFloat("_OutlineStrength", outlineStrength);
                Graphics.Blit(source, destination, outlinePostMaterial, 2);

                RenderTexture.ReleaseTemporary(temp1);
                RenderTexture.ReleaseTemporary(temp2);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}