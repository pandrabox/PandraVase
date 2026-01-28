Shader "Pan/Vase/GridUI"
{
    Properties
    {
        [NoScaleOffset] 
        [MainTexture]
        _MainTex("Main Texture", 2D) = "white" {}

        [NoScaleOffset]
        [MainTexture]
        _Tex2("Tex2", 2D) = "white" {}

        [Space(10)]
        [Header(General)]
        _Mode("Mode", Range(0, 1)) = 0
        _Opacity("Opacity", Range(0, 1)) = 1.0

        [Space(10)]
        [Header(Mode0)]
        _Size("Size", Range(0, 1)) = 0.5
        _xMax("X Max", Int) = 5
        _yMax("Y Max", Int) = 3
        _x("X", Int) = 0.0
        _y("Y", Int) = 0.0
        _SelectColor("Select Color", Color) = (1, 0, 0, 0.5)
        _CornerSize("Corner Size", Range(0, 1)) = 0.2
        _CornerThickness("Corner Thickness", Range(0, 1)) = 0.5
        
        [Space(10)]
        [Header(Mode1)]
        _Size2("Size2", Range(0, 1)) = 0.5
        _Tex2Size("Tex2 Size", Range(0.1, 1)) = 1.0
        _Tex2Y("Tex2 Y Position", Range(0, 1)) = 0.5
        _xPos("X Position", Range(0, 1)) = 0.5
        _yPos("Y Position", Range(0, 1)) = 0.5
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
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CameraAnalyzer.hlsl"
            #include "FixedBillboard.hlsl"
            #include "Cell.hlsl"

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

            // Main texture declarations
            UNITY_DECLARE_TEX2D(_MainTex);
            half4 _MainTex_ST;
            half4 _MainTex_TexelSize;

            // Tex2 declarations
            UNITY_DECLARE_TEX2D(_Tex2);
            half4 _Tex2_ST;
            half4 _Tex2_TexelSize;

            int _Mode;
            half _Size;
            half _Size2;
            half _xPos;
            half _yPos;
            int _xMax;
            int _yMax;
            int _x;
            int _y;
            half4 _SelectColor;
            half _CornerSize;
            half _CornerThickness;
            half _Tex2Y;
            half _Tex2Size;
            half _Opacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                half aspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                if (_Mode == 0)
                {
                    FixedBillboard fixedBillboard = GetFixedBillboard(aspectRatio, _Size);

                    half3 billboardPos = fixedBillboard.position
                        + fixedBillboard.rightDir * (v.vertex.x * fixedBillboard.width)
                        + fixedBillboard.upDir    * (v.vertex.y * fixedBillboard.height);
                    //v.vertexをuvに変更できないか検討（Oculusが心配）


                    o.pos = UnityWorldToClipPos(billboardPos);
                }
                else
                {
                    Cell cell = GetCell(_xMax, _yMax, _x, _y);
                    FixedBillboard fixedBillboard = GetFixedBillboard(aspectRatio * cell.aspectRatio , _Size2);

                    o.uv = v.uv;

                    half2 positionOffset = 
                        half2(_xPos - 0.5, _yPos - 0.5)
                          * (fixedBillboard.frustumSize - fixedBillboard.size);

                    half3 billboardPos = fixedBillboard.position
                        + fixedBillboard.rightDir * (v.vertex.x * fixedBillboard.width + positionOffset.x )
                        + fixedBillboard.upDir    * (v.vertex.y * fixedBillboard.height + positionOffset.y);

                    o.pos = UnityWorldToClipPos(billboardPos);
                }

                o.uv = float2(1.0 - o.uv.x, 1.0 - o.uv.y); //回転補正
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // DEBUG: discard判定を一時無効化
                // if (!(isPlayerView() || isVRView()))
                // {
                //     discard;
                // }

                half mainAspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                Cell cell = GetCell(_xMax, _yMax, _x, _y);

                // Mode1の場合、UV座標を選択セルに合わせて変換
                float2 currentUV = i.uv;
                if (_Mode == 1)
                {
                    // UV座標を選択したセルの範囲に変換
                    currentUV.x = (i.uv.x * cell.size.x) + cell.min.x;
                    currentUV.y = (i.uv.y * cell.size.y) + cell.min.y;
                }

                fixed4 baseColor = UNITY_SAMPLE_TEX2D(_MainTex, currentUV);
                fixed4 finalColor = baseColor;

                // 枠の描画
                float cornerSizex = _CornerSize * cell.size.x;
                float cornerSizey = _CornerSize * cell.size.x * mainAspectRatio;
                float2 cornerThickness = float2(cornerSizex * _CornerThickness, cornerSizey * _CornerThickness);
                float2 centerRectMin = cell.min + cornerThickness;
                float2 centerRectMax = cell.max - cornerThickness;
                bool isInCorner = 
                    ((currentUV.x >= cell.min.x && currentUV.x < cell.min.x + cornerSizex) || 
                        (currentUV.x >= cell.max.x - cornerSizex && currentUV.x < cell.max.x))  &&
                    ((currentUV.y >= cell.min.y && currentUV.y < cell.min.y + cornerSizey) || 
                        (currentUV.y >= cell.max.y - cornerSizey && currentUV.y < cell.max.y));
                bool isNotInCorner = 
                    currentUV.x >= centerRectMin.x && 
                    currentUV.x < centerRectMax.x && 
                    currentUV.y >= centerRectMin.y && 
                    currentUV.y < centerRectMax.y;
                if (isInCorner && !isNotInCorner)
                {
                    finalColor = lerp(finalColor, _SelectColor, _SelectColor.a);
                }

                if(_Mode==1)
                {
                    float tex2Aspect = _Tex2_TexelSize.z / _Tex2_TexelSize.w;
                    float tex2Height = cell.size.x / tex2Aspect;
                    float tex2ScaledWidth = cell.size.x * _Tex2Size;
                    float tex2ScaledHeight = tex2Height * _Tex2Size;
                    float2 tex2UV;
                    tex2UV.x = (currentUV.x - cell.min.x) / tex2ScaledWidth;
                    tex2UV.y = (currentUV.y - cell.min.y) / tex2ScaledHeight/mainAspectRatio;
                    fixed4 tex2Sample = UNITY_SAMPLE_TEX2D(_Tex2, tex2UV);
                    fixed3 blendedRGB = tex2Sample.rgb * tex2Sample.a + finalColor.rgb * (1.0 - tex2Sample.a);
                    fixed blendedA = tex2Sample.a + finalColor.a * (1.0 - tex2Sample.a);
                    finalColor = fixed4(blendedRGB, blendedA);
                }

                finalColor.a *= _Opacity;
                return finalColor;
            }
            ENDCG
        }
    }
}