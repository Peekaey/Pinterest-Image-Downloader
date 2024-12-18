using Pinterest_Image_Downloader.Models;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IFilerHelpers
{
    string GetImageQuality(string imageQuality);
    string CreateDownloadFolder(string folderPath);
    PinterestFile GenerateDownloadUrl(string imageUrl, string imageQualityLevel);
}