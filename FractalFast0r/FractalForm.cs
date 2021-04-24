using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace FactalFast0r
{
    public partial class FractalForm : Form
    {
        Device device = null;
        CustomVertex.PositionTextured[] texels;
        short[] indlist;
        const float xMin = -2.2f, xMax = 0.8f, yMax = 1.5f;
        //float xPos = (xMin + xMax) / 2, yPos = 0.360115f, zoom = 0.00001f;
        float xPos = (xMin + xMax) / 2, yPos = 0, zoom = 5f;

        public FractalForm()
        {
            InitializeComponent();
        }

        int rows = 40;
        private void FractalForm_Shown(object sender, EventArgs e)
        {
            //Calculate square
            texels = new CustomVertex.PositionTextured[rows * rows];
            indlist = new short[(rows - 1) * (rows - 1) * 2 * 3];

            int i = 0;
            float yPos = yMax;
            for (int y = 0; y < rows; y++)
            {
                float xPos = xMin;
                for (int x = 0; x < rows; x++)
                {
                    texels[i++] = new CustomVertex.PositionTextured(xPos, yPos, 0, xPos, yPos);
                    xPos += (xMax - xMin) / (rows - 1);
                }
                yPos -= (2 * yMax) / (rows - 1);
            }

            int tribaseindex = 0;
            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < rows - 1; x++)
                {
                    int pointbaseindex = y * rows + x;

                    indlist[tribaseindex++] = (short)(pointbaseindex);
                    indlist[tribaseindex++] = (short)(pointbaseindex + 1);
                    indlist[tribaseindex++] = (short)(pointbaseindex + rows);
                    indlist[tribaseindex++] = (short)(pointbaseindex + 1);
                    indlist[tribaseindex++] = (short)(pointbaseindex + rows + 1);
                    indlist[tribaseindex++] = (short)(pointbaseindex + rows);
                }
            }

            ClientSize = new Size(800, 600);
        }

        int frames = 0;
        Stopwatch stop;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (device == null)
                return;

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            device.Transform.View = Matrix.LookAtLH(new Vector3(xPos, yPos, -zoom), new Vector3(xPos, yPos, 0), new Vector3(0, 1, 0));
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, (float)this.Width / this.Height, 0f, 10f);

            Matrix mat = Matrix.Multiply(device.Transform.View, device.Transform.Projection);
            device.SetVertexShaderConstant(0, mat);

            int its = (int)Math.Round(128.0 / (Math.Pow(zoom, 0.25)));
            device.SetPixelShaderConstant(7, new Vector4(its, its, its, its));

            device.BeginScene();

            device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0, rows * rows, (rows - 1) * (rows - 1) * 2, indlist, true, texels);

            device.EndScene();
            device.Present();

            this.Text = frames + " Fr, " + Math.Round((double)Stopwatch.Frequency / stop.ElapsedTicks) + " FPS, " + its + " Iterations, Camera: ("+xPos+","+yPos+")";
            frames++;
            Console.WriteLine(DateTime.Now + ": " + this.Text);

            stop.Reset();
            stop.Start();
        }

        private void FractalForm_Resize(object sender, EventArgs e)
        {
            timer1.Stop();

            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Copy;
            presentParams.AutoDepthStencilFormat = DepthFormat.D16;
            presentParams.EnableAutoDepthStencil = true;

            device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, presentParams);

            device.RenderState.CullMode = Cull.None;
            device.RenderState.Lighting = false;
            device.VertexFormat = CustomVertex.PositionTextured.Format;

            ShaderFlags shaderFlags = ShaderFlags.None;

            Assembly assembly = Assembly.GetExecutingAssembly();

            string[] d = assembly.GetManifestResourceNames();
            Stream stream = assembly.GetManifestResourceStream("FractalFast0r.Resources.shaders.txt");
            string errors = "";
            ConstantTable consts;
            GraphicsStream gs = ShaderLoader.CompileShaderFromStream(stream, "vs_main", null, null, "vs_3_0", shaderFlags, out errors, out consts);
            VertexShader vs = new VertexShader(device, gs);
            gs.Close();
            device.VertexShader = vs;

            stream.Seek(0, System.IO.SeekOrigin.Begin);

            gs = ShaderLoader.CompileShaderFromStream(stream, "ps_main", null, null, "ps_3_0", shaderFlags, out errors, out consts);
            PixelShader ps = new PixelShader(device, gs);
            gs.Close();
            stream.Close();
            device.PixelShader = ps;

            stop = new Stopwatch();
            stop.Start();
            timer1.Start();
        }

        int lastX = 0, lastY = 0;
        private void FractalForm_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) > 0)
            {
                //broken.  meh.  close nuff for now.
                float zoomFactor = zoom / (0.9f * this.Width * (this.Width / this.Height));
                xPos -= (e.X - lastX) * zoomFactor;
                yPos += (e.Y - lastY) * zoomFactor;
                Console.WriteLine("Moved!");
            }
            lastX = e.X;
            lastY = e.Y;
        }

        private void FractalForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                zoom *= 1f - (e.Delta / 120f / 10f);
            }
        }
    }
}
