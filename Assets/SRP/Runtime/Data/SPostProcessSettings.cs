using UnityEngine;

namespace srp.data
{
    [System.Serializable]
    public struct SPostProcessSettings
    {
        public float Threshold;
        public float Intensity;

        public SPostProcessSettings(float _Threshold, float _Intensity)
        {
            this.Threshold = _Threshold;
            this.Intensity = _Intensity;
        }
    }
}