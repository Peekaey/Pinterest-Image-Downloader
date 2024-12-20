# Pinterest-Image-Downloader

.NET Console Application using Playwright to download entire images from Pinterest Boards

### How It Works
Application launches a playwright browser session to scroll and capture all data from a specified Pinterest board. It then extracts the image URLs and downloads them to the "Downloads" folder within the project.

The application attempts to originally download the image in the highest quality possible, however has fallbacks to 3 lower quality versions if this fails which is done automatically.

As the application is using Playwright, a browser session of chromium will automatically be launched and actions performed. However, this can be disabled by specifying the `Headless = true` parameter
to make the browser open in the background.

Playwright is required due to Pinterest's use of heavy javascript and lazy loading of images.

### Features/Options
- Download all images from a specified Pinterest board
- Automatically capture all <b><u>Public</u></b> boards from a users profile and then automatically download all images from each board into distinct folders

Does not currently support session tokens so only public boards can be downloaded. (What you would see in a private browser window)

### Notes/Limitations
As the application is currently using string parsing to extract image URLs instead of a parsing library like AngleSharp, it may capture/save additional noise (images) from each board - usually from the "more like this " section at the bottom. 
However, from testing this is only a couple of random images.

Currently only supports jpeg,jpeg,png and gif file types to be downloaded.

Images are also currently saved with their original file names

### Future Notes
- Add support for session tokens to download private boards 
- Move from string based parsing to a parsing library like AngleSharp for more precise parsing