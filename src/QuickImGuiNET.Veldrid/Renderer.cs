using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using Veldrid.StartupUtilities;
using VR = Veldrid;

namespace QuickImGuiNET.Veldrid;

public class Renderer : IRenderer
{
    private Context _ctx;
    
    public bool FrameBegun;
    public readonly Vector2 ScaleFactor = Vector2.One;
    public VR.GraphicsDevice GDevice;
    public VR.CommandList CmdList;
    public VR.Pipeline Pipeline;
    public VR.Shader FragmentShader;
    public VR.Shader VertexShader;
    public VR.DeviceBuffer IndexBuffer;
    public VR.DeviceBuffer VertexBuffer;
    public VR.DeviceBuffer ProjMatrixBuffer;
    public VR.ResourceLayout MainRl;
    public VR.ResourceSet MainRs;
    public VR.ResourceLayout FontRl;
    public VR.ResourceSet FontRs;

    public Renderer(Context ctx)
    {
        _ctx = ctx;
    }

    public void SetPerFrameImGuiData(float deltaSeconds)
    {
        _ctx.Io.DisplaySize = new Vector2(
            _ctx.WindowManager.MainWindow.Width / ScaleFactor.X,
            _ctx.WindowManager.MainWindow.Height / ScaleFactor.Y);
        _ctx.Io.DisplayFramebufferScale = ScaleFactor;
        _ctx.Io.DeltaTime = deltaSeconds;

        _ctx.PlatformIo.Viewports[0].Pos = new Vector2(_ctx.WindowManager.MainWindow.X, _ctx.WindowManager.MainWindow.Y);
        _ctx.PlatformIo.Viewports[0].Size = new Vector2(_ctx.WindowManager.MainWindow.Width, _ctx.WindowManager.MainWindow.Height);
    }

    public void CreateDeviceResources()
    {
        GDevice = VeldridStartup.CreateGraphicsDevice(_ctx.WindowManager.MainWindow,
            new VR.GraphicsDeviceOptions(true, null, true, VR.ResourceBindingModel.Improved, true, true),
            _ctx.GraphicsBackend);
        CmdList = GDevice.ResourceFactory.CreateCommandList();
        
        var outputDescription = GDevice.MainSwapchain.Framebuffer.OutputDescription;
        var factory = GDevice.ResourceFactory;

        VertexBuffer =
            factory.CreateBuffer(new VR.BufferDescription(10000, VR.BufferUsage.VertexBuffer | VR.BufferUsage.Dynamic));
        VertexBuffer.Name = "ImGui.NET Vertex Buffer";
        IndexBuffer =
            factory.CreateBuffer(new VR.BufferDescription(2000, VR.BufferUsage.IndexBuffer | VR.BufferUsage.Dynamic));
        IndexBuffer.Name = "ImGui.NET Index Buffer";
        _ctx.TextureManager.RecreateFontDeviceTexture();

        ProjMatrixBuffer =
            factory.CreateBuffer(new VR.BufferDescription(64, VR.BufferUsage.UniformBuffer | VR.BufferUsage.Dynamic));
        ProjMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        var vertexShaderBytes = LoadEmbeddedShaderCode("imgui-vertex");
        var fragmentShaderBytes = LoadEmbeddedShaderCode("imgui-frag");
        VertexShader = factory.CreateShader(new VR.ShaderDescription(VR.ShaderStages.Vertex, vertexShaderBytes,
            GDevice.BackendType == VR.GraphicsBackend.Metal ? "VS" : "main"));
        FragmentShader = factory.CreateShader(new VR.ShaderDescription(VR.ShaderStages.Fragment, fragmentShaderBytes,
            GDevice.BackendType == VR.GraphicsBackend.Metal ? "FS" : "main"));

        var vertexLayouts = new[]
        {
            new VR.VertexLayoutDescription(
                new VR.VertexElementDescription("in_position", VR.VertexElementSemantic.Position,
                    VR.VertexElementFormat.Float2),
                new VR.VertexElementDescription("in_texCoord", VR.VertexElementSemantic.TextureCoordinate,
                    VR.VertexElementFormat.Float2),
                new VR.VertexElementDescription("in_color", VR.VertexElementSemantic.Color,
                    VR.VertexElementFormat.Byte4_Norm))
        };

        MainRl = factory.CreateResourceLayout(new VR.ResourceLayoutDescription(
            new VR.ResourceLayoutElementDescription("ProjectionMatrixBuffer", VR.ResourceKind.UniformBuffer,
                VR.ShaderStages.Vertex),
            new VR.ResourceLayoutElementDescription("MainSampler", VR.ResourceKind.Sampler, VR.ShaderStages.Fragment)));
        FontRl = factory.CreateResourceLayout(new VR.ResourceLayoutDescription(
            new VR.ResourceLayoutElementDescription("MainTexture", VR.ResourceKind.TextureReadOnly,
                VR.ShaderStages.Fragment),
            new VR.ResourceLayoutElementDescription("MainSampler", VR.ResourceKind.Sampler, VR.ShaderStages.Fragment)));

        var pd = new VR.GraphicsPipelineDescription(
            VR.BlendStateDescription.SingleAlphaBlend,
            new VR.DepthStencilStateDescription(false, false, VR.ComparisonKind.Always),
            new VR.RasterizerStateDescription(VR.FaceCullMode.None, VR.PolygonFillMode.Solid, VR.FrontFace.Clockwise,
                false, true),
            VR.PrimitiveTopology.TriangleList,
            new VR.ShaderSetDescription(vertexLayouts, new[] { VertexShader, FragmentShader }),
            new[] { MainRl, FontRl },
            outputDescription,
            VR.ResourceBindingModel.Default);
        Pipeline = factory.CreateGraphicsPipeline(ref pd);

        MainRs = factory.CreateResourceSet(new VR.ResourceSetDescription(
            MainRl,
            ProjMatrixBuffer,
            GDevice.PointSampler
        ));

        FontRs = factory.CreateResourceSet(new VR.ResourceSetDescription(
            FontRl,
            (VR.TextureView)_ctx.TextureManager.FontTexture.Texture,
            GDevice.PointSampler
        ));
    }

    public void RenderImDrawData(ImDrawDataPtr drawData)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (drawData.CmdListsCount == 0)
            return;

        var totalVbSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVbSize > VertexBuffer.SizeInBytes)
        {
            GDevice.DisposeWhenIdle(VertexBuffer);
            VertexBuffer = GDevice.ResourceFactory.CreateBuffer(new VR.BufferDescription((uint)(totalVbSize * 1.5f),
                VR.BufferUsage.VertexBuffer | VR.BufferUsage.Dynamic));
        }

        var totalIbSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
        if (totalIbSize > IndexBuffer.SizeInBytes)
        {
            GDevice.DisposeWhenIdle(IndexBuffer);
            IndexBuffer = GDevice.ResourceFactory.CreateBuffer(new VR.BufferDescription((uint)(totalIbSize * 1.5f),
                VR.BufferUsage.IndexBuffer | VR.BufferUsage.Dynamic));
        }

        var pos = drawData.DisplayPos;
        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdListsRange[i];

            CmdList.UpdateBuffer(
                VertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmdList.VtxBuffer.Data,
                (uint)(cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            CmdList.UpdateBuffer(
                IndexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmdList.IdxBuffer.Data,
                (uint)(cmdList.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmdList.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmdList.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        var mvp = Matrix4x4.CreateOrthographicOffCenter(
            pos.X, pos.X + drawData.DisplaySize.X,
            pos.Y + drawData.DisplaySize.Y, pos.Y,
            -1.0f, 1.0f);

        CmdList.UpdateBuffer(ProjMatrixBuffer, 0, ref mvp);

        CmdList.SetVertexBuffer(0, VertexBuffer);
        CmdList.SetIndexBuffer(IndexBuffer, VR.IndexFormat.UInt16);
        CmdList.SetPipeline(Pipeline);
        CmdList.SetGraphicsResourceSet(0, MainRs);

        drawData.ScaleClipRects(_ctx.Io.DisplayFramebufferScale);

        // Render command lists
        var vtxOffset = 0;
        var idxOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdListsRange[n];
            for (var cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                var pcmd = cmdList.CmdBuffer[cmdI];
                if (pcmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException();
                CmdList.SetGraphicsResourceSet(1, pcmd.TextureId == _ctx.TextureManager.FontTexture.ID ? FontRs : _ctx.TextureManager.TextureRs[pcmd.TextureId]);

                CmdList.SetScissorRect(
                    0,
                    (uint)(pcmd.ClipRect.X - pos.X),
                    (uint)(pcmd.ClipRect.Y - pos.Y),
                    (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                    (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                CmdList.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idxOffset, (int)pcmd.VtxOffset + vtxOffset,
                    0);
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    public byte[] LoadEmbeddedShaderCode(string name)
    {
        return GDevice.ResourceFactory.BackendType switch
        {
            VR.GraphicsBackend.Direct3D11 => GetEmbeddedResourceBytes(name + ".hlsl.bytes"),
            VR.GraphicsBackend.OpenGL => GetEmbeddedResourceBytes(name + ".glsl"),
            VR.GraphicsBackend.Vulkan => GetEmbeddedResourceBytes(name + ".spv"),
            VR.GraphicsBackend.Metal => GetEmbeddedResourceBytes(name + ".metallib"),
            _ => throw new NotImplementedException()
        };
    }

    public byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        var assembly = typeof(Context).Assembly;
        using var s = assembly.GetManifestResourceStream(resourceName);
        var ret = new byte[s.Length];
        s.Read(ret, 0, (int)s.Length);
        return ret;
    }

    public unsafe void UpdateMonitors()
    {
        Marshal.FreeHGlobal(_ctx.PlatformIo.NativePtr->Monitors.Data);
        var numMonitors = SDL2Extensions.SDL_GetNumVideoDisplays();
        var data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
        _ctx.PlatformIo.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
        for (var i = 0; i < numMonitors; i++)
        {
            VR.Rectangle r;
            SDL2Extensions.SDL_GetDisplayUsableBounds(i, &r);
            var monitor = _ctx.PlatformIo.Monitors[i];
            monitor.DpiScale = 1f;
            monitor.MainPos = new Vector2(r.X, r.Y);
            monitor.MainSize = new Vector2(r.Width, r.Height);
            monitor.WorkPos = new Vector2(r.X, r.Y);
            monitor.WorkSize = new Vector2(r.Width, r.Height);
        }
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
        ProjMatrixBuffer.Dispose();
        VertexShader.Dispose();
        FragmentShader.Dispose();
        Pipeline.Dispose();
        MainRs.Dispose();
        FontRs.Dispose();
        MainRl.Dispose();
        FontRl.Dispose();
        CmdList.Dispose();
        //GDevice.Dispose(); // For some reason the process hangs on this line  
    }
}