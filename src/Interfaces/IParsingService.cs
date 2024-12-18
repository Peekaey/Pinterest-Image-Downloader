using AngleSharp.Dom;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IParsingService
{
    IEnumerable<string> GetAllBoardPinIds(IDocument webPageContent);
    IEnumerable<string> GetAllBoardContentUrlsByCssQuery(IDocument webPageContent);
    IEnumerable<string> GetAllBoardContentUrlsByUrlStringQuery(string webPageContent);
}