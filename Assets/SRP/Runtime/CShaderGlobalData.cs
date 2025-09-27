using UnityEngine;
using UnityEngine.Rendering;

public static class CShaderGlobalKeywordList
{
    public static GlobalKeyword _LIGHT_DIRECTIONAL;
    public static GlobalKeyword _LIGHT_POINT;
    public static GlobalKeyword _LIGHT_SPOT;
    public static GlobalKeyword _NORMAL_MAP;
    public static GlobalKeyword _METALLIC_ROUGHNESS_MAP;

    public static void InitKeywordList()
    {
        CShaderGlobalKeywordList._LIGHT_DIRECTIONAL = GlobalKeyword.Create("_LIGHT_DIRECTIONAL");
        CShaderGlobalKeywordList._LIGHT_POINT = GlobalKeyword.Create("_LIGHT_POINT");
        CShaderGlobalKeywordList._LIGHT_SPOT = GlobalKeyword.Create("_LIGHT_SPOT");
        CShaderGlobalKeywordList._NORMAL_MAP = GlobalKeyword.Create("_NORMAL_MAP");
        CShaderGlobalKeywordList._METALLIC_ROUGHNESS_MAP = GlobalKeyword.Create("_METALLIC_ROUGHNESS_MAP");
    }
}

public static class CShaderConstants
{
    public static int SRP_LightPos = Shader.PropertyToID("SRP_LightPos");
    public static int SRP_LightColor = Shader.PropertyToID("SRP_LightColor");
}