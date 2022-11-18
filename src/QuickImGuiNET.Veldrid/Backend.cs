using ImGuiNET;
using Veldrid;
using VR = Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace QuickImGuiNET.Veldrid;

public class Backend : QuickImGuiNET.Backend, IDisposable
{
    private GraphicsDevice _gd;
    private Sdl2Window _window;
    private IntPtr _icon;
    private CommandList _cl;
    private bool _frameBegun;
    private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);

    // Veldrid objects
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private DeviceBuffer _projMatrixBuffer;
    private Shader _vertexShader;
    private Shader _fragmentShader;
    private ResourceLayout _mainRl;
    private ResourceLayout _ftRl;
    private Pipeline _pipeline;
    private ResourceSet _mainRs;
    private ResourceSet _ftRs;
    private Dictionary<IntPtr, ResourceSet> _textureRs = new();

    // Input stuff
    private bool _controlDown;
    private bool _shiftDown;
    private bool _altDown;
    private bool _winKeyDown;

    // Window stuff
    private int _windowWidth;
    private int _windowHeight;
    private Vector2 _scaleFactor = Vector2.One;
    private ImGuiWindow _mainViewportWindow;
    private Platform_CreateWindow _createWindow;
    private Platform_DestroyWindow _destroyWindow;
    private Platform_GetWindowPos _getWindowPos;
    private Platform_ShowWindow _showWindow;
    private Platform_SetWindowPos _setWindowPos;
    private Platform_SetWindowSize _setWindowSize;
    private Platform_GetWindowSize _getWindowSize;
    private Platform_SetWindowFocus _setWindowFocus;
    private Platform_GetWindowFocus _getWindowFocus;
    private Platform_GetWindowMinimized _getWindowMinimized;
    private Platform_SetWindowTitle _setWindowTitle;

    // ImGui stuff
    private ImGuiIOPtr _io;
    private ImGuiPlatformIOPtr _platformIo;

    // Texture stuff
    private int _lastAssignedId = 100;

    public Backend()
    {
        //shadow ctor
    }

    public override unsafe void Init(params dynamic[] args)
    {
        int width = args[0];
        int height = args[1]; 
        var gfxbk = args[2];
        if (gfxbk == -1)
            gfxbk = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)   ? GraphicsBackend.Vulkan     // Vulkan & MoltenVK are prioritized on all platforms
                : GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal)      ? GraphicsBackend.Metal      // Metal is only available on macOS but is prioritized over OpenGL
                : GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL)     ? GraphicsBackend.OpenGL     // OpenGL is available on all platforms as a default backend if Vulkan is not available
                : GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11) ? GraphicsBackend.Direct3D11 // Direct3D11 is only available on Windows and is only a fallback if OpenGL is not available either
                : throw new InvalidOperationException("No supported backend found...");

        // Create window, GraphicsDevice, and all resources necessary to render
        _window = VeldridStartup.CreateWindow(new WindowCreateInfo(50, 50, width, height, WindowState.Normal, "QIMGUIN"));
        _gd = VeldridStartup.CreateGraphicsDevice(_window, new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true), (GraphicsBackend)gfxbk);
        _cl = _gd.ResourceFactory.CreateCommandList();

        _window.Resized += () =>
        {
            _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
            WindowResized(_window.Width, _window.Height);
        };
        _windowWidth = width;
        _windowHeight = height;

        // Load and set icon
        if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "", "Icon.png")))
        {
            var iconSrc = SDL2Extensions.SDL_RWFromFile.Invoke(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "", "Icon.png"), "rb");
            _icon = SDL2Extensions.SDL_LoadBMP_RW.Invoke(iconSrc, 1);
            SDL2Extensions.SDL_SetWindowIcon.Invoke(_window.SdlWindowHandle, _icon);
        }

        // Set up ImGui
        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        _io = ImGui.GetIO();

        _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        if ((GraphicsBackend)gfxbk == GraphicsBackend.Vulkan)
            _io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        _platformIo = ImGui.GetPlatformIO();
        ImGuiViewportPtr mainViewport = _platformIo.Viewports[0];
        mainViewport.PlatformHandle = _window.Handle;
        _mainViewportWindow = new ImGuiWindow(_gd, mainViewport, _window);

        _createWindow = CreateWindow;
        _destroyWindow = DestroyWindow;
        _getWindowPos = GetWindowPos;
        _showWindow = ShowWindow;
        _setWindowPos = SetWindowPos;
        _setWindowSize = SetWindowSize;
        _getWindowSize = GetWindowSize;
        _setWindowFocus = SetWindowFocus;
        _getWindowFocus = GetWindowFocus;
        _getWindowMinimized = GetWindowMinimized;
        _setWindowTitle = SetWindowTitle;

        _platformIo.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        _platformIo.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        _platformIo.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        _platformIo.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        _platformIo.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        _platformIo.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        _platformIo.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        _platformIo.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        _platformIo.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);

        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(_platformIo.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(_platformIo.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowSize));

        unsafe
        {
            _io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("QuickImGuiNET.Veldrid (SDL2) Backend").DataPtr;
            _io.NativePtr->BackendRendererName = (byte*)new FixedAsciiString(Enum.GetName(typeof(GraphicsBackend), gfxbk)).DataPtr;
        }
        _io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        _io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        _io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        _io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        _io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        _io.Fonts.AddFontDefault();

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);
        UpdateMonitors();

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public override void Run(
        Action  DrawUICallback,
        float   deltaSeconds          = 1f / 60f,
        Action<float>? UpdateCallback = null,
        Action? RenderCallback        = null,
        Action? EarlyUpdateCallback   = null,
        Action? EarlyRenderCallback   = null
    )
    {
        // Main application loop
        while (_window.Exists)
        {
            InputSnapshot input = _window.PumpEvents();
            if (!_window.Exists) { break; }

            EarlyUpdateCallback?.Invoke();
            Update(deltaSeconds, input);
            UpdateCallback?.Invoke(deltaSeconds);

            DrawUICallback.Invoke();

            _cl.Begin();
            _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
            
            EarlyRenderCallback?.Invoke();
            Render();
            RenderCallback?.Invoke();

            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_gd.MainSwapchain);
            SwapExtraWindows(_gd);
        }

        // Clean up Veldrid resources
        _gd.WaitForIdle();
        Dispose();
        _cl.Dispose();
        //_gd.Dispose(); // For some reason the process hangs on this line  
        SDL2Extensions.SDL_FreeSurface.Invoke(_icon);
    }

    private void Update(float deltaSeconds, InputSnapshot input)
    {
        if (_frameBegun)
        {
            ImGui.Render();
            ImGui.UpdatePlatformWindows();
        }

        SetPerFrameImGuiData(deltaSeconds);
        UpdateInput(input);
        UpdateMonitors();

        _frameBegun = true;
        ImGui.NewFrame();
    }

    private void Render()
    {
        if (!_frameBegun) return;
        _frameBegun = false;
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());

        // Update and Render additional Platform Windows
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) == 0) return;
        ImGui.UpdatePlatformWindows();
        for (var i = 1; i < _platformIo.Viewports.Size; i++)
        {
            var vp = _platformIo.Viewports[i];
            var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            _cl.SetFramebuffer(window.Swapchain.Framebuffer);
            RenderImDrawData(vp.DrawData);
        }
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        _io.DisplaySize = new Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        _io.DisplayFramebufferScale = _scaleFactor;
        _io.DeltaTime = deltaSeconds;

        _platformIo.Viewports[0].Pos = new Vector2(_window.X, _window.Y);
        _platformIo.Viewports[0].Size = new Vector2(_window.Width, _window.Height);
    }

    private void CreateDeviceResources()
    {
        var outputDescription = _gd.MainSwapchain.Framebuffer.OutputDescription;
        var factory = _gd.ResourceFactory;
        
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        _indexBuffer.Name = "ImGui.NET Index Buffer";
        RecreateFontDeviceTexture();

        _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        var vertexShaderBytes = LoadEmbeddedShaderCode("imgui-vertex");
        var fragmentShaderBytes = LoadEmbeddedShaderCode("imgui-frag");
        _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, _gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
        _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, _gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

        var vertexLayouts = new VertexLayoutDescription[]
        {
            new VertexLayoutDescription(
                new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        _mainRl = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        _ftRl = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        var pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
            new[] { _mainRl, _ftRl },
            outputDescription,
            ResourceBindingModel.Default);
        _pipeline = factory.CreateGraphicsPipeline(ref pd);

        _mainRs = factory.CreateResourceSet(new(
            _mainRl,
            _projMatrixBuffer,
            _gd.PointSampler
        ));

        _ftRs = factory.CreateResourceSet(new(
            _ftRl,
            (TextureView)FontTexture.Texture,
            _gd.PointSampler
        ));
    }

    public override void RenderImDrawData(ImDrawDataPtr drawData)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (drawData.CmdListsCount == 0)
            return;

        var totalVbSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVbSize > _vertexBuffer.SizeInBytes)
        {
            _gd.DisposeWhenIdle(_vertexBuffer);
            _vertexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVbSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        var totalIbSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
        if (totalIbSize > _indexBuffer.SizeInBytes)
        {
            _gd.DisposeWhenIdle(_indexBuffer);
            _indexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIbSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        var pos = drawData.DisplayPos;
        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdListsRange[i];

            _cl.UpdateBuffer(
                _vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmdList.VtxBuffer.Data,
                (uint)(cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            _cl.UpdateBuffer(
                _indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmdList.IdxBuffer.Data,
                (uint)(cmdList.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmdList.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmdList.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        var mvp = Matrix4x4.CreateOrthographicOffCenter(
            pos.X,
            pos.X + drawData.DisplaySize.X,
            pos.Y + drawData.DisplaySize.Y,
            pos.Y,
            -1.0f,
            1.0f);

        _cl.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

        _cl.SetVertexBuffer(0, _vertexBuffer);
        _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        _cl.SetPipeline(_pipeline);
        _cl.SetGraphicsResourceSet(0, _mainRs);

        drawData.ScaleClipRects(_io.DisplayFramebufferScale);

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
                _cl.SetGraphicsResourceSet(1, pcmd.TextureId == FontTexture.ID ? _ftRs : _textureRs[pcmd.TextureId]);
                
                _cl.SetScissorRect(
                    0,
                    (uint)(pcmd.ClipRect.X - pos.X),
                    (uint)(pcmd.ClipRect.Y - pos.Y),
                    (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                    (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                _cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idxOffset, (int)pcmd.VtxOffset + vtxOffset, 0);
            }
            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    private byte[] LoadEmbeddedShaderCode(string name) => _gd.ResourceFactory.BackendType switch
    {
        GraphicsBackend.Direct3D11 => GetEmbeddedResourceBytes(name + ".hlsl.bytes"),
        GraphicsBackend.OpenGL     => GetEmbeddedResourceBytes(name + ".glsl"),
        GraphicsBackend.Vulkan     => GetEmbeddedResourceBytes(name + ".spv"),
        GraphicsBackend.Metal      => GetEmbeddedResourceBytes(name + ".metallib"),
        _                          => throw new NotImplementedException()
    };

    private byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        var assembly = typeof(Backend).Assembly;
        using var s = assembly.GetManifestResourceStream(resourceName);
        var ret = new byte[s.Length];
        s.Read(ret, 0, (int)s.Length);
        return ret;
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _projMatrixBuffer.Dispose();

        _vertexShader.Dispose();
        _fragmentShader.Dispose();

        _pipeline.Dispose();

        _mainRs.Dispose();
        _ftRs.Dispose();

        _mainRl.Dispose();
        _ftRl.Dispose();

        FontTexture.Texture.Target.Dispose();
        FontTexture.Texture.Dispose();

        Array.ForEach<IntPtr>(Textures.Keys.ToArray(), FreeTexture);
    }

    //--------------------------Windows-----------------------------
    private void CreateWindow(ImGuiViewportPtr vp) { ImGuiWindow window = new ImGuiWindow(_gd, vp); }

    private void DestroyWindow(ImGuiViewportPtr vp)
    {
        if (vp.PlatformUserData == IntPtr.Zero) return;
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        window.Dispose();

        vp.PlatformUserData = IntPtr.Zero;
    }

    private void ShowWindow(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Sdl2Native.SDL_ShowWindow(window.Window.SdlWindowHandle);
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        *outPos = new Vector2(window.Window.Bounds.X, window.Window.Bounds.Y);
    }

    private void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        window.Window.X = (int)pos.X;
        window.Window.Y = (int)pos.Y;
    }

    private void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Sdl2Native.SDL_SetWindowSize(window.Window.SdlWindowHandle, (int)size.X, (int)size.Y);
    }

    private unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var bounds = window.Window.Bounds;
        *outSize = new Vector2(bounds.Width, bounds.Height);
    }

    private void SetWindowFocus(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        SDL2Extensions.SDL_RaiseWindow.Invoke(window.Window.SdlWindowHandle);
    }

    private byte GetWindowFocus(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
        return (flags & SDL_WindowFlags.InputFocus) != 0 ? (byte)1 : (byte)0;
    }

    private byte GetWindowMinimized(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
        return (flags & SDL_WindowFlags.Minimized) != 0 ? (byte)1 : (byte)0;
    }

    private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var titlePtr = (byte*)title;
        var count = 0;
        while (titlePtr[count] != 0)
        {
            titlePtr += 1;
        }
        window.Window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
    }

    private void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    private void SwapExtraWindows(GraphicsDevice gd)
    {
        for (var i = 1; i < _platformIo.Viewports.Size; i++)
        {
            var vp = _platformIo.Viewports[i];
            var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            SDL2Extensions.SDL_SetWindowIcon(window.Window.SdlWindowHandle, _icon);
            gd.SwapBuffers(window.Swapchain);
        }
    }

    private unsafe void UpdateMonitors()
    {  
        Marshal.FreeHGlobal(_platformIo.NativePtr->Monitors.Data);
        var numMonitors = SDL2Extensions.SDL_GetNumVideoDisplays();
        var data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
        _platformIo.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
        for (var i = 0; i < numMonitors; i++)
        {
            Rectangle r;
            SDL2Extensions.SDL_GetDisplayUsableBounds(i, &r);
            var monitor = _platformIo.Monitors[i];
            monitor.DpiScale = 1f;
            monitor.MainPos = new Vector2(r.X, r.Y);
            monitor.MainSize = new Vector2(r.Width, r.Height);
            monitor.WorkPos = new Vector2(r.X, r.Y);
            monitor.WorkSize = new Vector2(r.Width, r.Height);
        }
    }

    //-------------------------Bindings-----------------------------
    public override IntPtr BindTexture(Texture texture)
    {
        var t = _gd.ResourceFactory.CreateTexture(new TextureDescription(
            (uint)texture.Width,
            (uint)texture.Height,
            1, (uint)(Math.Floor(Math.Log2(Math.Max(texture.Width, texture.Height))) + 1), 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled | TextureUsage.GenerateMipmaps,
            TextureType.Texture2D
        ));

        _gd.UpdateTexture(t, texture.Pixels, 0, 0, 0, (uint)texture.Width, (uint)texture.Height, 1, 0, 0);
        var tempCl = _gd.ResourceFactory.CreateCommandList();
        tempCl.Begin();
        tempCl.GenerateMipmaps(t);
        tempCl.End();
        _gd.SubmitCommands(tempCl);
        tempCl.Dispose();

        var tv = _gd.ResourceFactory.CreateTextureView(t);
        var rs = _gd.ResourceFactory.CreateResourceSet(new(
            _ftRl,
            tv,
            texture.ScaleMode switch
            {
                Texture.ScalingMode.Point => _gd.LinearSampler,
                Texture.ScalingMode.Linear => _gd.PointSampler,
                _ => _gd.LinearSampler
            }
        ));

        var id = GetNextImGuiBindingId();

        Textures.Add(id, tv);
        _textureRs.Add(id, rs);

        return id;
    }
    
    public override IntPtr UpdateTexture(Texture texture)
    {
        VR.Texture t = Textures[texture.ID].Target;

        _gd.UpdateTexture(t, texture.Pixels, 0, 0, 0, (uint)texture.Width, (uint)texture.Height, 1, 0, 0);
        var tempCl = _gd.ResourceFactory.CreateCommandList();
        tempCl.Begin();
        tempCl.GenerateMipmaps(t);
        tempCl.End();
        _gd.SubmitCommands(tempCl);
        tempCl.Dispose();

        var tv = _gd.ResourceFactory.CreateTextureView(t);
        var rs = _gd.ResourceFactory.CreateResourceSet(new(
            _ftRl,
            tv,
            texture.ScaleMode switch
            {
                Texture.ScalingMode.Point => _gd.LinearSampler,
                Texture.ScalingMode.Linear => _gd.PointSampler,
                _ => _gd.LinearSampler
            }
        ));

        Textures[texture.ID] = tv;
        _textureRs[texture.ID] = rs;

        return texture.ID;
    }
    public override void FreeTexture(IntPtr id)
    {
        TextureView tv = Textures[id];
        var rs = _textureRs[id];

        Textures.Remove(id);
        _textureRs.Remove(id);

        tv.Target.Dispose();  
        tv.Dispose();
        rs.Dispose();        
    }
    private IntPtr GetNextImGuiBindingId() => (IntPtr)(++_lastAssignedId);

    private unsafe void RecreateFontDeviceTexture()
    {
        _io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out var bytesPerPixel);
        _io.Fonts.SetTexID(FontTexture.ID);

        var ft = _gd.ResourceFactory.CreateTexture(
            TextureDescription.Texture2D(
                (uint)width,
                (uint)height,
                1,1,
                PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.Sampled
            )
        );
        ft.Name = "ImGui.NET Font Texture";

        _gd.UpdateTexture(
            ft,
            (IntPtr)pixels,
            (uint)(bytesPerPixel * width * height),
            0,0,0,
            (uint)width,
            (uint)height,
            1,0,0
        );

        FontTexture.Texture = _gd.ResourceFactory.CreateTextureView(ft);

        _io.Fonts.ClearTexData();
    }

    //-------------------------Bindings-----------------------------
    public override void UpdateInput(dynamic input)
    {
        InputSnapshot snapshot = input;
        _io.MousePos = snapshot.MousePosition;
        _io.MouseWheel = snapshot.WheelDelta;

        // Determine if any of the mouse buttons were pressed during this input period, even if they are no longer held.
        var leftPressed = false;
        var middlePressed = false;
        var rightPressed = false;
        foreach (var me in snapshot.MouseEvents)
        {
            if (me.Down)
            {
                switch (me.MouseButton)
                {
                    case MouseButton.Left:
                        leftPressed = true;
                        break;
                    case MouseButton.Middle:
                        middlePressed = true;
                        break;
                    case MouseButton.Right:
                        rightPressed = true;
                        break;
                }
            }
        }

        _io.MouseDown[0] = leftPressed   || snapshot.IsMouseDown(MouseButton.Left);
        _io.MouseDown[1] = middlePressed || snapshot.IsMouseDown(MouseButton.Right);
        _io.MouseDown[2] = rightPressed  || snapshot.IsMouseDown(MouseButton.Middle);

        if (_io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            unsafe
            {
                int x,y;
                var buttons = SDL2Extensions.SDL_GetGlobalMouseState(&x, &y);
                _io.MouseDown[0] = (buttons & 0b0001) != 0;
                _io.MouseDown[1] = (buttons & 0b0010) != 0;
                _io.MouseDown[2] = (buttons & 0b0100) != 0;
                _io.MousePos = new Vector2(x, y);
            }

        var keyCharPresses = snapshot.KeyCharPresses;
        foreach (var c in keyCharPresses)
        {
            _io.AddInputCharacter(c);
        }

        var keyEvents = snapshot.KeyEvents;
        foreach (var keyEvent in keyEvents)
        {
            _io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
            switch (keyEvent.Key)
            {
                case Key.ControlLeft:
                    _controlDown = keyEvent.Down;
                    break;
                case Key.ShiftLeft:
                    _shiftDown = keyEvent.Down;
                    break;
                case Key.AltLeft:
                    _altDown = keyEvent.Down;
                    break;
                case Key.WinLeft:
                    _winKeyDown = keyEvent.Down;
                    break;
            }
        }

        _io.KeyCtrl  = _controlDown;
        _io.KeyAlt   = _altDown;
        _io.KeyShift = _shiftDown;
        _io.KeySuper = _winKeyDown;

        var viewports = ImGui.GetPlatformIO().Viewports;
        for (var i = 1; i < viewports.Size; i++)
        {
            var v = viewports[i];
            var window = (ImGuiWindow)GCHandle.FromIntPtr(v.PlatformUserData).Target;
            window.Update();
        }
    }

    private void SetKeyMappings()
    {
        _io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
        _io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
        _io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        _io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
        _io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
        _io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
        _io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
        _io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
        _io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
        _io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
        _io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
        _io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
        _io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
        _io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
        _io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
        _io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
        _io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
        _io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
        _io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
        _io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        _io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
    }
}
