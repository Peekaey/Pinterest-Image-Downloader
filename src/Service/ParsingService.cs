using AngleSharp.Dom;
using Pinterest_Image_Downloader.Interfaces;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Pinterest_Image_Downloader.Service;


public class ParsingService : IParsingService
{
    
    // Outdated
    // Used for Parsing with AngleSharp
    public IEnumerable<string> GetAllBoardPinIds(IDocument webPageContent)
    {
        var boardDivContainerCssSelector = ConfigurationManager.AppSettings["boardDivContainerCssSelector"];
        var boardPinIdSelector = ConfigurationManager.AppSettings["BoardPinIdSelector"];
        
        if (boardDivContainerCssSelector == null || boardPinIdSelector == null)
        {
            throw new ConfigurationErrorsException("Configuration settings for boardDivContainerCssSelector and BoardPinIdSelector are missing.");
        }
        
        var boardDivContainer = webPageContent.QuerySelector(boardDivContainerCssSelector);
        var pinElements = boardDivContainer.QuerySelectorAll(boardPinIdSelector);
        
        var pinIds = new List<string>();
        foreach (var pinElement in pinElements)
        {
            var pinId = pinElement.GetAttribute("data-test-pin-id");
            if (pinId != null)
            {
                pinIds.Add(pinId);
            }
        }
        return pinIds;
    }

    // Outdated
    // Used for Parsing with AngleSharp
    public IEnumerable<string> GetAllBoardContentUrlsByCssQuery(IDocument webPageContent)
    {
        var boardImgCssSelector = ConfigurationManager.AppSettings["BoardImgCSSSelector"];
        var boardVideoCssSelector = ConfigurationManager.AppSettings["BoardVideoCSSSelector"];
        var boardDivContainerCssSelector = ConfigurationManager.AppSettings["BoardDivContainerCSSSelector"];

        if (boardImgCssSelector == null || boardVideoCssSelector == null || boardDivContainerCssSelector == null)
        {
            throw new ConfigurationErrorsException("Configuration settings for BoardImgCSSSelector, BoardVideoCSSSelector, and BoardDivContainerCSSSelector are missing.");
        }
        
        var boardDivContainer = webPageContent.QuerySelector(boardDivContainerCssSelector);
        var srcValues = new List<string>();
        if (boardDivContainer != null)
        {
            // Get Img/Gif Contents
            var imgElements = boardDivContainer.QuerySelectorAll(boardImgCssSelector);
            foreach (var imgElement in imgElements)
            {
                var src = imgElement.GetAttribute("src");
                if (src != null)
                {
                    srcValues.Add(src);
                }
            }
            // Get Video Contents
            var videoElements = boardDivContainer.QuerySelectorAll(boardVideoCssSelector);
            foreach (var videoElement in videoElements)
            {
                var src = videoElement.GetAttribute("src");
                if (src != null)
                {
                    srcValues.Add(src);
                }
            }
        }
        return srcValues;
    }
    

    public IEnumerable<string> GetAllBoardContentUrlsByUrlStringQuery(string webPageContent)
    {
        if (webPageContent == null)
        {
            throw new ArgumentNullException(nameof(webPageContent), "The web page content cannot be null.");
        }
        
        var extractedUrls = new HashSet<string>(); 
        
        // Redundant using two different patterns as its unnecessary calls, however two pattern matchings help with debugging
        var basePattern = @"https:\/\/i\.pinimg\.com\/[^\s\""<>]+";
        var validUrlPattern = @"https:\/\/i\.pinimg\.com\/[^\/]+\/[^\/]+\/[^\/]+\/[^\/]+\/[^\/]+\.(jpg|jpeg|png|gif)";         
        

        // Match all potential image URLs in the content
        var matches = Regex.Matches(webPageContent, basePattern);
        
        foreach (var match in matches)
        {
            var url = match.ToString();
            
            // Ensure the URL matches the valid image pattern
            if (Regex.IsMatch(url, validUrlPattern))
            {
                extractedUrls.Add(url);
            }
        }

        return extractedUrls;
    }

    public IEnumerable<string> GetAllBoardUrlsFromProfileByUrlStringQuery(string webPageContent, string username)
    {
        if (webPageContent == null)
        {
            throw new ArgumentNullException(nameof(webPageContent), "The web page content cannot be null.");
        }
        
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        var boardUrls = new HashSet<string>();
        var usernamePattern = @"href=[""']\/([^\/]+)\/[^""']*[""']";
        
        // Find all matches for hrefs in the web page content
        var initialMatches = Regex.Matches(webPageContent, usernamePattern);
        
        // Filter if Url Contains username
        foreach (Match match in initialMatches)
        {
            var url = match.Value;
            if (url.Contains(username))
            {
                if (!url.Contains("_saved") && !url.Contains("_created") && !url.Equals($"href=\"/{username}/\""))
                {
                    // Append Pinterest URL to front of the url
                    url = "https://www.pinterest.com" + url.Substring(6, url.Length - 7);
                    boardUrls.Add(url);
                }
            }
        }
        return boardUrls;
    }

    public string GetUserNameFromUserUrl(string profileUrl)
    {
        var usernamePattern = @"https?:\/\/(?:[a-z]{2}\.)?pinterest\.com\/([^\/]+)\/";
        var usernameMatch = Regex.Match(profileUrl, usernamePattern);
        
        if (usernameMatch.Success)
        {
            return usernameMatch.Groups[1].Value.ToLower();
        }
        else
        {
            throw new ArgumentException("Unable to Get Profile URL");
        }
        
    }

    public string GetBoardNameFromUrl(string boardUrl)
    {
        var boardNamePattern = @"https?:\/\/(?:[a-z]{2}\.|www\.)?pinterest\.com\/[^\/]+\/([^\/]+)\/";
        var boardNameMatch = Regex.Match(boardUrl, boardNamePattern);
        
        if (boardNameMatch.Success)
        {
            return boardNameMatch.Groups[1].Value;
        }
        else
        {
            throw new ArgumentException("Unable to Get Board Name");
        }
    }
}