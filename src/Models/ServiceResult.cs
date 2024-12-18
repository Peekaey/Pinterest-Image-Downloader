namespace Pinterest_Image_Downloader.Models;

public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    
    private ServiceResult(bool isSuccess, string errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static ServiceResult AsSuccess()
    {
        return new ServiceResult(true);
    }

    public static ServiceResult AsError(string errorMessage)
    {
        return new ServiceResult(false, errorMessage);
    }
}