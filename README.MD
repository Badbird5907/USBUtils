# USBUtils
A CLI tool to toggle the offline status on external drives on windows, as windows does not let you change the status via diskpart/powershell.


## Usage
### Interactive:
![demo gif](https://raw.githubusercontent.com/Badbird5907/USBUtils/master/imgs/demo.gif)

### CLI:
`.\usbutils.exe <disk>` <disk> = disk number to toggle

## Building
1. run `dotnet publish --configuration Release`
2. The output exe can be found in `./bin/Release/net8.0/win-x64/publish`
