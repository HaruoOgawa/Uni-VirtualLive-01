using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace srp
{
    public class SPassDescriptor
    {
        public List<ShaderTagId> TargetShaderTags = new List<ShaderTagId>();
        public bool DrawSky = false;
        public bool DrawOpaque = true;
        public bool DrawTransparent = true;
        public bool PerObjLight = true;
    }
}