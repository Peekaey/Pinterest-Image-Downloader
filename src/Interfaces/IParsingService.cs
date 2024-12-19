using AngleSharp.Dom;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IParsingService
{
    IEnumerable<string> GetAllBoardPinIds(IDocument webPageContent);
    IEnumerable<string> GetAllBoardContentUrlsByCssQuery(IDocument webPageContent);
    IEnumerable<string> GetAllBoardContentUrlsByUrlStringQuery(string webPageContent);
    string GetUserNameFromUserUrl(string profileUrl);
    IEnumerable<string> GetAllBoardUrlsFromProfileByUrlStringQuery(string webPageContent, string username);
    string GetBoardNameFromUrl(string boardUrl);
}