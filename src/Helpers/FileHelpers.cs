using System.Configuration;
using System.Text.RegularExpressions;
using Pinterest_Image_Downloader.Interfaces;
using Pinterest_Image_Downloader.Models;

namespace Pinterest_Image_Downloader.Helpers;

public class FileHelpers : IFileHelpers
{
    private Dictionary<string, string> ImageQualityMap { get; } = new Dictionary<string, string>
    {
        { "4x", "originals/" }, // Highest Quality - may also have file type other than jpg
        { "3x", "736x/" }, // Should Always be JPG - Fallback
        { "2x", "474x/" },
        { "1x", "236x/" } // Preview Quality
    };

    public string GetImageQuality(string imageQuality)
    {
        return ImageQualityMap[imageQuality];
    }
    
    public List<string> GetAllImageQualityLevels()
    {
        return ImageQualityMap.Values.ToList();
    }

    public string CreateDownloadFolder(string folderPath, string folderName)
    {
        var entireFolderPath = Path.Combine(folderPath, folderName);
        if (!Directory.Exists(entireFolderPath))
        {
            Directory.CreateDirectory(entireFolderPath);
        }
        return entireFolderPath;
    }
    
    private string GetFileType(string imageUrl)
    {
        return Path.GetExtension(imageUrl);
    }
    
    private string GetFileNameWithoutExtension(string imageUrl)
    {
        return Path.GetFileNameWithoutExtension(imageUrl);
    }
    
    public string GetRootProjectDirectory()
    {
        // https://stackoverflow.com/a/11882118
        // This will get the current WORKING directory (i.e. \bin\Debug)
        var workingDirectory = Environment.CurrentDirectory;
        // or: Directory.GetCurrentDirectory() gives the same result

        // This will get the current PROJECT bin directory (ie ../bin/)
        var projectBinDirectory = Directory.GetParent(workingDirectory).Parent.FullName;

        // This will get the current PROJECT directory
        var projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

        return projectDirectory;
    }    
    
    public PinterestFile GenerateDownloadUrl(string imageUrl, string imageQualityLevel, int pinCount)
    {
        // URL Image Folder Structure is first 6 characters of file name split by / every 2 characters then the full file name
        // For example
        // https://i.pinimg.com/236x/2b/30/69/2b30694abd53a6de1799dc44ffbf9f9d.jpg -- Preview Size
        // https://i.pinimg.com/originals/2b/30/69/2b30694abd53a6de1799dc44ffbf9f9d.jpg -- Original High Definition Size
        // Base URL "https://i.pinimg.com/originals/"

        PinterestFile pinterestFile = new PinterestFile();
        var baseUrl = ConfigurationManager.AppSettings["BasePinterestImageUrl"];
        baseUrl += imageQualityLevel;

        var fileName = imageUrl.Substring(imageUrl.LastIndexOf('/') + 1);
        var fileType = GetFileType(imageUrl);
        var fileNameWithoutExtension = GetFileNameWithoutExtension(fileName);
        
        var firstSixChars = fileNameWithoutExtension.Substring(0, 6);
        var splitChars = Regex.Matches(firstSixChars, ".{1,2}");
        
        if (splitChars.Count == 3)
        {
            // pinterestFile.FileName = pinCount + "_" + fileName;
            pinterestFile.FileName = fileName;
            pinterestFile.FileUrl = $"{baseUrl}{splitChars[0].Value}/{splitChars[1].Value}/{splitChars[2].Value}/{fileNameWithoutExtension}{fileType}";
            return pinterestFile;
        }
        else
        {
            pinterestFile.FileName = fileName;
            return pinterestFile;
        }
    }
}