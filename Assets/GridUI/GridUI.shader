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

        [Space(10)]
        [Header(Mode0)]
        _Size("Size", Range(0, 1)) = 0.5
        _xMax("X Max", Int) = 5
        _yMax("Y Max", Int) = 3
        _x("X", Range(0, 1)) = 0.0
        _y("Y", Range(0, 1)) = 0.0
        _SelectColor("Select Color", Color) = (1, 0, 0, 0.5)
        _CornerSize("Corner Size", Range(0, 1)) = 0.2
        _CenterRectSize("Center Rect Size", Range(0, 1)) = 0.5
        _Tex2Y("Tex2 Y Position", Range(0, 1)) = 0.5
        _Tex2Size("Tex2 Size", Range(0.1, 1)) = 1.0
        
        [Space(10)]
        [Header(Mode1)]
        _Size2("Size2", Range(0, 1)) = 0.5
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
            half _x;
            half _y;
            half4 _SelectColor;
            half _CornerSize;
            half _CenterRectSize;
            half _Tex2Y;
            half _Tex2Size;

            // Helper functions
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
                half aspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;

                if (_Mode == 0)
                {
                    // Mode 0: Standard behavior
                    o.uv.x = v.uv.x;
                    o.uv.y = 1.0h - v.uv.y;

                    half3 cameraPos = _WorldSpaceCameraPos;
                    half3 cameraForward = -UNITY_MATRIX_V._m20_m21_m22;
                    half3 fixedPosition = cameraPos + cameraForward;

                    half3 rightDir = normalize(UNITY_MATRIX_V._m00_m01_m02);
                    half3 upDir    = normalize(UNITY_MATRIX_V._m10_m11_m12);

                    half fovY = atan(1.0h / unity_CameraProjection._m11) * 2.0h;
                    half fovX = atan(1.0h / unity_CameraProjection._m00) * 2.0h;

                    half frustumHeight = 2.0h * tan(fovY * 0.5h);
                    half frustumWidth = 2.0h * tan(fovX * 0.5h);

                    half scaleWidth = min(frustumHeight * aspectRatio, frustumWidth) * _Size;
                    half scaleHeight = scaleWidth / aspectRatio;

                    half3 billboardPos = fixedPosition
                        - rightDir * (v.vertex.x * scaleWidth)
                        - upDir    * (v.vertex.y * scaleHeight);

                    o.pos = UnityWorldToClipPos(billboardPos);
                }
                else
                {
                    // Mode 1: Display a selected grid cell with correct aspect ratio
                    float2 gridSize = float2(1.0 / _xMax, 1.0 / _yMax);
                    int cellX = min(int(_x * _xMax), _xMax - 1);
                    int cellY = min(int(_y * _yMax), _yMax - 1);
                    float2 cellMin = float2(cellX, _yMax - 1 - cellY) * gridSize;
                    float2 cellMax = cellMin + gridSize;

                    // Avoid 180-degree rotation of UV coordinates.
                    o.uv = lerp(cellMin, cellMax, float2(v.uv.x, 1.0 - v.uv.y));

                    half3 cameraPos = _WorldSpaceCameraPos;
                    half3 cameraForward = -UNITY_MATRIX_V._m20_m21_m22;
                    half3 fixedPosition = cameraPos + cameraForward;

                    half3 rightDir = normalize(UNITY_MATRIX_V._m00_m01_m02);
                    half3 upDir    = normalize(UNITY_MATRIX_V._m10_m11_m12);

                    half fovY = atan(1.0h / unity_CameraProjection._m11) * 2.0h;
                    half fovX = atan(1.0h / unity_CameraProjection._m00) * 2.0h;

                    half frustumHeight = 2.0h * tan(fovY * 0.5h);
                    half frustumWidth = 2.0h * tan(fovX * 0.5h);

                    half scaleWidth = min(frustumHeight, frustumWidth) * _Size2;
                    half scaleHeight = scaleWidth * (gridSize.y / gridSize.x) /aspectRatio; // Maintain original cell aspect ratio

                    float2 positionOffset = float2((1 - _xPos) - 0.5, _yPos - 0.5) * 2.0 * float2(frustumWidth, frustumHeight);

                    half3 billboardPos = fixedPosition
                        - rightDir * (v.vertex.x * scaleWidth + positionOffset.x)
                        - upDir    * (v.vertex.y * scaleHeight + positionOffset.y);

                    o.pos = UnityWorldToClipPos(billboardPos);
                }

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Discard fragments if not in player or VR view
                if (!(isPlayerView() || isVRView()))
                {
                    discard;
                }

                fixed4 baseColor = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
                fixed4 finalColor = baseColor;

                float2 gridSize = float2(1.0 / _xMax, 1.0 / _yMax);
                int cellX = min(int(_x * _xMax), _xMax - 1);
                int cellY = min(int(_y * _yMax), _yMax - 1);
                float2 cellMin = float2(cellX, _yMax - 1 - cellY) * gridSize;
                float2 cellMax = cellMin + gridSize;

                // Define border (frame) areas.
                float cornerSizeX = _CornerSize * gridSize.x;
                float cornerSizeY = _CornerSize * gridSize.y;
                float2 centerRectMin = cellMin + float2((1 - _CenterRectSize) * gridSize.x * 0.5, (1 - _CenterRectSize) * gridSize.y * 0.5);
                float2 centerRectMax = cellMax - float2((1 - _CenterRectSize) * gridSize.x * 0.5, (1 - _CenterRectSize) * gridSize.y * 0.5);

                bool isInCorner = 
                    (i.uv.x >= cellMin.x && i.uv.x < cellMin.x + cornerSizeX && i.uv.y >= cellMin.y && i.uv.y < cellMin.y + cornerSizeY) ||
                    (i.uv.x >= cellMax.x - cornerSizeX && i.uv.x < cellMax.x && i.uv.y >= cellMin.y && i.uv.y < cellMin.y + cornerSizeY) ||
                    (i.uv.x >= cellMin.x && i.uv.x < cellMin.x + cornerSizeX && i.uv.y >= cellMax.y - cornerSizeY && i.uv.y < cellMax.y) ||
                    (i.uv.x >= cellMax.x - cornerSizeX && i.uv.x < cellMax.x && i.uv.y >= cellMax.y - cornerSizeY && i.uv.y < cellMax.y);

                bool isInCenterRect = i.uv.x >= centerRectMin.x && i.uv.x < centerRectMax.x && i.uv.y >= centerRectMin.y && i.uv.y < centerRectMax.y;

                // Apply the border (frame) blending over the main texture.
                if (isInCorner && !isInCenterRect)
                {
                    finalColor = lerp(finalColor, _SelectColor, _SelectColor.a);
                }

                // Apply Tex2 only in Mode1
                if (_Mode == 1)
                {
                    bool isInSelectedCell = (i.uv.x >= cellMin.x && i.uv.x < cellMax.x &&
                                             i.uv.y >= cellMin.y && i.uv.y < cellMax.y);
                    if (isInSelectedCell)
                    {
                        // Resize Tex2 so that its horizontal span equals the grid cell's width.
                        float cellWidth = gridSize.x;
                        // Determine the aspect ratio of Tex2 (width/height).
                        float tex2Aspect = _Tex2_TexelSize.z / _Tex2_TexelSize.w;
                        // Compute the height that respects Tex2's aspect ratio.
                        float tex2Height = cellWidth / tex2Aspect;
                        // Scale Tex2 size from the center.
                        float scaledWidth = cellWidth * _Tex2Size;
                        float scaledHeight = tex2Height * _Tex2Size;
                        // Center Tex2 vertically within the cell, with vertical offset.
                        float yOffset = cellMin.y + (gridSize.y - scaledHeight) * (1 - _Tex2Y);
                        float2 tex2UV;
                        tex2UV.x = (i.uv.x - (cellMin.x + (cellWidth - scaledWidth) * 0.5)) / scaledWidth;
                        tex2UV.y = (i.uv.y - yOffset) / scaledHeight;

                        fixed4 tex2Sample = UNITY_SAMPLE_TEX2D(_Tex2, tex2UV);
                        // Alpha blending: Tex2 is rendered on top of the current finalColor.
                        fixed3 blendedRGB = tex2Sample.rgb * tex2Sample.a + finalColor.rgb * (1.0 - tex2Sample.a);
                        fixed blendedA = tex2Sample.a + finalColor.a * (1.0 - tex2Sample.a);
                        finalColor = fixed4(blendedRGB, blendedA);
                    }
                }

                return finalColor;
            }
            ENDCG
        }
    }
}