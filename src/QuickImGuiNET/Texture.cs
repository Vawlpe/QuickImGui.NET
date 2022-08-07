using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QuickImGuiNET;

public class Texture
{
    public int Width;
    public int Height;
    public IntPtr ID;
    public ScalingMode ScaleMode;

    //------------------------------------------------------------------------------------------------------------------------
    private Rgba32[] _Pixels;
    public Rgba32 this[int x, int y]
    {
        get => _Pixels[x + y * Width];
        set {
            var old = (Texture)MemberwiseClone();
            _Pixels[x + y * Width] = value;
            OnChanged?.Invoke(new OnTextureChangedEventArgs(old, this));
        }
    }
    public Rgba32[] Pixels
    {
        get => _Pixels;
        set {
            var old = (Texture)MemberwiseClone();
            _Pixels = value;
            OnChanged?.Invoke(new OnTextureChangedEventArgs(old, this));
        }
    }

    //----------------------------------------------------------------------------------------------------------------------
    public event Action<OnTextureChangedEventArgs>? OnChanged;
    public class OnTextureChangedEventArgs : EventArgs
    {
        public Texture oldT;
        public Texture newT;
        public OnTextureChangedEventArgs(Texture oldT, Texture newT) { this.oldT = oldT; this.newT = newT; }
    }
    
    //----------------------------------------------------------------------------------------------------------------------
    public static Texture Bind(string FilePath, Backend backend, ScalingMode? scaleMode = null)
    {
        var texture = new Texture(FilePath, scaleMode ?? ScalingMode.Linear);
        texture.ID = backend.BindTexture(texture);

        texture.OnChanged += (e) => backend.UpdateTexture(e.newT);

        return texture;
    }
    private Texture(string FilePath, ScalingMode scaleMode)
    {
        Image<Rgba32> img = Image.Load<Rgba32>(FilePath);

        Width = img.Width;
        Height = img.Height;

        _Pixels = new Rgba32[Width * Height];
        img.CopyPixelDataTo(Pixels);
        ScaleMode = scaleMode;
    }

    //----------------------------------------------------------------------------------------------------------------------
    public enum ScalingMode
    {
        Point,
        Linear
    }
}
