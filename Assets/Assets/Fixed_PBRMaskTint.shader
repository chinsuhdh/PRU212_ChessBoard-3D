Shader "Universal Render Pipeline/Custom/Fixed_PBRMaskTint"
{
    Properties
    {
        // --- GIỮ NGUYÊN TÊN BIẾN ĐỂ KHÔNG MẤT SETTING CŨ ---
        _Albedo("Albedo", 2D) = "white" {}
        _Mask01("Mask01 (R=Tint1, G=Tint2, B=Tint3)", 2D) = "white" {}
        _Mask02("Mask02 (R=Tint4)", 2D) = "white" {}
        _SAM("SAM (R:Smooth, G:Occ, B:Metal)", 2D) = "white" {}
        
        _Color01("Color01", Color) = (0,0.139,0.808,0)
        _Color02("Color02", Color) = (0.455,0,0.617,0)
        _Color03("Color03", Color) = (0.617,0.268,0,0)
        _Color04("Color04", Color) = (0,0.617,0.055,0)
        
        _Color01Power("Color01Power", Range( 0 , 4)) = 1
        _Color02Power("Color02Power", Range( 0 , 4)) = 2
        _Color03Power("Color03Power", Range( 0 , 4)) = 1
        _Color04Power("Color04Power", Range( 0 , 4)) = 1
        
        _OverallBrightness("OverallBrightness", Range( 0 , 4)) = 1
        
        // Thêm tùy chỉnh độ bóng (Mặc định tắt bóng để đỡ bị đen)
        _ExtraSmoothness("Custom Smoothness", Range(0, 1)) = 0
        // Thêm tùy chỉnh cường độ tẩy màu gốc (1 = Tẩy trắng hoàn toàn, 0 = Giữ màu gốc)
        _Desaturation("Desaturation Strength", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry" 
        }

        // --- PASS 1: VẼ MÀU SẮC (FORWARD LIT) ---
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                float4 _Mask01_ST;
                float4 _Mask02_ST;
                float4 _SAM_ST;
                float4 _Color01;
                float4 _Color02;
                float4 _Color03;
                float4 _Color04;
                float _Color01Power;
                float _Color02Power;
                float _Color03Power;
                float _Color04Power;
                float _OverallBrightness;
                float _ExtraSmoothness;
                float _Desaturation;
            CBUFFER_END

            TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo);
            TEXTURE2D(_Mask01); SAMPLER(sampler_Mask01);
            TEXTURE2D(_Mask02); SAMPLER(sampler_Mask02);
            TEXTURE2D(_SAM);    SAMPLER(sampler_SAM);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(1,1,1,1));

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Lấy dữ liệu Texture
                float2 uv_Main = input.uv * _Albedo_ST.xy + _Albedo_ST.zw;
                half4 albedoTex = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, uv_Main);
                half4 mask01 = SAMPLE_TEXTURE2D(_Mask01, sampler_Mask01, uv_Main);
                half4 mask02 = SAMPLE_TEXTURE2D(_Mask02, sampler_Mask02, uv_Main);

                // 2. Tính toán các lớp màu Tint (như cũ)
                half3 tint01 = min(mask01.r, _Color01.rgb) * _Color01Power;
                half3 tint02 = min(mask01.g, _Color02.rgb) * _Color02Power;
                half3 tint03 = min(mask01.b, _Color03.rgb) * _Color03Power;
                half3 tint04 = min(mask02.r, _Color04.rgb) * _Color04Power;

                // Cộng dồn các lớp màu tint
                half3 totalTint = tint01 + tint02 + tint03 + tint04;
                
                // --- SỬA LỖI MÀU (NEW) ---
                // Bước A: Tính toán độ sáng (Grayscale) của texture gốc
                half grey = dot(albedoTex.rgb, float3(0.299, 0.587, 0.114));
                
                // Bước B: Tạo một nền màu trung tính (trắng đen) dựa trên độ sáng đó
                // _Desaturation = 0.9 nghĩa là làm mất 90% màu gốc (xanh) để dễ ăn màu mới (đỏ)
                half3 desaturatedAlbedo = lerp(albedoTex.rgb, float3(grey, grey, grey), _Desaturation);

                // Bước C: Nhân màu Tint vào cái nền đã tẩy trắng này
                half3 tintedResult = saturate(desaturatedAlbedo * totalTint) * _OverallBrightness;

                // Dùng tổng mask để quyết định vùng nào tô màu tint, vùng nào giữ nguyên
                half maskSum = mask01.r + mask01.g + mask01.b + mask02.r;
                
                // Kết quả cuối cùng
                half3 finalAlbedo = lerp(albedoTex.rgb, tintedResult, saturate(maskSum));

                // 3. Setup ánh sáng URP
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo;
                
                // ÉP CỨNG METALLIC = 0 để tránh bị đen
                surfaceData.metallic = 0; 
                surfaceData.smoothness = _ExtraSmoothness; 
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        // --- PASS 2: ĐỔ BÓNG (SHADOW CASTER) ---
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

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
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(1, 1, 1, 1));

                float3 positionWS = vertexInput.positionWS;
                float3 normalWS = normalInput.normalWS;

                // FIX LỖI: Dùng TransformWorldToHClip
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

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