using AngleSharp.Dom;
using Pinterest_Image_Downloader.Models;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IWebService
{
    Task<IDocument> GetStaticWebPageContent(string url);
    Task<List<PinterestFile>> DownloadBoardPinsJob(IEnumerable<string> pins, string parentDownloadPath, string boardName);
    Task<string> GetStatefulWebPageContent(string boardUrl,string endContentCSSSelector);
    Task<string> GetStatefulProfileWebPageContent(string boardUrl, string cssSelector);
}