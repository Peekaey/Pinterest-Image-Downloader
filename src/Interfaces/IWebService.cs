using AngleSharp.Dom;
using Pinterest_Image_Downloader.Models;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IWebService
{
    Task<IDocument> GetStaticWebPageContent(string url);
    Task<List<PinterestFile>> DownloadBoardPinsJob(IEnumerable<string> pins);
    Task<string> GetStatefulWebPageContent(string boardUrl);
}