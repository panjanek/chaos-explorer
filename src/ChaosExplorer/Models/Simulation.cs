using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            shaderConfig.particlesCount = 10000;
            shaderConfig.t = 0;
            shaderConfig.dt = 0.01f;
            SetupParticles();
        }

        public void SetupParticles()
        {
            particles = new Particle[shaderConfig.particlesCount];
            for(int i=0; i<particles.Length; i++)
            {
                particles[i].position.X = (float)(3 * (rnd.NextDouble() - 0.5));
                particles[i].position.Y = (float)(3 * (rnd.NextDouble() - 0.5));
                particles[i].position.Z = (float)(3 * (rnd.NextDouble() - 0.5));
            }
        }
    }
}
