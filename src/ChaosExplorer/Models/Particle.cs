using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace ChaosExplorer.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct Particle
    {
        [FieldOffset(0)]
        public Vector3 position;

        // padding to complete vec3 slot
        [FieldOffset(12)]
        private float _pad0;

        // vec3 velocity (aligned to 16 bytes)
        [FieldOffset(16)]
        public Vector3 velocity;

        // padding to complete vec3 slot
        [FieldOffset(28)]
        private float _pad1;

        // ivec2 pixel (8-byte aligned)
        [FieldOffset(32)]
        public Vector2i pixel;

        [FieldOffset(48)]
        public Vector4 color;
    }
}
