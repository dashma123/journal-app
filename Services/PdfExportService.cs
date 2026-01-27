using JournalApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Text;
using System.Reflection; 

namespace JournalApp.Services;

public class PdfExportService
{
    
    private void ExportToPdf() 
    {
        // Add temporarily at the start
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        foreach (var name in resourceNames)
        {
            Console.WriteLine(name);
        }
    
       
    }
    public async Task<string> ExportToPdfAsync(JournalEntry entry)
    {
        return await Task.Run(() =>
        {
            // Create PDF document
            var document = new PdfDocument();
            var entryDate = entry.EntryDate != default ? entry.EntryDate : DateTime.Now;
            document.Info.Title = $"Journal Entry - {entryDate:yyyy-MM-dd}";
            
            // Add a page
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            
            var titleFont = new XFont("Roboto", 20, XFontStyle.Bold);
            var dateFont = new XFont("Roboto", 12, XFontStyle.Italic);
            var bodyFont = new XFont("Roboto", 11, XFontStyle.Regular);
            var moodFont = new XFont("Roboto", 10, XFontStyle.Regular);
            
            // Layout settings
            double yPosition = 50;
            double leftMargin = 50;
            double rightMargin = page.Width - 50;
            double maxWidth = rightMargin - leftMargin;
            
            // Draw title
            var title = !string.IsNullOrEmpty(entry.Title) ? entry.Title : "Journal Entry";
            gfx.DrawString(title, titleFont, XBrushes.Black,
                new XRect(leftMargin, yPosition, maxWidth, 30),
                XStringFormats.TopLeft);
            yPosition += 40;
            
            // Draw date
            gfx.DrawString($"Date: {entryDate:MMMM dd, yyyy}", dateFont, XBrushes.Gray,
                new XRect(leftMargin, yPosition, maxWidth, 20),
                XStringFormats.TopLeft);
            yPosition += 30;
            
            // Draw mood if exists
            if (!string.IsNullOrEmpty(entry.PrimaryMood))
            {
                gfx.DrawString($"Mood: {entry.PrimaryMood}", moodFont, XBrushes.DarkBlue,
                    new XRect(leftMargin, yPosition, maxWidth, 20),
                    XStringFormats.TopLeft);
                yPosition += 25;
            }
            
            // Draw separator line
            gfx.DrawLine(XPens.LightGray, leftMargin, yPosition, rightMargin, yPosition);
            yPosition += 20;
            
            // Draw content (with word wrapping)
            if (!string.IsNullOrEmpty(entry.Content))
            {
                DrawWrappedText(gfx, entry.Content, bodyFont, leftMargin, ref yPosition, maxWidth, page);
            }
            
            // Save to file
            var fileName = $"JournalEntry_{entryDate:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf";
            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            
            document.Save(filePath);
            
            return filePath;
        });
    }
    
    private void DrawWrappedText(XGraphics gfx, string text, XFont font, double x, ref double y, double maxWidth, PdfPage page)
    {
        var lineHeight = font.Height * 1.2;
        var words = text.Split(' ');
        var currentLine = new StringBuilder();
        
        foreach (var word in words)
        {
            var testLine = currentLine.Length == 0 ? word : $"{currentLine} {word}";
            var size = gfx.MeasureString(testLine, font);
            
            if (size.Width > maxWidth && currentLine.Length > 0)
            {
                // Draw current line
                gfx.DrawString(currentLine.ToString(), font, XBrushes.Black,
                    new XRect(x, y, maxWidth, lineHeight),
                    XStringFormats.TopLeft);
                y += lineHeight;
                
                // Check if we need a new page
                if (y > page.Height - 50)
                {
                    var newPage = page.Owner.AddPage();
                    gfx = XGraphics.FromPdfPage(newPage);
                    y = 50;
                }
                
                currentLine.Clear();
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                    currentLine.Append(" ");
                currentLine.Append(word);
            }
        }
        
        // Draw last line
        if (currentLine.Length > 0)
        {
            gfx.DrawString(currentLine.ToString(), font, XBrushes.Black,
                new XRect(x, y, maxWidth, lineHeight),
                XStringFormats.TopLeft);
            y += lineHeight;
        }
    }
}