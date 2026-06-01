Shader "InkForm/Parallax Layer Visual"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurRadius ("Screen Blur Radius", Range(0, 8)) = 0

        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_SKINNED_VERTEX_INPUTS
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaTex);
        SAMPLER(sampler_AlphaTex);

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _BlurRadius;
            half _EnableExternalAlpha;
        CBUFFER_END

        half4 SampleSpriteTexture(float2 uv)
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

            #if ETC1_EXTERNAL_ALPHA
            half externalAlpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
            color.a = lerp(color.a, externalAlpha, _EnableExternalAlpha);
            #endif

            return color;
        }

        Varyings ParallaxLayerVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            UNITY_SKINNED_VERTEX_COMPUTE(input);

            SetUpSpriteInstanceProperties();
            input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.color = input.color * _Color * unity_SpriteColor;
            return output;
        }

        half4 ParallaxLayerFragment(Varyings input) : SV_Target
        {
            half blurRadius = max(_BlurRadius, 0.0h);
            half4 texelColor;

            if (blurRadius <= 0.001h)
            {
                texelColor = SampleSpriteTexture(input.uv);
            }
            else
            {
                float2 screenPixelX = ddx(input.uv) * blurRadius;
                float2 screenPixelY = ddy(input.uv) * blurRadius;
                texelColor =
                    SampleSpriteTexture(input.uv) * 0.25h +
                    SampleSpriteTexture(input.uv + screenPixelX) * 0.125h +
                    SampleSpriteTexture(input.uv - screenPixelX) * 0.125h +
                    SampleSpriteTexture(input.uv + screenPixelY) * 0.125h +
                    SampleSpriteTexture(input.uv - screenPixelY) * 0.125h +
                    SampleSpriteTexture(input.uv + screenPixelX + screenPixelY) * 0.0625h +
                    SampleSpriteTexture(input.uv - screenPixelX - screenPixelY) * 0.0625h +
                    SampleSpriteTexture(input.uv + screenPixelX - screenPixelY) * 0.0625h +
                    SampleSpriteTexture(input.uv - screenPixelX + screenPixelY) * 0.0625h;
            }

            return texelColor * input.color;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex ParallaxLayerVertex
            #pragma fragment ParallaxLayerFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex ParallaxLayerVertex
            #pragma fragment ParallaxLayerFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            ENDHLSL
        }
    }
}
