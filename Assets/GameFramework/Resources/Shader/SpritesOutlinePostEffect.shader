Shader "GameFramwork/Sprite/OutlinePostEffect" {
 
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
        _BlurTex("Blur", 2D) = "white"{}
    }
 
    CGINCLUDE
    #include "UnityCG.cginc"
    
    //用于剔除中心留下轮廓
    struct v2f_cull
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
 
    //用于模糊
    struct v2f_blur
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
    };
 
    //用于最后叠加
    struct v2f_add
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
        float2 uv1 : TEXCOORD1;
    };
 
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BlurTex;
    float4 _BlurTex_TexelSize;
    float4 _offsets;
    float _OutlineStrength;
    float _OutlineWidth;
    float4 _OutlineColor;
 
    //高斯模糊 vert shader（之前的文章有详细注释，此处也可以用BoxBlur，更省一点）
    v2f_blur vert_blur(appdata_img v)
    {
        v2f_blur o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        return o;
    }
 
    //高斯模糊 pixel shader
    fixed4 frag_blur(v2f_blur i) : SV_Target
    {
        fixed4 left = tex2D(_MainTex, i.uv + float2(-1, 0) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 right = tex2D(_MainTex, i.uv + float2(1, 0) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 top = tex2D(_MainTex, i.uv + float2(0, 1) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 bottom = tex2D(_MainTex, i.uv + float2(0, 1) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 tl = tex2D(_MainTex, i.uv + float2(-1, 1) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 tr = tex2D(_MainTex, i.uv + float2(1, 1) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 bl = tex2D(_MainTex, i.uv + float2(-1, -1) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed4 br = tex2D(_MainTex, i.uv + float2(1, -1) * _MainTex_TexelSize.xy * _OutlineWidth);
        fixed leftValue = (left.r + left.g + left.b);
        fixed rightValue = (right.r + right.g + right.b);
        fixed topValue = (top.r + top.g + top.b);
        fixed bottomValue = (bottom.r + bottom.g + bottom.b);
        fixed tlValue = (tl.r + tl.g + tl.b);
        fixed trValue = (tr.r + tr.g + tr.b);
        fixed blValue = (bl.r + bl.g + bl.b);
        fixed brValue = (br.r + br.g + br.b);
        fixed standardValue = (_OutlineColor.r + _OutlineColor.g + _OutlineColor.b);
        fixed percent = (leftValue + rightValue + topValue + bottomValue + tlValue + trValue + blValue + brValue) / 8 / standardValue;

        fixed4 c = percent < 0.95 ? fixed4(0, 0, 0, 0) : _OutlineColor;

        return c;
    }
 
    //Blur图和原图进行相减获得轮廓
    v2f_cull vert_cull(appdata_img v)
    {
        v2f_cull o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        //dx中纹理从左上角为初始坐标，需要反向
#if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            o.uv.y = 1 - o.uv.y;
#endif    
        return o;
    }
 
    fixed4 frag_cull(v2f_cull i) : SV_Target
    {
        fixed4 colorMain = tex2D(_MainTex, i.uv);
        fixed4 colorBlur = tex2D(_BlurTex, i.uv);
        //最后的颜色是_BlurTex - _MainTex，周围0-0=0，黑色；边框部分为描边颜色-0=描边颜色；中间部分为描边颜色-描边颜色=0。最终输出只有边框
        //return fixed4((colorBlur - colorMain).rgb, 1);
        return colorMain - colorBlur;
    }
 
    //最终叠加 vertex shader
    v2f_add vert_add(appdata_img v)
    {
        v2f_add o;
        //mvp矩阵变换
        o.pos = UnityObjectToClipPos(v.vertex);
        //uv坐标传递
        o.uv.xy = v.texcoord.xy;
        o.uv1.xy = o.uv.xy;
#if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            o.uv.y = 1 - o.uv.y;
#endif    
        return o;
    }
 
    fixed4 frag_add(v2f_add i) : SV_Target
    {
        //取原始场景图片进行采样
        fixed4 ori = tex2D(_MainTex, i.uv1);
        //取得到的轮廓图片进行采样
        fixed4 blur = tex2D(_BlurTex, i.uv);
        fixed4 final = (blur.r > 0.1 || blur.g > 0.1|| blur.b > 0.1) ? blur * _OutlineStrength : ori;
        return final;
    }
 
        ENDCG
 
    SubShader
    {
        //pass 0: 高斯模糊
        Pass
        {
            ZTest Off
            Cull Off
            ZWrite Off
            Fog{ Mode Off }
 
            CGPROGRAM
            #pragma vertex vert_blur
            #pragma fragment frag_blur
            ENDCG
        }
        
        //pass 1: 剔除中心部分 
        Pass
        {
            ZTest Off
            Cull Off
            ZWrite Off
            Fog{ Mode Off }
 
            CGPROGRAM
            #pragma vertex vert_cull
            #pragma fragment frag_cull
            ENDCG
        }
 
 
        //pass 2: 最终叠加
        Pass
        { 
            ZTest Off
            Cull Off
            ZWrite Off
            Fog{ Mode Off }
 
            CGPROGRAM
            #pragma vertex vert_add
            #pragma fragment frag_add
            ENDCG
        }
 
    }
}