using System.Configuration;
using Pinterest_Image_Downloader.Interfaces;
using Pinterest_Image_Downloader.Models;
using Spectre.Console;

namespace Pinterest_Image_Downloader.Helpers;

public class MenuHelpers : IMenuHelper
{
   private readonly IWebService _webService;
   private readonly IParsingService _parsingService;
   
   public MenuHelpers(IWebService webService, IParsingService parsingService)
   {
      _webService = webService;
      _parsingService = parsingService;
   }
   
   
   public async Task<ServiceResult> ExecuteAction(MenuChoice menuChoice)
   {
      if (menuChoice.UserMenuChoice.Contains("Single Board"))
      {
         return await ExecuteDownloadSingleBoard(menuChoice.UserUrl);
      }
      else
      {
         // TODO - Build
         return ExecuteDownloadAllBoards(menuChoice.UserUrl);
      }
   }

   // TODO - Tidy up Logging
   private async Task<ServiceResult> ExecuteDownloadSingleBoard(string url)
   {
      var statefuWebPageContent = await _webService.GetStatefulWebPageContent(url);
      var pins = _parsingService.GetAllBoardContentUrlsByUrlStringQuery(statefuWebPageContent).ToList();
      AnsiConsole.WriteLine("Starting download of " + pins.Count + " pins from board: " + url);
      var downloadResult = await _webService.DownloadBoardPinsJob(pins);
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

   private ServiceResult ExecuteDownloadAllBoards(string url)
   {
      return ServiceResult.AsSuccess();
   }
}