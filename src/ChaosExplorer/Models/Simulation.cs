using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace ChaosExplorer.Models
{
    public class Simulation
    {
        public ShaderConfig shaderConfig;

        public Particle[] particles;

        private Random rnd = new Random(123);
        public Simulation() 
        {
            shaderConfig = new ShaderConfig();
            shaderConfig.attractor = 0;
            
            shaderConfig.t = 0;
            shaderConfig.dt = 0.001f;

            shaderConfig.fractalWidth = 1920 / 1;
            shaderConfig.fractalHeight = 1040 / 1;
            SetupParticles();
        }

        public void SetupParticles()
        {
            shaderConfig.particlesCount = shaderConfig.fractalWidth * shaderConfig.fractalHeight;
            particles = new Particle[shaderConfig.particlesCount];
            for(int px=0; px< shaderConfig.fractalWidth; px++)
            {
                for(int py=0; py< shaderConfig.fractalHeight; py++)
                {
                    int idx = py * shaderConfig.fractalWidth + px;
                    float x = 0.5f * (px - shaderConfig.fractalWidth / 2);
                    float y = 1;
                    float z = 0.3f * (py - shaderConfig.fractalHeight / 2);
                    particles[idx].position = new Vector3(x, y, z);
                    particles[idx].pixel = new Vector2i(px, py);
                }
            }

            /*
            for(int i=0; i<particles.Length; i++)
            {
                particles[i].position.X = (float)(0.3 * (rnd.NextDouble() - 0.5));
                particles[i].position.Y = 0;
                particles[i].position.Z = (float)(0.3 * (rnd.NextDouble() - 0.5));
            }*/
        }
    }
}
