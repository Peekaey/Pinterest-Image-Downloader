using Microsoft.Extensions.DependencyInjection;
using Pinterest_Image_Downloader.Helpers;
using Pinterest_Image_Downloader.Interfaces;
using Pinterest_Image_Downloader.Models;
using Pinterest_Image_Downloader.Service;
using Spectre.Console;

namespace Pinterest_Image_Downloader;
class Program
{
    static async Task Main(string[] args)
    {
        // Configure the DI container
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var menuHelpers = serviceProvider.GetService<IMenuHelper>();
        
        if (menuHelpers == null)
        {
            AnsiConsole.WriteLine("Issue Initialising MenuHelpers");
            return;
        }
        var userChoices = GetUserInput();
        var result = await menuHelpers.ExecuteAction(userChoices);
    }
    
    private static MenuChoice GetUserInput()
    {
        AnsiConsole.WriteLine("Please select if you wish to download a single board or every board from a profile");
        var userChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an option")
                .PageSize(3)
                .AddChoices(new[] { "Single Board", "All Boards" }));

        var urlMessage = userChoice == "Single Board"
            ? "Please enter the URL of the board you wish to download"
            : "Please enter the URL of the profile you wish to download all the contents of boards from";

        var userUrl = AnsiConsole.Prompt(
            new TextPrompt<string>(urlMessage)
                .Validate(url =>
                {
                    if (string.IsNullOrWhiteSpace(url) || !url.ToLower().Contains("pinterest"))
                    {
                        return ValidationResult.Error("Entered URL must not be empty and must be for pinterest only");
                    }
                    return ValidationResult.Success();
                }));

        return new MenuChoice
        {
            UserMenuChoice = userChoice,
            UserUrl = userUrl
        };
    }
    
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IWebService, WebService>();
        services.AddSingleton<IMenuHelper, MenuHelpers>();
        services.AddSingleton<IParsingService, ParsingService>();
        services.AddSingleton<IFileHelpers, FileHelpers>();
    }
}