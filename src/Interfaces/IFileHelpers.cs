using Pinterest_Image_Downloader.Models;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IFileHelpers
{
    string GetImageQuality(string imageQuality);
    List<string> GetAllImageQualityLevels();
    string CreateDownloadFolder(string folderPath, string folderName);
    PinterestFile GenerateDownloadUrl(string imageUrl, string imageQualityLevel, int pinCount);
    string GetRootProjectDirectory();
}