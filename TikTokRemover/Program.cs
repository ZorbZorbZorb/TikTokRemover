namespace TikTokRemover;

public class Program {
    public static async Task Main(string[] args) {

        if (!File.Exists(FfmpegHelpers.FFMPEGPath)) {
            Console.WriteLine("FFMPEG not found. Please copy FFMPEG.exe to the program's working directory.");
            return;
        }
        if (!File.Exists(FfmpegHelpers.FFProbePath)) {
            Console.WriteLine("FFMPEG not found. Please copy FFPROBE.exe to the program's working directory.");
            return;
        }

        string outputPath;
        if (args.Length < 1) {
            Console.WriteLine("Missing arguments.");
            Console.WriteLine("Usage: RemoveTikTokOutro.exe {inputPath} {outputPath}");
            Console.WriteLine("Example: RemoveTikTokOutro.exe C:\\input.mp4 \"C:\\my output.mp4\"");
            return;
        }
        else if (args.Length < 2) {
            outputPath = '"' + Path.Combine(Directory.GetCurrentDirectory(), "output.mp4") + '"';

        }
        else {
            outputPath = args[1];
        }

        if (!Path.Exists(args[0])) {
            Console.WriteLine($"File not found: {args[0]}");
            return;
        }

        await TikTok.RemoveTikTokOutro(args[0], outputPath);

        Console.WriteLine($"Finished. Output: {outputPath}");
    }

}
