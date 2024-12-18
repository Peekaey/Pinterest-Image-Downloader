namespace Pinterest_Image_Downloader.Models;

public class PinterestFile
{
    public string FileName { get; set; }
    public string FileUrl { get; set; }
    public string FolderPath { get; set; }
    
    public string? Error { get; set; }
}