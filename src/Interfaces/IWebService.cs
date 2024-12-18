using AngleSharp.Dom;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IPlaywrightService
{
    Task<IDocument> GetStaticWebPageContent(string url);
}