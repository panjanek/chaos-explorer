using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ChaosExplorer.Models;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using static System.Formats.Asn1.AsnWriter;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Panel = System.Windows.Controls.Panel;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace ChaosExplorer.Gpu
{
    public class OpenGlRenderer
    {
        public int FrameCounter => frameCounter;

        public bool Paused;

        private Panel placeholder;

        private WindowsFormsHost host;

        private GLControl glControl;

        private int frameCounter;

        private Simulation simulation;

        private int particlesBuffer;

        private int particlesCount;

        private readonly int configUbo;

        private readonly int maxGroupsX;

        private int computeProgram;

        private int attractorProgram;

        private int fractalProgram;

        private int projLocation;

        private int fractalStateLocation;

        private int fractalOffsetLocation;

        private int fractalSizeLocation;

        private int dummyVao;

        private int stateTex;

        private int fboA;

        private Vector2 center = new Vector2(0,0);

        private float zoom = 10;

        private OpenTK.Mathematics.Matrix4 projectionMatrix;


        public OpenGlRenderer(Panel placeholder, Simulation sim)
        {
            this.placeholder = placeholder;
            this.simulation = sim;
            host = new WindowsFormsHost();
            host.Visibility = Visibility.Visible;
            host.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            glControl = new GLControl(new GLControlSettings
            {
                API = OpenTK.Windowing.Common.ContextAPI.OpenGL,
                APIVersion = new Version(3, 3), // OpenGL 3.3
                Profile = ContextProfile.Compatability,
                Flags = ContextFlags.Default,
                IsEventDriven = false
            });
            glControl.Dock = DockStyle.Fill;
            host.Child = glControl;
            placeholder.Children.Add(host);
            glControl.Paint += GlControl_Paint;

            //setup required features
            GL.Enable(EnableCap.ProgramPointSize);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            GL.BlendEquation(OpenTK.Graphics.OpenGL.BlendEquationMode.FuncAdd);
            GL.Enable(EnableCap.PointSprite);

            // allocate space for ComputeShaderConfig passed to each compute shader
            configUbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, configUbo);
            int configSizeInBytes = Marshal.SizeOf<ShaderConfig>();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, configSizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, configUbo);
            GL.GetInteger((OpenTK.Graphics.OpenGL.GetIndexedPName)All.MaxComputeWorkGroupCount, 0, out maxGroupsX);

            // create dummy vao
            GL.GenVertexArrays(1, out dummyVao);
            GL.BindVertexArray(dummyVao);

            //create particles and testure buffers
            ResetBuffers();

            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            glControl.Invalidate();

            computeProgram = ShaderUtil.CompileAndLinkComputeShader("solver.comp");
            attractorProgram = ShaderUtil.CompileAndLinkRenderShader("attractor.vert", "attractor.frag");
            projLocation = GL.GetUniformLocation(attractorProgram, "projection");
            if (projLocation == -1) throw new Exception("Uniform 'projection' not found. Shader optimized it out?");

            fractalProgram = ShaderUtil.CompileAndLinkRenderShader("fractal.vert", "fractal.frag");
            fractalStateLocation = GL.GetUniformLocation(fractalProgram, "uState");
            if (fractalStateLocation == -1) throw new Exception("Uniform 'uState' not found. Shader optimized it out?");
            fractalOffsetLocation = GL.GetUniformLocation(fractalProgram, "offset");
            if (fractalOffsetLocation == -1) throw new Exception("Uniform 'offset' not found. Shader optimized it out?");
            fractalSizeLocation = GL.GetUniformLocation(fractalProgram, "size");
            if (fractalSizeLocation == -1) throw new Exception("Uniform 'size' not found. Shader optimized it out?");

            placeholder.SizeChanged += Placeholder_SizeChanged;
        }

        private void ResetBuffers()
        {
            if (particlesCount != simulation.shaderConfig.particlesCount)
            {
                // create buffer for points data
                if (particlesBuffer > 0)
                    GL.DeleteBuffer(particlesBuffer);
                GL.GenBuffers(1, out particlesBuffer);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particlesBuffer);
                particlesCount = simulation.shaderConfig.particlesCount;
                GL.BufferData(BufferTarget.ShaderStorageBuffer, particlesCount * Marshal.SizeOf<Particle>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, particlesBuffer);

                if (stateTex > 0)
                    GL.DeleteTexture(stateTex);
                stateTex = TextureUtil.CreateStateTexture(simulation.shaderConfig.fractalWidth, simulation.shaderConfig.fractalHeight);
                float[] initialState = new float[simulation.shaderConfig.fractalWidth * simulation.shaderConfig.fractalHeight * 4]; //upload initial texture - empty
                GL.BindTexture(TextureTarget.Texture2D, stateTex);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, simulation.shaderConfig.fractalWidth, simulation.shaderConfig.fractalHeight, PixelFormat.Rgba, PixelType.Float, initialState);
                fboA = TextureUtil.CreateFboForTexture(stateTex);
                GL.ClearColor(0f, 0f, 0f, 0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
            }

            //upload initial data
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, particlesBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, simulation.shaderConfig.particlesCount * Marshal.SizeOf<Particle>(), simulation.particles);
        }

        private void Placeholder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (glControl.Width <= 0 || glControl.Height <= 0)
                return;

            if (!glControl.Context.IsCurrent)
                glControl.MakeCurrent();

            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            glControl.Invalidate();
        }

        private void GlControl_Paint(object? sender, PaintEventArgs e)
        {
            // draw pointcloud
            GL.Enable(EnableCap.ProgramPointSize);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            GL.BlendEquation(OpenTK.Graphics.OpenGL.BlendEquationMode.FuncAdd);
            GL.Enable(EnableCap.PointSprite);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.UseProgram(attractorProgram);
            GL.BindVertexArray(dummyVao);
            projectionMatrix = GetProjectionMatrix();
            GL.UniformMatrix4(projLocation, false, ref projectionMatrix);
            GL.DrawArrays(PrimitiveType.Points, 0, particlesCount);

            // draw plot from texture
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);
            GL.UseProgram(fractalProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, stateTex);
            GL.Uniform1(fractalStateLocation, 0);
            GL.Uniform2(fractalOffsetLocation, new Vector2(0.0f, 0.0f));
            var plotSize = new Vector2(0.3f, 0.3f);
            GL.Uniform2(fractalSizeLocation, plotSize);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            glControl.SwapBuffers();
            frameCounter++;
 
        }

        public void Draw()
        {
            if (Application.Current.MainWindow.WindowState == System.Windows.WindowState.Minimized)
                return;

            lock (simulation)
            {
                if (particlesCount == 0 || particlesCount != simulation.shaderConfig.particlesCount)
                    ResetBuffers();

                //upload config
                simulation.shaderConfig.t += simulation.shaderConfig.dt;
                int configSizeInBytes = Marshal.SizeOf<ShaderConfig>();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, configUbo);
                GL.BufferSubData(
                    BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero,
                    Marshal.SizeOf<ShaderConfig>(),
                    ref simulation.shaderConfig
                );
            }

            //compute
            if (!Paused)
            {
                GL.UseProgram(computeProgram);
                GL.BindImageTexture(2, stateTex, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
                int dispatchGroupsX = (particlesCount + ShaderUtil.LocalSizeX - 1) / ShaderUtil.LocalSizeX;
                if (dispatchGroupsX > maxGroupsX)
                    dispatchGroupsX = maxGroupsX;
                GL.DispatchCompute(dispatchGroupsX, 1, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit | MemoryBarrierFlags.ShaderImageAccessBarrierBit);//| MemoryBarrierFlags.ShaderImageAccessBarrierBit);
                //GL.CopyImageSubData(plotTexBack, ImageTarget.Texture2D, 0, 0, 0, 0, plotTexFront, ImageTarget.Texture2D, 0, 0, 0, 0, scene.shaderConfig.plotWidth, scene.shaderConfig.plotHeight, 1);
            }

            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            glControl.Invalidate();
        }

        private Matrix4 GetProjectionMatrix()
        {
            // rescale by windows display scale setting to match WPF coordinates ??
            var w = (float)(glControl.Width / zoom) / 2;
            var h = (float)(glControl.Height / zoom) / 2;
            var translate = Matrix4.CreateTranslation(-center.X, -center.Y, 0.0f);
            var ortho = Matrix4.CreateOrthographicOffCenter(-w, w, -h, h, -1f, 1f);
            var matrix = translate * ortho;
            return matrix;
        }
    }
}
