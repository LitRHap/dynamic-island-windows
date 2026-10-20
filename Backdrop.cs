using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace DynamicIsland;


internal sealed class Backdrop : IDisposable
{
    private const int Scale = 4;         
    private const int BlurRadius = 4;    
    private const int BlurPasses = 3;     
    private const double Saturation = 1.55; 
    private const int Lift = 8;             

    private readonly int _w, _h, _sw, _sh;
    private readonly Bitmap _full;
    private readonly Graphics _g;
    private byte[] _src;
    private readonly byte[] _a, _b;

    public WriteableBitmap Output { get; }

    public Backdrop(int widthPx, int heightPx)
    {
        _w = Math.Max(8, widthPx);
        _h = Math.Max(8, heightPx);
        _sw = (_w + Scale - 1) / Scale;
        _sh = (_h + Scale - 1) / Scale;
        _full = new Bitmap(_w, _h, PixelFormat.Format32bppArgb);
        _g = Graphics.FromImage(_full);
        _src = new byte[_w * 4 * _h];
        _a = new byte[_sw * _sh * 4];
        _b = new byte[_sw * _sh * 4];
        Output = new WriteableBitmap(_sw, _sh, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
    }

    public bool Update(int screenX, int screenY)
    {
        try
        {
            _g.CopyFromScreen(screenX, screenY, 0, 0, new System.Drawing.Size(_w, _h), CopyPixelOperation.SourceCopy);

            var bd = _full.LockBits(new Rectangle(0, 0, _w, _h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bd.Stride;
            if (_src.Length < stride * _h) _src = new byte[stride * _h];
            Marshal.Copy(bd.Scan0, _src, 0, stride * _h);
            _full.UnlockBits(bd);

            Downsample(stride);
            for (int i = 0; i < BlurPasses; i++)
            {
                BoxBlur(_a, _b, _sw, _sh, BlurRadius, true);
                BoxBlur(_b, _a, _sw, _sh, BlurRadius, false);
            }
            Vibrance(_a);
            Output.WritePixels(new System.Windows.Int32Rect(0, 0, _sw, _sh), _a, _sw * 4, 0);
            return true;
        }
        catch
        {
            return false; 
        }
    }

    
    private static void Vibrance(byte[] p)
    {
        for (int i = 0; i + 3 < p.Length; i += 4)
        {
            double b = p[i], g = p[i + 1], r = p[i + 2];
            double l = 0.299 * r + 0.587 * g + 0.114 * b;
            p[i]     = (byte)Math.Clamp(l + (b - l) * Saturation + Lift, 0, 255);
            p[i + 1] = (byte)Math.Clamp(l + (g - l) * Saturation + Lift, 0, 255);
            p[i + 2] = (byte)Math.Clamp(l + (r - l) * Saturation + Lift, 0, 255);
        }
    }

    private void Downsample(int stride)
    {
        for (int sy = 0; sy < _sh; sy++)
        {
            int y0 = sy * Scale, y1 = Math.Min(y0 + Scale, _h);
            for (int sx = 0; sx < _sw; sx++)
            {
                int x0 = sx * Scale, x1 = Math.Min(x0 + Scale, _w);
                int b = 0, g = 0, r = 0, n = 0;
                for (int y = y0; y < y1; y++)
                {
                    int row = y * stride;
                    for (int x = x0; x < x1; x++)
                    {
                        int i = row + x * 4;
                        b += _src[i]; g += _src[i + 1]; r += _src[i + 2]; n++;
                    }
                }
                int o = (sy * _sw + sx) * 4;
                _a[o] = (byte)(b / n); _a[o + 1] = (byte)(g / n); _a[o + 2] = (byte)(r / n); _a[o + 3] = 255;
            }
        }
    }

    
    private static void BoxBlur(byte[] src, byte[] dst, int w, int h, int r, bool horizontal)
    {
        int lines = horizontal ? h : w;
        int len = horizontal ? w : h;
        int step = horizontal ? 4 : w * 4;
        int lineStep = horizontal ? w * 4 : 4;
        int win = 2 * r + 1;

        for (int l = 0; l < lines; l++)
        {
            int baseIdx = l * lineStep;
            for (int c = 0; c < 3; c++)
            {
                int sum = 0;
                for (int k = -r; k <= r; k++)
                    sum += src[baseIdx + Math.Clamp(k, 0, len - 1) * step + c];

                for (int i = 0; i < len; i++)
                {
                    dst[baseIdx + i * step + c] = (byte)(sum / win);
                    int add = Math.Min(i + r + 1, len - 1);
                    int sub = Math.Max(i - r, 0);
                    sum += src[baseIdx + add * step + c] - src[baseIdx + sub * step + c];
                }
            }
            for (int i = 0; i < len; i++) dst[baseIdx + i * step + 3] = 255;
        }
    }

    public void Dispose()
    {
        _g.Dispose();
        _full.Dispose();
    }
}
