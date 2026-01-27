using PdfSharpCore.Fonts;
using System.Reflection;

namespace JournalApp.Services;

public class CustomFontResolver : IFontResolver
{
    public string DefaultFontName => "Roboto";
    
    public byte[]? GetFont(string faceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Debug: print all available resources to find the exact name
        var allResources = assembly.GetManifestResourceNames();
        System.Diagnostics.Debug.WriteLine("=== Available Embedded Resources ===");
        foreach (var res in allResources)
        {
            System.Diagnostics.Debug.WriteLine(res);
        }
        System.Diagnostics.Debug.WriteLine("====================================");
        
        // Map font names to embedded resources
        var resourceName = faceName switch
        {
            "Roboto" => "JournalApp.Fonts.Roboto-Regular.ttf",
            "Roboto#Bold" => "JournalApp.Fonts.Roboto-Regular.ttf",
            "Roboto#Italic" => "JournalApp.Fonts.Roboto-Regular.ttf",
            _ => "JournalApp.Fonts.Roboto-Regular.ttf"
        };

        System.Diagnostics.Debug.WriteLine($"Trying to load font: {resourceName}");
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            System.Diagnostics.Debug.WriteLine($"Font not found: {resourceName}");
            return null;
        }

        var buffer = new byte[stream.Length];
        var bytesRead = 0;
        var totalBytes = (int)stream.Length;
        
        // Read all bytes from the stream (fixes CA2022 warning)
        while (bytesRead < totalBytes)
        {
            var read = stream.Read(buffer, bytesRead, totalBytes - bytesRead);
            if (read == 0)
                break;
            bytesRead += read;
        }
        
        System.Diagnostics.Debug.WriteLine($"Font loaded successfully: {resourceName}");
        return buffer;
    }
    
    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var fontName = familyName;
        
        if (isBold && isItalic)
            fontName += "#BoldItalic";
        else if (isBold)
            fontName += "#Bold";
        else if (isItalic)
            fontName += "#Italic";
        
        return new FontResolverInfo(fontName);
    }
}