using UnityEngine;
using UnityEngine.Rendering;

public static class CShaderGlobalKeywordList
{
    public static GlobalKeyword UNITY_GAME_VIEW;
    public static GlobalKeyword UNITY_SCENE_VIEW;

    public static GlobalKeyword _LIGHT_DIRECTIONAL;
    public static GlobalKeyword _LIGHT_POINT;
    public static GlobalKeyword _LIGHT_SPOT;
    public static GlobalKeyword _NORMAL_MAP;
    public static GlobalKeyword _METALLIC_ROUGHNESS_MAP;

    public static void InitKeywordList()
    {
        CShaderGlobalKeywordList.UNITY_GAME_VIEW = GlobalKeyword.Create("UNITY_GAME_VIEW");
        CShaderGlobalKeywordList.UNITY_SCENE_VIEW = GlobalKeyword.Create("UNITY_SCENE_VIEW");

        CShaderGlobalKeywordList._LIGHT_DIRECTIONAL = GlobalKeyword.Create("_LIGHT_DIRECTIONAL");
        CShaderGlobalKeywordList._LIGHT_POINT = GlobalKeyword.Create("_LIGHT_POINT");
        CShaderGlobalKeywordList._LIGHT_SPOT = GlobalKeyword.Create("_LIGHT_SPOT");
        CShaderGlobalKeywordList._NORMAL_MAP = GlobalKeyword.Create("_NORMAL_MAP");
        CShaderGlobalKeywordList._METALLIC_ROUGHNESS_MAP = GlobalKeyword.Create("_METALLIC_ROUGHNESS_MAP");
    }
}

public static class CShaderConstants
{
    // Deferred Rendering
    public static int SRP_Deferred_LightPos = Shader.PropertyToID("SRP_Deferred_LightPos");
    public static int SRP_Deferred_LightColor = Shader.PropertyToID("SRP_Deferred_LightColor");
    public static int SRP_Deferred_LightDir = Shader.PropertyToID("SRP_Deferred_LightDir");
    public static int SRP_Deferred_SpotAngle = Shader.PropertyToID("SRP_Deferred_SpotAngle");

    // Foreground Rendering
    public static int SRP_Foreground_MainLightCount = Shader.PropertyToID("SRP_Foreground_MainLightCount");
    public static int SRP_Foreground_MainLightDirArray = Shader.PropertyToID("SRP_Foreground_MainLightDirArray");
    public static int SRP_Foreground_MainLightColorArray = Shader.PropertyToID("SRP_Foreground_MainLightColorArray");

    public static int SRP_Foreground_SubLightCount = Shader.PropertyToID("SRP_Foreground_SubLightCount");
    public static int SRP_Foreground_SubLightPosArray = Shader.PropertyToID("SRP_Foreground_SubLightPosArray");
    public static int SRP_Foreground_SubLightColorArray = Shader.PropertyToID("SRP_Foreground_SubLightColorArray");
    public static int SRP_Foreground_SubLightDirArray = Shader.PropertyToID("SRP_Foreground_SubLightDirArray");
    public static int SRP_Foreground_SubLightAngleArray = Shader.PropertyToID("SRP_Foreground_SubLightAngleArray");

    // Common
    public static int SRP_CameraPos = Shader.PropertyToID("SRP_CameraPos");
    public static int SRP_DirectionLight_ViewProjMatrix_List = Shader.PropertyToID("SRP_DirectionLight_ViewProjMatrix_List");
}