using System.Configuration;
using Pinterest_Image_Downloader.Interfaces;
using Pinterest_Image_Downloader.Models;
using Spectre.Console;

namespace Pinterest_Image_Downloader.Helpers;

public class MenuHelpers : IMenuHelper
{
   private readonly IWebService _webService;
   private readonly IParsingService _parsingService;
   private readonly IFileHelpers _fileHelpers;
   
   public MenuHelpers(IWebService webService, IParsingService parsingService, IFileHelpers fileHelpers)
   {
      _webService = webService;
      _parsingService = parsingService;
      _fileHelpers = fileHelpers;
   }
   
   
   public async Task<ServiceResult> ExecuteAction(MenuChoice menuChoice)
   {
      var folderPath = _fileHelpers.GetRootProjectDirectory();
      var parentDownloadFolder = _fileHelpers.CreateDownloadFolder(folderPath, "Downloads");
      
      if (menuChoice.UserMenuChoice.Contains("Single Board"))
      {
         return await ExecuteDownloadSingleBoard(menuChoice.UserUrl, parentDownloadFolder);
      }
      else
      {
         // TODO - Build
         return await ExecuteDownloadAllBoards(menuChoice.UserUrl, parentDownloadFolder);
      }
   }

   // TODO - Tidy up Logging
   private async Task<ServiceResult> ExecuteDownloadSingleBoard(string url, string parentDownloadFolder)
   {
      var endContentCSSSelector = ConfigurationManager.AppSettings["BoardMoreLikeThisCSSSelector"]; // Selector to stop scrolling
      
      var statefuWebPageContent = await _webService.GetStatefulWebPageContent(url, endContentCSSSelector);
      var pins = _parsingService.GetAllBoardContentUrlsByUrlStringQuery(statefuWebPageContent).ToList();
      var boardName = _parsingService.GetBoardNameFromUrl(url);
      AnsiConsole.WriteLine("Starting download of " + pins.Count + " pins from board: " + url);
      var downloadResult = await _webService.DownloadBoardPinsJob(pins, parentDownloadFolder, boardName);
      if (downloadResult.Count < 1)
      {
         AnsiConsole.WriteLine("Download of " + pins.Count + " pins from board: " + url + " was successful");
         return ServiceResult.AsSuccess();
      }
      else
      {
         AnsiConsole.WriteLine($"{downloadResult.Count} pins failed to download. Printing list...");
         foreach (var pin in downloadResult)
         {
            AnsiConsole.WriteLine("Failed to download pin: " + pin.FileUrl);
            AnsiConsole.WriteLine("Reason: " + pin.Error);
            AnsiConsole.WriteLine(" ");
         }
      }
      return ServiceResult.AsSuccess();
   }

   private async Task<ServiceResult> ExecuteDownloadAllBoards(string url, string parentDownloadFolder)
   {
      var endContentCSSSelector = ConfigurationManager.AppSettings["ProfileMoreIdeasCSSSelector"]; // Selector to stop scrolling
      var profileStatefulWebPageContent = await _webService.GetStatefulProfileWebPageContent(url, endContentCSSSelector);
      var username = _parsingService.GetUserNameFromUserUrl(url);
      var boardUrls = _parsingService.GetAllBoardUrlsFromProfileByUrlStringQuery(profileStatefulWebPageContent, username).ToList();

      var boardsWithErrors = new List<string>();
      foreach (var board in boardUrls)
      {
         var fullProfileDownloadResult = await ExecuteDownloadSingleBoard(board, parentDownloadFolder);
         if (fullProfileDownloadResult.IsSuccess == false)
         {
            boardsWithErrors.Add(board);
         }
      }

      if (boardsWithErrors.Count > 0)
      {
         AnsiConsole.WriteLine("The following boards failed to download:");
         foreach (var board in boardsWithErrors)
         {
            AnsiConsole.WriteLine(board);
         }
      }
      
      return ServiceResult.AsSuccess();
   }
}