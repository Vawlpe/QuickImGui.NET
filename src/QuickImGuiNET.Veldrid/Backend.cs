#pragma warning disable CS8600, CS8618, CS8602

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
    private ResourceLayout _mainRL;
    private ResourceLayout _ftRL;
    private Pipeline _pipeline;
    private ResourceSet _mainRS;
    private ResourceSet _ftRS;
    private Dictionary<IntPtr, ResourceSet> _textureRS = new();

    // Input stuff
    private bool _controlDown;
    private bool _shiftDown;
    private bool _altDown;
    private bool _winKeyDown;

    // Window stuff
    private int _windowWidth;
    private int _windowHeight;
    private Vector2 _scaleFactor = Vector2.One;
    private readonly ImGuiWindow _mainViewportWindow;
    private readonly Platform_CreateWindow _createWindow;
    private readonly Platform_DestroyWindow _destroyWindow;
    private readonly Platform_GetWindowPos _getWindowPos;
    private readonly Platform_ShowWindow _showWindow;
    private readonly Platform_SetWindowPos _setWindowPos;
    private readonly Platform_SetWindowSize _setWindowSize;
    private readonly Platform_GetWindowSize _getWindowSize;
    private readonly Platform_SetWindowFocus _setWindowFocus;
    private readonly Platform_GetWindowFocus _getWindowFocus;
    private readonly Platform_GetWindowMinimized _getWindowMinimized;
    private readonly Platform_SetWindowTitle _setWindowTitle;

    // ImGui stuff
    private ImGuiIOPtr _IO;
    private ImGuiPlatformIOPtr _platformIO;

    // Texture stuff
    private int _lastAssignedID = 100;

    public unsafe Backend(int width, int height, dynamic gfxbk)
    {
        if (gfxbk == -1)
            gfxbk = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)   ? GraphicsBackend.Vulkan     // Vulkan & MoltenVK are prioritized on all platforms
                : GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal)      ? GraphicsBackend.Metal      // Metal is only available on macOS but is prioritized over OpenGL
                : GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL)     ? GraphicsBackend.OpenGL     // OpenGL is available on all platforms as a default backend if Vulkan is not available
                : GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11) ? GraphicsBackend.Direct3D11 // Direct3D11 is only available on Windows and is only a fallback if OpenGL is not available either
                : throw new System.InvalidOperationException("No supported backend found...");

        // Create window, GraphicsDevice, and all resources necessary to render
        _window = VeldridStartup.CreateWindow(new WindowCreateInfo(50, 50, width, height, WindowState.Normal, "RetroMole"));
        _gd = VeldridStartup.CreateGraphicsDevice(_window, new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true), gfxbk);
        _cl = _gd.ResourceFactory.CreateCommandList();

        _window.Resized += () =>
        {
            _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
            WindowResized(_window.Width, _window.Height);
        };
        _windowWidth = width;
        _windowHeight = height;

        // Load and set icon
        if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Icon.png")))
        {
            var icon_src = SDL2Extensions.SDL_RWFromFile.Invoke(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Icon.png"), "rb");
            _icon = SDL2Extensions.SDL_LoadBMP_RW.Invoke(icon_src, 1);
            SDL2Extensions.SDL_SetWindowIcon.Invoke(_window.SdlWindowHandle, _icon);
        }

        // Set up ImGui
        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        _IO = ImGui.GetIO();

        _IO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        if (gfxbk == GraphicsBackend.Vulkan)
            _IO.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        _platformIO = ImGui.GetPlatformIO();
        ImGuiViewportPtr mainViewport = _platformIO.Viewports[0];
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

        _platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        _platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        _platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        _platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        _platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        _platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        _platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        _platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        _platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);

        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(_platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(_platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowSize));

        unsafe
        {
            _IO.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("QuickImGuiNET.Veldrid (SDL2) Backend").DataPtr;
        }
        _IO.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        _IO.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        _IO.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        _IO.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        _IO.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        _IO.Fonts.AddFontDefault();

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);
        UpdateMonitors();

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public override void Run(
        Action  DrawUICallback,
        float   deltaSeconds        = 1f / 60f,
        Action? UpdateCallback      = null,
        Action? RenderCallback      = null,
        Action? EarlyUpdateCallback = null,
        Action? EarlyRenderCallback = null
    )
    {
        // Main application loop
        while (_window.Exists)
        {
            InputSnapshot input = _window.PumpEvents();
            if (!_window.Exists) { break; }

            EarlyUpdateCallback?.Invoke();
            Update(deltaSeconds, input);
            UpdateCallback?.Invoke();

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
        _gd.Dispose();
        SDL2Extensions.SDL_FreeSurface.Invoke(_icon);
    }

    public void Update(float deltaSeconds, InputSnapshot input)
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

    public void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());

            // Update and Render additional Platform Windows
            if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                for (int i = 1; i < _platformIO.Viewports.Size; i++)
                {
                    ImGuiViewportPtr vp = _platformIO.Viewports[i];
                    ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
                    _cl.SetFramebuffer(window.Swapchain.Framebuffer);
                    RenderImDrawData(vp.DrawData);
                }
            }
        }
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        _IO.DisplaySize = new Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        _IO.DisplayFramebufferScale = _scaleFactor;
        _IO.DeltaTime = deltaSeconds; // DeltaTime is in seconds.

        _platformIO.Viewports[0].Pos = new Vector2(_window.X, _window.Y);
        _platformIO.Viewports[0].Size = new Vector2(_window.Width, _window.Height);
    }

    public void CreateDeviceResources()
    {
        OutputDescription outputDescription = _gd.MainSwapchain.Framebuffer.OutputDescription;
        ResourceFactory factory = _gd.ResourceFactory;
        
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        _indexBuffer.Name = "ImGui.NET Index Buffer";
        RecreateFontDeviceTexture();

        _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        byte[] vertexShaderBytes = LoadEmbeddedShaderCode("imgui-vertex");
        byte[] fragmentShaderBytes = LoadEmbeddedShaderCode("imgui-frag");
        _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, _gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
        _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, _gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

        VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
        {
            new VertexLayoutDescription(
                new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        _mainRL = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        _ftRL = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
            new[] { _mainRL, _ftRL },
            outputDescription,
            ResourceBindingModel.Default);
        _pipeline = factory.CreateGraphicsPipeline(ref pd);

        _mainRS = factory.CreateResourceSet(new(
            _mainRL,
            _projMatrixBuffer,
            _gd.PointSampler
        ));

        _ftRS = factory.CreateResourceSet(new(
            _ftRL,
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

        uint totalVBSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVBSize > _vertexBuffer.SizeInBytes)
        {
            _gd.DisposeWhenIdle(_vertexBuffer);
            _vertexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        uint totalIBSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > _indexBuffer.SizeInBytes)
        {
            _gd.DisposeWhenIdle(_indexBuffer);
            _indexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        Vector2 pos = drawData.DisplayPos;
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = drawData.CmdListsRange[i];

            _cl.UpdateBuffer(
                _vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmd_list.VtxBuffer.Data,
                (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            _cl.UpdateBuffer(
                _indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmd_list.IdxBuffer.Data,
                (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
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
        _cl.SetGraphicsResourceSet(0, _mainRS);

        drawData.ScaleClipRects(_IO.DisplayFramebufferScale);

        // Render command lists
        int vtx_offset = 0;
        int idx_offset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = drawData.CmdListsRange[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException();
                else
                {
                    if (pcmd.TextureId == FontTexture.ID)
                        _cl.SetGraphicsResourceSet(1, _ftRS);
                    else
                        _cl.SetGraphicsResourceSet(1, _textureRS[pcmd.TextureId]);

                    _cl.SetScissorRect(
                        0,
                        (uint)(pcmd.ClipRect.X - pos.X),
                        (uint)(pcmd.ClipRect.Y - pos.Y),
                        (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    _cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)pcmd.VtxOffset + vtx_offset, 0);
                }
            }
            vtx_offset += cmd_list.VtxBuffer.Size;
            idx_offset += cmd_list.IdxBuffer.Size;
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
        Assembly assembly = typeof(Backend).Assembly;
        using (Stream s = assembly.GetManifestResourceStream(resourceName))
        {
            byte[] ret = new byte[s.Length];
            s.Read(ret, 0, (int)s.Length);
            return ret;
        }
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _projMatrixBuffer.Dispose();

        _vertexShader.Dispose();
        _fragmentShader.Dispose();

        _pipeline.Dispose();

        _mainRS.Dispose();
        _ftRS.Dispose();

        _mainRL.Dispose();
        _ftRL.Dispose();

        FontTexture.Texture.Target.Dispose();
        FontTexture.Texture.Dispose();

        Array.ForEach<IntPtr>(Textures.Keys.ToArray(), ID => FreeTexture(ID));
    }

    //--------------------------Windows-----------------------------
    private void CreateWindow(ImGuiViewportPtr vp) { ImGuiWindow window = new ImGuiWindow(_gd, vp); }

    private void DestroyWindow(ImGuiViewportPtr vp)
    {
        if (vp.PlatformUserData != IntPtr.Zero)
        {
            ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            window.Dispose();

            vp.PlatformUserData = IntPtr.Zero;
        }
    }

    private void ShowWindow(ImGuiViewportPtr vp)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Sdl2Native.SDL_ShowWindow(window.Window.SdlWindowHandle);
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        *outPos = new Vector2(window.Window.Bounds.X, window.Window.Bounds.Y);
    }

    private void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        window.Window.X = (int)pos.X;
        window.Window.Y = (int)pos.Y;
    }

    private void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Sdl2Native.SDL_SetWindowSize(window.Window.SdlWindowHandle, (int)size.X, (int)size.Y);
    }

    private unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Rectangle bounds = window.Window.Bounds;
        *outSize = new Vector2(bounds.Width, bounds.Height);
    }

    private void SetWindowFocus(ImGuiViewportPtr vp)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        SDL2Extensions.SDL_RaiseWindow.Invoke(window.Window.SdlWindowHandle);
    }

    private byte GetWindowFocus(ImGuiViewportPtr vp)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        SDL_WindowFlags flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
        return (flags & SDL_WindowFlags.InputFocus) != 0 ? (byte)1 : (byte)0;
    }

    private byte GetWindowMinimized(ImGuiViewportPtr vp)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        SDL_WindowFlags flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
        return (flags & SDL_WindowFlags.Minimized) != 0 ? (byte)1 : (byte)0;
    }

    private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
    {
        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        byte* titlePtr = (byte*)title;
        int count = 0;
        while (titlePtr[count] != 0)
        {
            titlePtr += 1;
        }
        window.Window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void SwapExtraWindows(GraphicsDevice gd)
    {
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        for (int i = 1; i < platformIO.Viewports.Size; i++)
        {
            ImGuiViewportPtr vp = platformIO.Viewports[i];
            ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            SDL2Extensions.SDL_SetWindowIcon(window.Window.SdlWindowHandle, _icon);
            gd.SwapBuffers(window.Swapchain);
        }
    }

    private unsafe void UpdateMonitors()
    {  
        Marshal.FreeHGlobal(_platformIO.NativePtr->Monitors.Data);
        int numMonitors = SDL2Extensions.SDL_GetNumVideoDisplays();
        IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
        _platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
        for (int i = 0; i < numMonitors; i++)
        {
            Rectangle r;
            SDL2Extensions.SDL_GetDisplayUsableBounds(i, &r);
            ImGuiPlatformMonitorPtr monitor = _platformIO.Monitors[i];
            monitor.DpiScale = 1f;
            monitor.MainPos = new Vector2(r.X, r.Y);
            monitor.MainSize = new Vector2(r.Width, r.Height);
            monitor.WorkPos = new Vector2(r.X, r.Y);
            monitor.WorkSize = new Vector2(r.Width, r.Height);
        }
    }

    //-------------------------Bindings-----------------------------
    public override IntPtr BindTexture(Texture Texture)
    {
        VR.Texture t = _gd.ResourceFactory.CreateTexture(new TextureDescription(
            (uint)Texture.Width,
            (uint)Texture.Height,
            1,1,1,
            PixelFormat.B8_G8_R8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D
        ));    
        
        _gd.UpdateTexture(t, Texture.Pixels, 0, 0, 0, (uint)Texture.Width, (uint)Texture.Height, 1, 0, 0);

        var tv = _gd.ResourceFactory.CreateTextureView(t);
        var rs = _gd.ResourceFactory.CreateResourceSet(new(
            _ftRL,
            tv,
            _gd.PointSampler
        ));

        var ID = GetNextImGuiBindingID();

        Textures.Add(ID, tv);
        _textureRS.Add(ID, rs);

        return ID;
    }
    
    public override IntPtr UpdateTexture(Texture Texture)
    {
        VR.Texture t = Textures[Texture.ID].Target;

        _gd.UpdateTexture(t, Texture.Pixels, 0, 0, 0, (uint)Texture.Width, (uint)Texture.Height, 1, 0, 0);

        var tv = _gd.ResourceFactory.CreateTextureView(t);
        var rs = _gd.ResourceFactory.CreateResourceSet(new(
            _ftRL,
            tv,
            _gd.PointSampler
        ));

        Textures[Texture.ID] = tv;
        _textureRS[Texture.ID] = rs;

        return Texture.ID;
    }
    public override void FreeTexture(IntPtr ID)
    {
        TextureView tv = Textures[ID];
        ResourceSet rs = _textureRS[ID];

        Textures.Remove(ID);
        _textureRS.Remove(ID);

        tv.Target.Dispose();  
        tv.Dispose();
        rs.Dispose();        
    }
    private IntPtr GetNextImGuiBindingID() => (IntPtr)(++_lastAssignedID);

    public unsafe void RecreateFontDeviceTexture()
    {
        byte* pixels;
        int width, height, bytesPerPixel;
        _IO.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bytesPerPixel);
        _IO.Fonts.SetTexID(FontTexture.ID);

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

        _IO.Fonts.ClearTexData();
    }

    //-------------------------Bindings-----------------------------
    public override void UpdateInput(dynamic input)
    {
        InputSnapshot snapshot = input;
        _IO.MousePos = snapshot.MousePosition;
        _IO.MouseWheel = snapshot.WheelDelta;

        // Determine if any of the mouse buttons were pressed during this input period, even if they are no longer held.
        bool leftPressed = false;
        bool middlePressed = false;
        bool rightPressed = false;
        foreach (MouseEvent me in snapshot.MouseEvents)
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

        _IO.MouseDown[0] = leftPressed   || snapshot.IsMouseDown(MouseButton.Left);
        _IO.MouseDown[1] = middlePressed || snapshot.IsMouseDown(MouseButton.Right);
        _IO.MouseDown[2] = rightPressed  || snapshot.IsMouseDown(MouseButton.Middle);

        if (_IO.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            unsafe
            {
                int x,y;
                uint buttons = SDL2Extensions.SDL_GetGlobalMouseState(&x, &y);
                _IO.MouseDown[0] = (buttons & 0b0001) != 0;
                _IO.MouseDown[1] = (buttons & 0b0010) != 0;
                _IO.MouseDown[2] = (buttons & 0b0100) != 0;
                _IO.MousePos = new Vector2(x, y);
            }

        IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
        for (int i = 0; i < keyCharPresses.Count; i++)
        {
            char c = keyCharPresses[i];
            _IO.AddInputCharacter(c);
        }

        IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
        for (int i = 0; i < keyEvents.Count; i++)
        {
            KeyEvent keyEvent = keyEvents[i];
            _IO.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
            if (keyEvent.Key == Key.ControlLeft)
                _controlDown = keyEvent.Down;
            if (keyEvent.Key == Key.ShiftLeft)
                _shiftDown = keyEvent.Down;
            if (keyEvent.Key == Key.AltLeft)
                _altDown = keyEvent.Down;
            if (keyEvent.Key == Key.WinLeft)
                _winKeyDown = keyEvent.Down;
        }

        _IO.KeyCtrl  = _controlDown;
        _IO.KeyAlt   = _altDown;
        _IO.KeyShift = _shiftDown;
        _IO.KeySuper = _winKeyDown;

        ImVector<ImGuiViewportPtr> viewports = ImGui.GetPlatformIO().Viewports;
        for (int i = 1; i < viewports.Size; i++)
        {
            ImGuiViewportPtr v = viewports[i];
            ImGuiWindow window = ((ImGuiWindow)GCHandle.FromIntPtr(v.PlatformUserData).Target);
            window.Update();
        }
    }

    private void SetKeyMappings()
    {
        _IO.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
        _IO.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
        _IO.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        _IO.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
        _IO.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
        _IO.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
        _IO.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
        _IO.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
        _IO.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
        _IO.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
        _IO.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
        _IO.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
        _IO.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
        _IO.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
        _IO.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
        _IO.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
        _IO.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
        _IO.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
        _IO.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
        _IO.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        _IO.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
    }
}

#pragma warning restore CS8600, CS8618, CS8602