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

    public async Task<List<PinterestFile>> DownloadBoardPinsJob(IEnumerable<string> pins)
    {
        // TODO Implement More Robust Exponential Backoff
        // 3 Seconds
        var delayTimer = 3000;
        var maxRetryAttempts = 3;
        var pinCount = 0;
        List<PinterestFile> failedDownloads = new List<PinterestFile>();
        try
        {
            foreach (var pin in pins)
            {
                var pinParentParentTaskResult = await DownloadPinParentTask(pin, pinCount);

                // Forbidden Error in the case of direct image url is invalid
                // != Forbidden likely implies rate limiting at this stage
                if (!pinParentParentTaskResult.IsSuccess && !pinParentParentTaskResult.ErrorMessage.Contains("Forbidden"))
                {
                    AnsiConsole.WriteLine("Download Task Failed and Was Rate Limited With Error : " + pinParentParentTaskResult.ErrorMessage);
                    
                    for (int i = 0; i < maxRetryAttempts; i++)
                    {
                        var pinRetryDownloadResult = await DownloadPinParentTask(pin, pinCount);
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
    
    private async Task<ServiceResult> DownloadPinParentTask(string imageUrl, int pinCount)
    {
        var downloadFolder = _fileHelpers.CreateDownloadFolder("tmpFolderPath");
        var downloadResult = await DownloadPinTask(imageUrl, downloadFolder, pinCount);
        
        if (downloadResult.IsSuccess)
        {
            return ServiceResult.AsSuccess();
        } else {
            return ServiceResult.AsError(downloadResult.ErrorMessage);
        }
    }

    private async Task<ServiceResult> DownloadPinTask(string imageUrl, string downloadFolder, int pinCount)
    {
        // Cycle Through Each Quality To eventually download an image
        var imageQualityLevels = _fileHelpers.GetAllImageQualityLevels();
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

    public async Task<string> GetStatefulWebPageContent(string boardUrl) { 
        // TODO Move to app.config instead of hardcoding
        var moreLikeThisCSSSelector = ".qQp > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)"; // Selector to stop scrolling
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
            int maxScrollAttempts = 50; // Max scroll attempts to avoid infinite loops
            int scrollAttempts = 0;

            while (true)
            {
                // TODO Redundant Code -- Remove associated
                scrollAttempts++;
                if (scrollAttempts > maxScrollAttempts)
                {
                    break;
                }

                // Check if the "More Like This" section is visible
                if (await page.Locator(moreLikeThisCSSSelector).IsVisibleAsync())
                {
                    Console.WriteLine($"Selector '{moreLikeThisCSSSelector}' is visible. Stopping scroll.");
                    break;
                }

                // Scroll the page down incrementally
                await page.EvaluateAsync($"window.scrollBy(0, {scrollStep});");
                await page.WaitForTimeoutAsync(1000); // Wait to ensure new content loads

                // Capture current page content
                var currentContent = await page.ContentAsync();
                capturedContent.Add(currentContent);

                // Check if new content has been loaded by comparing DOM length
                int currentContentLength = currentContent.Length;
                if (currentContentLength == previousContentLength)
                {
                    Console.WriteLine("No new content detected. Stopping scroll.");
                    break; // Stop scrolling if the content hasn't changed
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

}
 