// このファイルにUnityEngineがデフォルトで渡してくれるUniformValueを宣言して各シェーダーで使えるようにしておく
//
#ifndef SRP_UNITY_INPUT_LIST
#define SRP_UNITY_INPUT_LIST

float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;

float4x4 glstate_matrix_projection;
#define unity_MatrixP glstate_matrix_projection


float3 _WorldSpaceCameraPos;

// unity_LightDataとunity_LightIndicesはRendererListDesc.rendererConfigurationにPerObjectDataを設定したうえで
// さらにこのようにシェーダーに宣言を書かないと使えない(ビルトイン変数のように勝手に用意してはくれない)
half4 unity_LightData;
half4 unity_LightIndices[2];

#endif