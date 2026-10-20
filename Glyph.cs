using System.Windows;
using System.Windows.Media;

namespace DynamicIsland;


public sealed class Glyph : FrameworkElement
{
    // Setiap ikon digambar pada kanvas 24x24. Fill = bentuk terisi (sudutnya tetap dibulatkan oleh goresan).
    private sealed record Part(string Data, bool Fill = false, double W = 1.9);

    private const string SpeakerBody = "M4.5,9.7 H7.8 L12.2,6 V18 L7.8,14.3 H4.5 Z";
    private const string Wave1 = "M15.6,9.4 A3.8,3.8 0 0 1 15.6,14.6";
    private const string Wave2 = "M18.4,6.8 A7.2,7.2 0 0 1 18.4,17.2";
    private const string Clock = "M20,13 A8,8 0 1 1 4,13 A8,8 0 1 1 20,13 Z M12,8.8 V13 L14.8,14.6";

    private static readonly Dictionary<string, Part[]> Defs = new()
    {
        
        ["calendar"] = new[]
        {
            new Part("M5.5,5.5 H18.5 A2,2 0 0 1 20.5,7.5 V18.5 A2,2 0 0 1 18.5,20.5 H5.5 A2,2 0 0 1 3.5,18.5 V7.5 A2,2 0 0 1 5.5,5.5 Z M3.5,10.5 H20.5"),
            new Part("M8,3 V6.2 M16,3 V6.2", false, 2.2),
        },
        ["music"] = new[]
        {
            new Part("M9,17 V6.2 L19,4 V15 M9,17 A2.5,2.5 0 1 1 4,17 A2.5,2.5 0 1 1 9,17 Z M19,15 A2.5,2.5 0 1 1 14,15 A2.5,2.5 0 1 1 19,15 Z"),
        },
        ["chart"] = new[] { new Part("M6,19 V12.5 M12,19 V5 M18,19 V9.5", false, 3.2) },
        ["sliders"] = new[]
        {
            new Part("M4,8 H6.8 M11.2,8 H20 M11.2,8 A2.2,2.2 0 1 1 6.8,8 A2.2,2.2 0 1 1 11.2,8 Z " +
                     "M4,16 H12.8 M17.2,16 H20 M17.2,16 A2.2,2.2 0 1 1 12.8,16 A2.2,2.2 0 1 1 17.2,16 Z"),
        },
        ["bell"] = new[]
        {
            new Part("M5.5,17.5 C7,16.3 7,14 7,11 A5,5 0 0 1 17,11 C17,14 17,16.3 18.5,17.5 Z M10,20.3 C10.8,21 13.2,21 14,20.3 M12,4 V5.5"),
        },
        ["clock"] = new[] { new Part(Clock), new Part("M9.5,3 H14.5", false, 2.2) },
        ["alarm"] = new[] { new Part(Clock), new Part("M4.2,5.2 L2.8,6.8 M19.8,5.2 L21.2,6.8", false, 2.2) },
        ["folder"] = new[]
        {
            new Part("M3.5,8 A2,2 0 0 1 5.5,6 H9.5 L11.7,8.4 H18.5 A2,2 0 0 1 20.5,10.4 V16.5 A2,2 0 0 1 18.5,18.5 H5.5 A2,2 0 0 1 3.5,16.5 Z"),
        },
        ["file"] = new[]
        {
            new Part("M6.5,3.5 H13 L18.5,9 V18.5 A2,2 0 0 1 16.5,20.5 H7.5 A2,2 0 0 1 5.5,18.5 V4.5 A1,1 0 0 1 6.5,3.5 Z M13,3.5 V9 H18.5"),
        },

        
        ["play"] = new[] { new Part("M8.2,5.8 V18.2 L18.4,12 Z", true, 2.4) },
        ["pause"] = new[] { new Part("M8.6,6.2 V17.8 M15.4,6.2 V17.8", false, 3.6) },
        ["prev"] = new[] { new Part("M17.4,6.4 V17.6 L8.8,12 Z", true, 2.2), new Part("M6.4,6.4 V17.6", false, 2.4) },
        ["next"] = new[] { new Part("M6.6,6.4 V17.6 L15.2,12 Z", true, 2.2), new Part("M17.6,6.4 V17.6", false, 2.4) },
        ["reset"] = new[] { new Part("M1,4 V10 H7 M3.51,15 A9,9 0 1 0 5.64,5.64 L1,10") },

    
        ["volume-0"] = new[] { new Part(SpeakerBody, true, 1.8), new Part("M16.2,9.6 L21,14.4 M21,9.6 L16.2,14.4") },
        ["volume-1"] = new[] { new Part(SpeakerBody, true, 1.8) },
        ["volume-2"] = new[] { new Part(SpeakerBody, true, 1.8), new Part(Wave1) },
        ["volume-3"] = new[] { new Part(SpeakerBody, true, 1.8), new Part(Wave1), new Part(Wave2) },

        
        ["sun"] = new[]
        {
            new Part("M16,12 A4,4 0 1 1 8,12 A4,4 0 1 1 16,12 Z"),
            new Part("M12,2.8 V4.6 M12,19.4 V21.2 M2.8,12 H4.6 M19.4,12 H21.2 M5.5,5.5 L6.8,6.8 M17.2,17.2 L18.5,18.5 M5.5,18.5 L6.8,17.2 M17.2,6.8 L18.5,5.5", false, 2.1),
        },
        ["wifi"] = new[]
        {
            new Part("M3.5,9.6 A12,12 0 0 1 20.5,9.6 M6.6,13.1 A7.6,7.6 0 0 1 17.4,13.1 M9.4,16.4 A3.6,3.6 0 0 1 14.6,16.4"),
            new Part("M12,19.6 H12.01", false, 3),
        },
        ["bluetooth"] = new[] { new Part("M7,7.6 L17,16.4 L12,21 V3 L17,7.6 L7,16.4") },
        ["shot"] = new[]
        {
            new Part("M4,9 V6.5 A2.5,2.5 0 0 1 6.5,4 H9 M15,4 H17.5 A2.5,2.5 0 0 1 20,6.5 V9 M20,15 V17.5 A2.5,2.5 0 0 1 17.5,20 H15 M9,20 H6.5 A2.5,2.5 0 0 1 4,17.5 V15"),
            new Part("M13.5,12 A1.5,1.5 0 1 1 10.5,12 A1.5,1.5 0 1 1 13.5,12 Z", true, 1.2),
        },
        ["lock"] = new[]
        {
            new Part("M7,11 H17 A2,2 0 0 1 19,13 V18 A2,2 0 0 1 17,20 H7 A2,2 0 0 1 5,18 V13 A2,2 0 0 1 7,11 Z M8.5,11 V8 A3.5,3.5 0 0 1 15.5,8 V11"),
        },
        ["gear"] = new[]
        {
            new Part("M15,12 A3,3 0 1 1 9,12 A3,3 0 1 1 15,12 Z M18.4,12 A6.4,6.4 0 1 1 5.6,12 A6.4,6.4 0 1 1 18.4,12 Z", false, 1.8),
            new Part("M12,3 V5.4 M12,18.6 V21 M3,12 H5.4 M18.6,12 H21 M5.6,5.6 L7.3,7.3 M16.7,16.7 L18.4,18.4 M5.6,18.4 L7.3,16.7 M16.7,7.3 L18.4,5.6", false, 3),
        },

        
        ["battery"] = new[]
        {
            new Part("M5,8 H16 A2,2 0 0 1 18,10 V14 A2,2 0 0 1 16,16 H5 A2,2 0 0 1 3,14 V10 A2,2 0 0 1 5,8 Z"),
            new Part("M20.6,11 V13", false, 2.4),
        },
        ["battery-low"] = new[]
        {
            new Part("M5,8 H16 A2,2 0 0 1 18,10 V14 A2,2 0 0 1 16,16 H5 A2,2 0 0 1 3,14 V10 A2,2 0 0 1 5,8 Z"),
            new Part("M20.6,11 V13 M6.4,11.2 V12.8", false, 2.4),
        },
        ["bolt"] = new[] { new Part("M13.2,3.4 L6.2,13.4 H11.4 L10.6,20.6 L17.8,10.4 H12.6 Z", true, 1.8) },
        ["caps"] = new[] { new Part("M12,4.6 L5.2,12 H9.6 V15.8 H14.4 V12 H18.8 Z M9.6,19.4 H14.4") },

       
        ["close"] = new[] { new Part("M7.2,7.2 L16.8,16.8 M16.8,7.2 L7.2,16.8", false, 2.2) },
        ["plus"] = new[] { new Part("M12,5.6 V18.4 M5.6,12 H18.4", false, 2.2) },
        ["chev-l"] = new[] { new Part("M14.5,6 L8.5,12 L14.5,18", false, 2.3) },
        ["chev-r"] = new[] { new Part("M9.5,6 L15.5,12 L9.5,18", false, 2.3) },
        ["arrow-down"] = new[] { new Part("M12,5 V19 M6.5,13.5 L12,19 L17.5,13.5", false, 2.3) },
        ["arrow-up"] = new[] { new Part("M12,19 V5 M6.5,10.5 L12,5 L17.5,10.5", false, 2.3) },
    };

    private static readonly Dictionary<string, Geometry> GeoCache = new();

    private static Geometry GetGeometry(string data)
    {
        if (!GeoCache.TryGetValue(data, out var g))
        {
            g = Geometry.Parse(data);
            g.Freeze();
            GeoCache[data] = g;
        }
        return g;
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(string), typeof(Glyph),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    
    public static readonly DependencyProperty ForegroundProperty = System.Windows.Documents.TextElement.ForegroundProperty.AddOwner(
        typeof(Glyph),
        new FrameworkPropertyMetadata(Brushes.White,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    public string Kind
    {
        get => (string)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(18, 18);

    protected override void OnRender(DrawingContext dc)
    {
        var brush = Foreground;
        if (brush == null || !Defs.TryGetValue(Kind ?? "", out var parts)) return;

        double s = Math.Min(ActualWidth, ActualHeight) / 24.0;
        if (s <= 0) return;

        dc.PushTransform(new TranslateTransform((ActualWidth - 24 * s) / 2, (ActualHeight - 24 * s) / 2));
        dc.PushTransform(new ScaleTransform(s, s));
        foreach (var p in parts)
        {
            var pen = new Pen(brush, p.Fill ? Math.Max(p.W, 1.8) : p.W)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            dc.DrawGeometry(p.Fill ? brush : null, pen, GetGeometry(p.Data));
        }
        dc.Pop();
        dc.Pop();
    }
}
