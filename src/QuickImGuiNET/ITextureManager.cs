namespace QuickImGuiNET;

public abstract class ITextureManager
{
    public (IntPtr ID, dynamic Texture) FontTexture;
    public Dictionary<IntPtr, dynamic> Textures = new();

    public abstract IntPtr BindTexture(Texture texture);
    public abstract IntPtr UpdateTexture(Texture texture);
    public abstract void FreeTexture(IntPtr id);
    public abstract IntPtr GetNextImGuiBindingId();
    public abstract unsafe void RecreateFontDeviceTexture();
    public abstract void Dispose();
}