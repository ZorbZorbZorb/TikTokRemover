# TikTokRemover
Removes the TikTok outro from videos

<br>

# Building from source code
### Building from source code with FFmpeg in the program
1. Build this program with .NET 7.0.
2. Copy FFmpeg.exe and FFprobe.exe into the program's binaries folder

### Building from source code with FFmpeg in a different location
1. Change FfmpegHelpers.FFmpegPath to the location where you have FFmpeg installed on your machine. https://github.com/ZorbZorbZorb/TikTokRemover/blob/86e0b1671f3295e1420aed98c851cffb9d9c4ada/TikTokRemover/FfmpegHelpers.cs#L4
2. Change FfmpegHelpers.FFprobePath to the location where you have FFprobe installed on your machine. https://github.com/ZorbZorbZorb/TikTokRemover/blob/86e0b1671f3295e1420aed98c851cffb9d9c4ada/TikTokRemover/FfmpegHelpers.cs#L5
3. Build this program with .NET 7.0.

<br>

# Usage
1. Open a command line to the program's binaries folder
2. Execute one of the below commands:


RemoveTikTokOutro.exe<br>
> Displays this help text<br>
> Example:<br>
> RemoveTikTokOutro.exe<br>
<br>

RemoveTikTokOutro.exe "{inputFilePath}"<br>
> Remove the TikTok outro from the input video file, output the result as an mp4 into the program's binaries folder<br>
> Example:<br>
> RemoveTikTokOutro.exe "C:/my_video.mp4"<br>
<br>

RemoveTikTokOutro.exe "{inputFilePath}" "{outputFilePath}"<br>
> Remove the TikTok outro from the input video file, output the result to the output file path<br>
> Example:<br>
> RemoveTikTokOutro.exe "C:/my_input_video.mp4" "C:/my_output_video.mp4" <br>
