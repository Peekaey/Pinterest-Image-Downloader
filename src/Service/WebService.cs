using System.Configuration;
using System.Diagnostics;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using Pinterest_Image_Downloader.Interfaces;
using Pinterest_Image_Downloader.Models;
using Spectre.Console;
using Configuration = AngleSharp.Configuration;

namespace Pinterest_Image_Downloader.Service;

public class WebService : IWebService
{
    private readonly IParsingService _parsingService;
    private readonly IFileHelpers _fileHelpers;

    public WebService(IParsingService parsingService, IFileHelpers fileHelpers)
    {
        _parsingService = parsingService;
        _fileHelpers = fileHelpers;
    }

    public async Task<IDocument> GetStaticWebPageContent(string url)
    {
        try
        {
            // Get Browser Content
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(url);
            var rawContent = await page.ContentAsync();
            await browser.CloseAsync();

            // Pass content through AngleSharp Parser
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();
            var document = await parser.ParseDocumentAsync(rawContent);
            return document;
        }
        catch (Exception e)
        {
            AnsiConsole.WriteLine("Error occurred when getting static webpage content: " + e.Message);
            throw;
        }
    }
    
    public async Task<List<PinterestFile>> DownloadBoardPinsJob(IEnumerable<string> pins, string parentDownloadFolder, string boardName)
    {
        // TODO Implement More Robust Exponential Backoff
        // 3 Seconds
        var delayTimer = 3000;
        var maxRetryAttempts = 3;
        var pinCount = 0;
        
        // TODO - Allow for Folder Path Selection
        // Temp for Now
        var folderPath = _fileHelpers.GetRootProjectDirectory();
        var downloadFolder = _fileHelpers.CreateDownloadFolder(parentDownloadFolder, boardName);
        List<PinterestFile> failedDownloads = new List<PinterestFile>();
        var imageQualityLevels = _fileHelpers.GetAllImageQualityLevels();
        try
        {
            foreach (var pin in pins)
            {
                var pinParentParentTaskResult = await DownloadPinParentTask(pin, pinCount, downloadFolder, imageQualityLevels);

                // Forbidden Error in the case of direct image url is invalid
                // != Forbidden likely implies rate limiting at this stage
                if (!pinParentParentTaskResult.IsSuccess && !pinParentParentTaskResult.ErrorMessage.Contains("Forbidden"))
                {
                    AnsiConsole.WriteLine("Download Task Failed and Was Rate Limited With Error : " + pinParentParentTaskResult.ErrorMessage);
                    
                    for (int i = 0; i < maxRetryAttempts; i++)
                    {
                        var pinRetryDownloadResult = await DownloadPinParentTask(pin, pinCount, downloadFolder, imageQualityLevels);
                        if (pinRetryDownloadResult.IsSuccess)
                        {
                            break;
                        }
                        else
                        {
                            AnsiConsole.WriteLine($"Rate Timeout {maxRetryAttempts} Failed : " + pinRetryDownloadResult.ErrorMessage);
                            AnsiConsole.WriteLine("Retrying Download Again....");
                            await Task.Delay(delayTimer);
                            delayTimer *= 2;
                        }
                    }
                    failedDownloads.Add(new PinterestFile { FileUrl = pin, Error = pinParentParentTaskResult.ErrorMessage });
                }
                
                if (!pinParentParentTaskResult.IsSuccess)
                {
                    failedDownloads.Add(new PinterestFile { FileUrl = pin, Error = pinParentParentTaskResult.ErrorMessage });
                }
                pinCount++;
            }
        }
        catch (Exception e)
        {
            AnsiConsole.WriteLine("Exception occurred when downloading board pins: " + e.Message);
        }

        return failedDownloads;
    }
    
    private async Task<ServiceResult> DownloadPinParentTask(string imageUrl, int pinCount, string downloadFolder, List<string> imageQualityLevels)
    {

        var downloadResult = await DownloadPinTask(imageUrl, downloadFolder, pinCount, imageQualityLevels);
        
        if (downloadResult.IsSuccess)
        {
            return ServiceResult.AsSuccess();
        } else {
            return ServiceResult.AsError(downloadResult.ErrorMessage);
        }
    }

    private async Task<ServiceResult> DownloadPinTask(string imageUrl, string downloadFolder, int pinCount, List<string> imageQualityLevels)
    {
        // Cycle Through Each Quality To eventually download an image
        var lastErrorMessage = string.Empty;
        
        foreach (var imageLevel in imageQualityLevels)
        {
            var pinFile = _fileHelpers.GenerateDownloadUrl(imageUrl, imageLevel, pinCount);
            
            if (string.IsNullOrWhiteSpace(pinFile.FileUrl))
            {
                AnsiConsole.WriteLine($"PinCount : {pinCount} Skipping quality level {imageLevel}: Invalid URL.");
                continue;
            }
            
            AnsiConsole.WriteLine($"PinCount : {pinCount } Attempting Download Link: " + pinFile.FileUrl);
            pinFile.FolderPath = downloadFolder;
            
            try
            {
                var downloadResult = await DownloadAndSave(pinFile);
                
                if (downloadResult.IsSuccess)
                {
                    return ServiceResult.AsSuccess();
                }

                AnsiConsole.WriteLine($"PinCount : {pinCount } Download failed for {imageLevel}: {downloadResult.ErrorMessage}");
                lastErrorMessage = downloadResult.ErrorMessage;
            }
            catch (Exception e)
            {
                AnsiConsole.WriteLine($"PinCount : {pinCount } Exception during download for {imageLevel}: {e.Message}");
                lastErrorMessage = $"PinCount : {pinCount } Error during download for {imageLevel}: {e.Message}";            }
        }
        if (!string.IsNullOrEmpty(lastErrorMessage))
        {
            AnsiConsole.WriteLine($"All download attempts failed. Last error: {lastErrorMessage}");
        }
        return ServiceResult.AsError(lastErrorMessage);
    }

    private async Task<ServiceResult> DownloadAndSave(PinterestFile pinterestFile)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(pinterestFile.FileUrl);

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult.AsError("Error occurred when downloading file: " + response.ReasonPhrase);
            }

            var savePath = Path.Combine(pinterestFile.FolderPath, pinterestFile.FileName);
            await using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            return ServiceResult.AsSuccess();
        }
    }

    // Used To Get Board Content
    // NGL - Kind of Spaghetti code but works quite well for now

    public async Task<string> GetStatefulWebPageContent(string boardUrl, string cssSelector) { 
        
        try
        {
            var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(boardUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var capturedContent = new HashSet<string>(); // Store unique HTML snapshots
            int previousContentLength = 0; // Track changes in the DOM content length
            int scrollStep = 500; // Scroll step size (pixels)

            while (true)
            {
                // Capture current page content
                var currentContent = await page.ContentAsync();
                capturedContent.Add(currentContent);
                
                // Check if the "More Like This" section is visible
                if (await page.Locator(cssSelector).IsVisibleAsync())
                {
                    Console.WriteLine($"Selector '{cssSelector}' is visible. Stopping scroll.");
                    
                    // Scroll 3 more times and capture content after each scroll
                    for (int i = 0; i < 4; i++)
                    {
                        await page.EvaluateAsync($"window.scrollBy(0, {scrollStep});");
                        await page.WaitForTimeoutAsync(1500); // Allow time for new content to load
                        currentContent = await page.ContentAsync();
                        capturedContent.Add(currentContent);
                    }
                    break;
                }

                // Scroll the page down incrementally
                await page.EvaluateAsync($"window.scrollBy(0, {scrollStep});");
                await page.WaitForTimeoutAsync(1000); // Wait to ensure new content loads
                
                // Check if new content has been loaded by comparing DOM length
                int currentContentLength = currentContent.Length;
                if (currentContentLength == previousContentLength)
                {
                    Console.WriteLine("No new content detected. Stopping scroll.");
                    break;
                }
                previousContentLength = currentContentLength;
            }

            // Combine all captured HTML content into a single structure
            var combinedContent = $"<html><body>{string.Join("\n", capturedContent)}</body></html>";
            await browser.CloseAsync();

            // Captured Content Not Being Parsed Correctly By AngleSharp
            // Currently Parsing With Regex for URLs instead
            // Parse the content using AngleSharp
            // var context = BrowsingContext.New(Configuration.Default);
            // var parser = context.GetService<IHtmlParser>();
            // var document = await parser.ParseDocumentAsync(combinedContent);
            return combinedContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Fetching Web Content: " + ex.Message);
            return string.Empty;
        }
    }
    
    // Used to get public boards from profile
    // Spaghetti code as well
    public async Task<string> GetStatefulProfileWebPageContent(string boardUrl, string cssSelector)
    {
        try
        {
            var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(boardUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var capturedContent = new List<string>(); // Store unique HTML snapshots
            int previousContentLength = 0; // Track changes in the DOM content length
            int scrollStep = 500; // Scroll step size (pixels)

            while (true)
            {
                // Capture current page content
                var currentContent = await page.ContentAsync();
                capturedContent.Add(currentContent);

                // Check if the "More Like This" section is visible
                if (await page.Locator(cssSelector).IsVisibleAsync())
                {
                    Console.WriteLine($"Selector '{cssSelector}' is visible. Capturing additional content before stopping scroll.");

                    // Scroll 3 more times and capture content after each scroll
                    for (int i = 0; i < 4; i++)
                    {
                        await page.EvaluateAsync($"window.scrollBy(0, {scrollStep});");
                        await page.WaitForTimeoutAsync(1500); // Allow time for new content to load
                        currentContent = await page.ContentAsync();
                        capturedContent.Add(currentContent);
                    }

                    break;
                }

                // Scroll the page down incrementally
                await page.EvaluateAsync($"window.scrollBy(0, {scrollStep});");
                await page.WaitForTimeoutAsync(1000); // Wait to ensure new content loads

                // Check if new content has been loaded by comparing DOM length
                int currentContentLength = currentContent.Length;
                if (currentContentLength == previousContentLength)
                {
                    Console.WriteLine("No new content detected. Stopping scroll.");
                    break;
                }
                previousContentLength = currentContentLength;
            }

            // Combine all captured HTML content into a single structure
            var combinedContent = $"<html><body>{string.Join("\n", capturedContent)}</body></html>";
            await browser.CloseAsync();
            return combinedContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Fetching Web Content: " + ex.Message);
            return string.Empty;
        }
    }

}
 