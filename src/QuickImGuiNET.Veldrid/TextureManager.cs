using ImGuiNET;
using VR = Veldrid;

namespace QuickImGuiNET.Veldrid;

public class TextureManager : ITextureManager
{
    private Context _ctx;
    private int _lastAssignedId;
    public readonly Dictionary<IntPtr, VR.ResourceSet> TextureRs = new();

    public TextureManager(Context ctx)
    {
        _ctx = ctx;
    }
    public override IntPtr BindTexture(Texture texture)
    {
        var t = _ctx.Renderer.GDevice.ResourceFactory.CreateTexture(new VR.TextureDescription(
            (uint)texture.Width,
            (uint)texture.Height,
            1, (uint)(Math.Floor(Math.Log2(Math.Max(texture.Width, texture.Height))) + 1), 1,
            VR.PixelFormat.R8_G8_B8_A8_UNorm,
            VR.TextureUsage.Sampled | VR.TextureUsage.GenerateMipmaps,
            VR.TextureType.Texture2D
        ));

        _ctx.Renderer.GDevice.UpdateTexture(t, texture.Pixels, 0, 0, 0, (uint)texture.Width, (uint)texture.Height, 1, 0, 0);
        var tempCl = _ctx.Renderer.GDevice.ResourceFactory.CreateCommandList();
        tempCl.Begin();
        tempCl.GenerateMipmaps(t);
        tempCl.End();
        _ctx.Renderer.GDevice.SubmitCommands(tempCl);
        tempCl.Dispose();

        var tv = _ctx.Renderer.GDevice.ResourceFactory.CreateTextureView(t);
        var rs = _ctx.Renderer.GDevice.ResourceFactory.CreateResourceSet(new VR.ResourceSetDescription(
            _ctx.Renderer.FontRl,
            tv,
            texture.ScaleMode switch
            {
                Texture.ScalingMode.Point => _ctx.Renderer.GDevice.LinearSampler,
                Texture.ScalingMode.Linear => _ctx.Renderer.GDevice.PointSampler,
                _ => _ctx.Renderer.GDevice.LinearSampler
            }
        ));

        var id = GetNextImGuiBindingId();

        Textures.Add(id, tv);
        TextureRs.Add(id, rs);

        return id;
    }

    public override IntPtr UpdateTexture(Texture texture)
    {
        VR.Texture t = Textures[texture.ID].Target;

        _ctx.Renderer.GDevice.UpdateTexture(t, texture.Pixels, 0, 0, 0, (uint)texture.Width, (uint)texture.Height, 1, 0, 0);
        var tempCl = _ctx.Renderer.GDevice.ResourceFactory.CreateCommandList();
        tempCl.Begin();
        tempCl.GenerateMipmaps(t);
        tempCl.End();
        _ctx.Renderer.GDevice.SubmitCommands(tempCl);
        tempCl.Dispose();

        var tv = _ctx.Renderer.GDevice.ResourceFactory.CreateTextureView(t);
        var rs = _ctx.Renderer.GDevice.ResourceFactory.CreateResourceSet(new VR.ResourceSetDescription(
            _ctx.Renderer.FontRl,
            tv,
            texture.ScaleMode switch
            {
                Texture.ScalingMode.Point => _ctx.Renderer.GDevice.LinearSampler,
                Texture.ScalingMode.Linear => _ctx.Renderer.GDevice.PointSampler,
                _ => _ctx.Renderer.GDevice.LinearSampler
            }
        ));

        Textures[texture.ID] = tv;
        TextureRs[texture.ID] = rs;

        return texture.ID;
    }

    public override void FreeTexture(IntPtr id)
    {
        VR.TextureView tv = Textures[id];
        var rs = TextureRs[id];

        Textures.Remove(id);
        TextureRs.Remove(id);

        tv.Target.Dispose();
        tv.Dispose();
        rs.Dispose();
    }

    public override IntPtr GetNextImGuiBindingId()
    {
        return (IntPtr)(++_lastAssignedId);
    }

    public override unsafe void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out var bytesPerPixel);
        io.Fonts.SetTexID(FontTexture.ID);

        var ft = _ctx.Renderer.GDevice.ResourceFactory.CreateTexture(
            VR.TextureDescription.Texture2D(
                (uint)width,
                (uint)height,
                1, 1,
                VR.PixelFormat.B8_G8_R8_A8_UNorm,
                VR.TextureUsage.Sampled
            )
        );
        ft.Name = "ImGui.NET Font Texture";

        _ctx.Renderer.GDevice.UpdateTexture(
            ft,
            (IntPtr)pixels,
            (uint)(bytesPerPixel * width * height),
            0, 0, 0,
            (uint)width,
            (uint)height,
            1, 0, 0
        );

        FontTexture.Texture = _ctx.Renderer.GDevice.ResourceFactory.CreateTextureView(ft);

        io.Fonts.ClearTexData();
    }

    public override void Dispose()
    {
        FontTexture.Texture.Target.Dispose();
        FontTexture.Texture.Dispose();

        Array.ForEach(Textures.Keys.ToArray(), FreeTexture); 
    }
}