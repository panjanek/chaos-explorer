using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChaosExplorer.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ShaderConfig
    {
        [FieldOffset(0)] public int attractor;

        [FieldOffset(4)] public int particlesCount;

        [FieldOffset(8)] public float dt;

        [FieldOffset(12)] public float t;
    }
}
