Shader "NeonCircuit/Car Paint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PaintColor ("Paint Color", Color) = (1,0.34,0.08,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment PaintSpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            fixed4 _PaintColor;

            fixed4 PaintSpriteFrag(v2f IN) : SV_Target
            {
                fixed4 sprite = SampleSpriteTexture(IN.texcoord);
                fixed brightest = max(sprite.r, max(sprite.g, sprite.b));
                fixed darkest = min(sprite.r, min(sprite.g, sprite.b));
                fixed saturation = brightest - darkest;
                fixed luminance = dot(sprite.rgb, fixed3(0.299, 0.587, 0.114));

                // Repaint the neutral metal panels while preserving their shading.
                fixed neutralMask = 1.0 - smoothstep(0.10, 0.24, saturation);
                fixed metalMask = neutralMask * smoothstep(0.22, 0.56, luminance);
                fixed shade = saturate(luminance * 1.12 + 0.04);
                fixed highlight = smoothstep(0.82, 1.0, luminance);
                fixed3 paintedMetal = _PaintColor.rgb * shade;
                paintedMetal = lerp(paintedMetal, fixed3(1.0, 1.0, 1.0), highlight * 0.18);

                sprite.rgb = lerp(sprite.rgb, paintedMetal, metalMask);

                // Cyan trim is baked into every source sprite. It used to overpower
                // purple and red paint, making the car look blue from a distance.
                // Treat that trim as part of the body while keeping green neon,
                // yellow headlights and dark blue glass untouched.
                fixed cyanStrength = min(sprite.g, sprite.b) - sprite.r;
                fixed cyanTrimMask = smoothstep(0.055, 0.20, cyanStrength)
                    * smoothstep(0.24, 0.48, brightest);
                fixed3 paintedTrim = lerp(_PaintColor.rgb, fixed3(1.0, 1.0, 1.0), 0.16)
                    * saturate(brightest * 1.08);
                sprite.rgb = lerp(sprite.rgb, paintedTrim, cyanTrimMask);

                fixed4 result = sprite * IN.color;
                result.rgb *= result.a;
                return result;
            }
            ENDCG
        }
    }
}
