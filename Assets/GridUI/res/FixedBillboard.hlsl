
#include "UnityCG.cginc"

struct FixedBillboard
{
    half3 cameraPos;
    half3 cameraForward;
    half3 position;
    half3 rightDir;
    half3 upDir;
    half fovY;
    half fovX;
    half frustumHeight;
    half frustumWidth;
    half2 frustumSize;
    half width;
    half height;
    half2 size;
    half scale;
};

inline FixedBillboard GetFixedBillboard(half textureAspect, half size)
{
    FixedBillboard data;
    data.cameraPos = _WorldSpaceCameraPos;
    data.cameraForward = -UNITY_MATRIX_V._m20_m21_m22;
    data.position = data.cameraPos + data.cameraForward;

    data.rightDir = normalize(UNITY_MATRIX_V._m00_m01_m02);
    data.upDir = normalize(UNITY_MATRIX_V._m10_m11_m12);

    data.fovY = atan(1.0h / unity_CameraProjection._m11) * 2.0h;
    data.fovX = atan(1.0h / unity_CameraProjection._m00) * 2.0h;

    data.frustumHeight = 2.0h * tan(data.fovY * 0.5h);
    data.frustumWidth = 2.0h * tan(data.fovX * 0.5h);
    data.frustumSize = half2(data.frustumWidth, data.frustumHeight);

    data.width = min(data.frustumHeight * textureAspect, data.frustumWidth) * size;
    data.height = data.width / textureAspect;
    data.size = half2(data.width, data.height);
    data.scale = data.width / textureAspect;

    return data;
}

