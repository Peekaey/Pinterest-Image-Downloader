using AngleSharp.Dom;
using Pinterest_Image_Downloader.Interfaces;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Pinterest_Image_Downloader.Service;


public class ParsingService : IParsingService
{
    
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
    
    // TODO Tidy Code
    public IEnumerable<string> GetAllBoardContentUrlsByUrlStringQuery(string webPageContent)
    {
        if (webPageContent == null)
        {
            throw new ArgumentNullException(nameof(webPageContent), "The web page content cannot be null.");
        }
        
        var boardDivContainerCssSelector = ConfigurationManager.AppSettings["BoardDivContainerCSSSelector"];

        // Hash Set to Ensure Uniqueness
        var srcValues = new HashSet<string>(); 
        
        var urlPattern = @"https:\/\/i\.pinimg\.com\/[^\s\""<>]+";

        // Match all Pin Img URLs
        var matches = System.Text.RegularExpressions.Regex.Matches(webPageContent, urlPattern);

        // Add each unique URL to the HashSet
        foreach (var match in matches)
        {
            var url = match.ToString();
            
            // Regex Pattern Match so that it matches Downloadable Pins Part Of The Board Only
            var validUrlPattern = @"https:\/\/i\.pinimg\.com\/[^\/]+\/[^\/]+\/[^\/]+\/[^\/]+\/[^\/]+\.(jpg|jpeg|png|gif)";            // var validUrlPattern = @"https:\/\/i\.pinimg\.com\/";
            if (Regex.IsMatch(url, validUrlPattern))
            {
                srcValues.Add(url);
            }
        }

        return srcValues;
    }
    
}