using System.Diagnostics;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using Pinterest_Image_Downloader.Interfaces;

namespace Pinterest_Image_Downloader.Service;

public class PlaywrightService : IPlaywrightService
{
    public async Task<IDocument> GetStaticWebPageContent(string url)
    {
        try
        {
            // Get Browser Content
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();
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
            Console.WriteLine("Error occurred when getting static webpage content: " + e.Message);
            throw;
        }
    }
}
