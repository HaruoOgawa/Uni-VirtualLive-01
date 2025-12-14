using UnityEngine;

[System.Serializable]
public struct SRenderSettings
{
    public SPostProcessSettings PostProcessSettings;

    public SRenderSettings(SPostProcessSettings _PostProcessSettings)
    {
        PostProcessSettings = _PostProcessSettings;
    }
}
