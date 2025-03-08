Shader "Pan/Vase/TextJack"
{
    Properties
    {
        [NoScaleOffset] 
        [MainTexture]
        _MainTex("Main Texture", 2D) = "white" {}

        [Header(Color)]
        [LDR] _TextColor("Text Color", Color) = (1,1,1,1)
        [LDR] _OutlineColor("Outline Color", Color) = (0,0,0,1)
        [LDR] _BackgroundColor("Background Color", Color) = (0,0,0,0)

        [Space(10)]
        [Header(General)]
        _Size("Size", Range(0, 1)) = 0.5

        [Header(Text)]
        _TextCutoff("Text Cutoff", Range(0,1)) = 0.4
        _TextSmooth("Text Smooth", Range(0,3)) = 0.2

        [Space(10)]
        [Header(Outline)]
        _OutlineWidth("Width", Range(0, 10)) = 4
        _OutlineSmooth("Smooth", Range(0,1)) = 0.1

        [Space(10)]
        [Header(Label)]
        _HeightRatio("Height Ratio", Range(0, 1)) = 0.125
        _CurrentNo("Current Number", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "ForceNoShadowCasting" = "True"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        LOD 100

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            ZTest Always
            ZWrite Off
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half4 pos : SV_POSITION;
                UNITY_FOG_COORDS(1)
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            half4 _MainTex_ST;
            half4 _MainTex_TexelSize;

            half4 _TextColor;
            half4 _OutlineColor;
            half4 _BackgroundColor;

            half _Size;
            half _TextCutoff;
            half _TextSmooth;
            half _OutlineWidth;
            half _OutlineSmooth;
            half _HeightRatio;
            int _CurrentNo;

            static const half3 LUMINANCE_WEIGHTS = half3(0.299h, 0.587h, 0.114h);

            // Added helper functions (converted to float for precision)
            bool isOrthographic()
            {
                return UNITY_MATRIX_P[3][3] == 1.0;
            }

            bool isInMirror()
            {
                return unity_CameraProjection[2][0] != 0.0 || unity_CameraProjection[2][1] != 0.0;
            }

            bool isPlayerView()
            {
            #if defined(USING_STEREO_MATRICES)
                return true;
            #endif
                if (isOrthographic())
                    return false;
                float t = unity_CameraProjection[1][1];
                const float Rad2Deg = 180.0 / UNITY_PI;
                float fov = atan(1.0 / t) * 2.0 * Rad2Deg;
                if (abs(fov - 60.0) >= 0.01)
                    return false;
                float3 centerVec = _WorldSpaceCameraPos;
                float3 y_vec = float3(0, 1, 0) + _WorldSpaceCameraPos;
                float4 centerProj = UnityWorldToClipPos(_WorldSpaceCameraPos);
                float4 projY = UnityWorldToClipPos(float4(y_vec, 1));
                float4 offset = centerProj - projY;
                if (abs(offset.x) >= 0.0001)
                    return false;
                return true;
            }

            bool isVRView()
            {
            #if defined(USING_STEREO_MATRICES)
                return true;
            #endif
                return false;
            }

            v2f vert(appdata v)
            {
                v2f o;
                // テクスチャの横幅に対する高さの割合を計算
                half aspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w; // width/height
                half sectionHeight = _HeightRatio * aspectRatio;
                half totalSections = 1.0h / sectionHeight;
                


                // 正確な位置計算
                o.uv.x = v.uv.x;
                o.uv.y = 1.0h - (v.uv.y * sectionHeight + _CurrentNo * sectionHeight);
                
                half3 cameraPos = _WorldSpaceCameraPos;
                half3 cameraForward = -UNITY_MATRIX_V._m20_m21_m22;
                half3 fixedPosition = cameraPos + cameraForward;
                
                half3 rightDir = normalize(UNITY_MATRIX_V._m00_m01_m02);
                half3 upDir    = normalize(UNITY_MATRIX_V._m10_m11_m12);
                
                half fovY = atan(1.0h / unity_CameraProjection._m11) * 2.0h;
                half fovX = atan(1.0h / unity_CameraProjection._m00) * 2.0h;

                half frustumHeight = 2.0h * tan(fovY * 0.5h);
                half frustumWidth = 2.0h * tan(fovX * 0.5h);

                half scaleWidth = min(frustumHeight/ _HeightRatio, frustumWidth) * _Size;
                half scaleHeight = scaleWidth * _HeightRatio;

                half3 billboardPos = fixedPosition
                    - rightDir * (v.vertex.x * scaleWidth)
                    - upDir    * (v.vertex.y * scaleHeight);
                
                o.pos = UnityWorldToClipPos(billboardPos);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Only render if in player or VR view; otherwise discard.
                if (!(isPlayerView() || isVRView()))
                {
                    discard;
                }
                
                half4 sampled = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
                half brightness = dot(sampled.rgb, LUMINANCE_WEIGHTS);
                half width = fwidth(brightness);

                half textCutLow = _TextCutoff - _TextSmooth * width;
                half textCutHigh = _TextCutoff + _TextSmooth * width;
                half textAlpha = smoothstep(textCutLow, textCutHigh, brightness);

                half aspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                half pixelSize = (_OutlineWidth * 2) / (_MainTex_TexelSize.z);
                half2 texelSize = half2(pixelSize, pixelSize* aspectRatio); // / _HeightRatio 

                half sumWeights = 0;
                half sumNeighborAlpha = 0;

                [unroll]
                for(half x = -1; x <= 1; x += 0.1)
                {
                    [unroll]
                    for(half y = -1; y <= 1; y += 0.1)
                    {
                        half2 offset = half2(x, y) * texelSize;
                        half4 neighborSample = UNITY_SAMPLE_TEX2D(_MainTex, i.uv + offset);
                        half neighborBrightness = dot(neighborSample.rgb, LUMINANCE_WEIGHTS);
                        half neighborAlpha = smoothstep(textCutLow, textCutHigh, neighborBrightness);
                        
                        half weight = exp(-(x * x + y * y));
                        sumWeights += weight;
                        sumNeighborAlpha += weight * neighborAlpha;
                    }
                }
                
                half outlineStrength = (sumNeighborAlpha / sumWeights) * (1 - textAlpha);
                half outlineAlpha = smoothstep(0, _OutlineSmooth, outlineStrength);

                half3 finalColor = lerp(_OutlineColor.rgb, _TextColor.rgb, textAlpha);
                half finalAlpha = max(textAlpha, outlineAlpha);
                finalColor = lerp(_BackgroundColor.rgb * _BackgroundColor.a, finalColor, finalAlpha);
                finalAlpha = max(finalAlpha, _BackgroundColor.a);

                fixed4 col = fixed4(finalColor, finalAlpha);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}