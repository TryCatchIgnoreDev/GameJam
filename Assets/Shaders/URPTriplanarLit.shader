Shader "Custom/URP_Triplanar_Lit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Color Tint", Color) = (1,1,1,1)

        [Toggle(_NORMALMAP)] _EnableNormalMap("Enable Normal Map", Float) = 1
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        [Toggle(_METALLICSPECGLOSSMAP)] _EnableMetallicMap("Enable Metallic Map", Float) = 1
        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.2

        [Toggle(_OCCLUSIONMAP)] _EnableOcclusionMap("Enable Occlusion Map", Float) = 1
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1.0

        _Tiling("Global Tiling (Scale)", Float) = 0.5
        _BlendSharpness("Triplanar Blend Sharpness", Range(1, 16)) = 4.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _OCCLUSIONMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS      : SV_POSITION;
                float3 positionWS      : TEXCOORD0;
                float3 normalWS        : TEXCOORD1;
                float3 normalOS        : TEXCOORD2;
                float3 localPosScaled  : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);           SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);           SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap);  SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);      SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _BumpScale;
                float _Metallic;
                float _Smoothness;
                float _OcclusionStrength;
                float _Tiling;
                float _BlendSharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normInputs.normalWS;
                output.normalOS   = input.normalOS;

                float3 scale = float3(
                    length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
                    length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
                    length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z))
                );

                output.localPosScaled = input.positionOS.xyz * scale;
                return output;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 worldPos = input.positionWS;
                float3 geomNormalOS = normalize(input.normalOS);
                float3 geomNormalWS = normalize(input.normalWS);

                if (!isFrontFace)
                {
                    geomNormalOS = -geomNormalOS;
                    geomNormalWS = -geomNormalWS;
                }

                float3 blendWeights = pow(abs(geomNormalOS), _BlendSharpness);
                blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z, 0.0001);

                float3 pos = input.localPosScaled;
                float2 uvX = pos.zy * _Tiling;
                float2 uvY = pos.xz * _Tiling;
                float2 uvZ = pos.xy * _Tiling;

                // 1. Albedo
                half4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);
                half4 albedo = (colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z) * _BaseColor;

                // 2. Normal Map
                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _BumpScale);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _BumpScale);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _BumpScale);

                half3 objNX = half3(0, nX.y, nX.x * (geomNormalOS.x >= 0 ? 1.0 : -1.0));
                half3 objNY = half3(nY.x, 0, nY.y * (geomNormalOS.y >= 0 ? 1.0 : -1.0));
                half3 objNZ = half3(nZ.x * (geomNormalOS.z >= 0 ? 1.0 : -1.0), nZ.y, 0);

                half3 blendedNormalOS = normalize(
                    objNX * blendWeights.x +
                    objNY * blendWeights.y +
                    objNZ * blendWeights.z +
                    geomNormalOS
                );

                half3 blendedNormalWS = TransformObjectToWorldNormal(blendedNormalOS);

                // 3. Metallic & Smoothness
                half4 mX = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uvX);
                half4 mY = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uvY);
                half4 mZ = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uvZ);
                half4 blendedMetallic = mX * blendWeights.x + mY * blendWeights.y + mZ * blendWeights.z;

                half metallicVal = saturate(blendedMetallic.r * _Metallic);
                half smoothnessVal = saturate(blendedMetallic.a * _Smoothness);

                // 4. Ambient Occlusion
                half occX = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uvX).g;
                half occY = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uvY).g;
                half occZ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uvZ).g;
                half blendedOcc = occX * blendWeights.x + occY * blendWeights.y + occZ * blendWeights.z;
                half occlusionVal = LerpWhiteTo(blendedOcc, _OcclusionStrength);

                InputData inputData = (InputData)0;
                inputData.positionWS = worldPos;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = blendedNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(worldPos);
                inputData.shadowMask = half4(1, 1, 1, 1);
                inputData.bakedGI = SampleSH(blendedNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = metallicVal;
                surfaceData.smoothness = smoothnessVal;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = occlusionVal;
                surfaceData.alpha = 1.0;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.a = 1.0;
                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS   = normInputs.normalWS;
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                return half4(NormalizeNormalPerPixel(normalWS), 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
