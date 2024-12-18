using Pinterest_Image_Downloader.Models;

namespace Pinterest_Image_Downloader.Interfaces;

public interface IMenuHelper
{
    Task<ServiceResult> ExecuteAction(MenuChoice menuChoice);
}