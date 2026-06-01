Shader "InkForm/Can Glow Additive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Glow Color", Color) = (1,0.48,0,0.58)
        _GlowPower ("Glow Power", Range(0.25, 4)) = 1.2

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

        Blend SrcAlpha One
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
            half _GlowPower;
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

        Varyings CanGlowVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            UNITY_SKINNED_VERTEX_COMPUTE(input);

            SetUpSpriteInstanceProperties();
            input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.color = input.color * unity_SpriteColor;
            return output;
        }

        half4 CanGlowFragment(Varyings input) : SV_Target
        {
            half4 sprite = SampleSpriteTexture(input.uv);
            half2 centered = abs(input.uv - 0.5h) * 2.0h;
            half radial = saturate(1.0h - length(centered) * 0.62h);
            half glow = pow(radial, _GlowPower) * sprite.a * input.color.a;
            half alpha = saturate(glow * _Color.a);

            return half4(_Color.rgb, alpha);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex CanGlowVertex
            #pragma fragment CanGlowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex CanGlowVertex
            #pragma fragment CanGlowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            ENDHLSL
        }
    }
}
