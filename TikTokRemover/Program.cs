using System.Diagnostics;

namespace TikTokRemover;

internal class Program {
    const string FFMPEGPath = "ffmpeg.exe";
    const string FFProbePath = "ffprobe.exe";

    private static string GetFfmpegArgs(string input, string output, string endTime) {
        return $"-i {input} -y -ss 0 -t {endTime} -c:v copy -c:a copy {output}";
    }

    private static string GetFfprobeArgs(string input) {
        const string command = "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 ";
        return command + input;
    }

    private static async Task<int> CountVideoFrames(string input) {
        string args = $"-v error -select_streams v:0 -count_frames -show_entries stream=nb_read_frames -of csv=p=0 {input}";
        string result = await ExecuteWithOutput(FFProbePath, args);
        if (int.TryParse(result, out var frames)) {
            return frames;
        }
        throw new InvalidOperationException();
    }

    private static async Task<(int, int)> GetVideoDimensions(string input) {
        string args = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {input}";
        string result = await ExecuteWithOutput(FFProbePath, args);
        if (result.Length > 9 || result.Length < 3) {
            throw new ArgumentOutOfRangeException();
        }
        var explosion = result.Split('x');
        if (explosion.Length != 2) {
            throw new ArgumentException();
        }
        if (!float.TryParse(explosion[0], out float x) || !float.TryParse(explosion[0], out float y)) {
            throw new InvalidDataException();
        }
        return ((int)x, (int)y);
    }

    private static async Task<(byte, byte, byte)> GetPixelAsync(string input, string frameTime, int pixelX, int pixelY) {
        // Get pixel at position
        string tempOutputFile = Path.Join(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.yuv");
        string args = $"-i {input} -vf \"crop=1:1:{pixelX}:{pixelY}:exact=1\" -vframes 1 -ss {frameTime} -y \"{tempOutputFile}\"";
        await Execute(FFMPEGPath, args);
        byte[] bytes = await File.ReadAllBytesAsync(tempOutputFile);
        File.Delete(tempOutputFile);

        // Convert YUV to RGB
        float y = bytes[0] - 16f;
        float u = bytes[1] - 128f;
        float v = bytes[2] - 128f;
        float r = float.Round(1.164f * y + 1.596f * v, 0);
        float g = float.Round(1.164f * y - 0.392f * u - 0.813f * v, 0);
        float b = float.Round(1.164f * y + 2.017f * u, 0);

        return ((byte)r, (byte)g, (byte)b);
    }

    private static async Task<byte[]> GetFrameAsync(string input, string frameTime) {
        // Get pixel at position
        string tempOutputFile = Path.Join(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.png");
        string args = $"-i {input} -vframes 1 -ss {frameTime} -y \"{tempOutputFile}\"";
        await Execute(FFMPEGPath, args);
        byte[] bytes = await File.ReadAllBytesAsync(tempOutputFile);
        File.Delete(tempOutputFile);
        return bytes;
    }

    private static async Task<byte[]> GetFrameAsync(string input, int frameNumber, float fps) {
        float frameTime = GetTimeFromFrame(fps, frameNumber);
        string frameTimeString = FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        return await GetFrameAsync(input, frameTimeString);
    }

    private static async Task<byte[]> DebugGetFrameAsync(string input, int frameNumber, float fps) {
        float frameTime = GetTimeFromFrame(fps, frameNumber);
        string frameTimeString = FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        string tempOutputFile = Path.Join(Directory.GetCurrentDirectory(), $"frame_{frameNumber}.png");
        string args = $"-i {input} -vframes 1 -ss {frameTimeString} -y \"{tempOutputFile}\"";
        await Execute(FFMPEGPath, args);
        byte[] bytes = await File.ReadAllBytesAsync(tempOutputFile);
        return bytes;
    }

    private static float GetTimeFromFrame(float fps, int frameNumber) {
        float frameInterval = 1000f / fps;
        return frameNumber * frameInterval;
    }

    private static string FormatMillisecondsTimeAsFfmpegSeek(float time) {
        int minutes = (int)(time / 1000f / 60f);
        int seconds = (int)(time / 1000f) % 60;
        int milliseconds = (int)time % 1000;

        return minutes.ToString() + ':' + seconds.ToString() + '.' + milliseconds.ToString();
    }

    private static async Task<float> GetVideoDurationAsync(string input) {
        var durationString = await ExecuteWithOutput(FFProbePath, GetFfprobeArgs(input));
        if (float.TryParse(durationString, out float duration)) {
            return duration;
        }
        throw new ArgumentException();
    }

    private static async Task Main(string[] args) {
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

        await RemoveTikTokOutro(args[0], args[1]);
        await RemoveTikTokOutro(args[0], outputPath);
        Console.WriteLine($"Finished. Output: {outputPath}");
    }

    private static async Task RemoveTikTokOutro(string input, string output) {
        float duration = await GetVideoDurationAsync(input);
        int frameCount = await CountVideoFrames(input);
        float fps = (float)frameCount / (float)duration;

        float[] frameTimes = new float[frameCount];
        string[] frameTimeStrings = new string[frameCount];
        for (int i = 0; i < frameCount; i++) {
            frameTimes[i] = GetTimeFromFrame(fps, i);
            frameTimeStrings[i] = FormatMillisecondsTimeAsFfmpegSeek(frameTimes[i]);
        }

        var videoDimensions = await GetVideoDimensions(input);
        int videoWidth = videoDimensions.Item1;
        int videoHeight = videoDimensions.Item2;

        // Search for start of video
        int targetFrame = frameCount;
        int jumpAmount = 60;
        while (true) {
            bool isOutroFrame = await IsTikTokFrame(input, targetFrame - jumpAmount, fps, videoWidth, videoHeight);
            // If still in outro, move back jumpAmount
            if (isOutroFrame) {
                targetFrame -= jumpAmount;
            }
            // If now in video, halve jump amount and retry
            else {
                // If located border, break.
                if (jumpAmount == 1) {
                    // Check for error (no outro detected)
                    if (targetFrame == frameCount) {
                        throw new ArgumentException();
                    }
                    // targetFrame is now the first frame where the outro begins.
                    else {
                        break;
                    }
                }
                // Halve jump amount.
                jumpAmount /= 2;
            }
        }

        float frameTime = GetTimeFromFrame(fps, targetFrame - 1);
        string frameTimeString = FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        await ExecuteWithOutput(FFMPEGPath, GetFfmpegArgs(input, output, frameTimeString));
    }

    private static async Task<bool> IsTikTokFrame(string input, string timespanString, int frameWidth, int frameHeight) {
        (byte, byte, byte) color1 = await GetPixelAsync(input, timespanString, 1, 1);
        if (!IsTikTokOutroColor(color1.Item1, color1.Item2, color1.Item3)) {
            return false;
        }
        (byte, byte, byte) color2 = await GetPixelAsync(input, timespanString, frameWidth, 1);
        if (!IsTikTokOutroColor(color2.Item1, color2.Item2, color2.Item3)) {
            return false;
        }
        (byte, byte, byte) color3 = await GetPixelAsync(input, timespanString, 1, frameWidth);
        if (!IsTikTokOutroColor(color1.Item3, color3.Item2, color3.Item3)) {
            return false;
        }
        (byte, byte, byte) color4 = await GetPixelAsync(input, timespanString, frameWidth, frameHeight);
        if (!IsTikTokOutroColor(color4.Item1, color4.Item2, color4.Item3)) {
            return false;
        }
        return true;
    }

    private static async Task<bool> IsTikTokFrame(string input, int frameNumber, float fps, int frameWidth, int frameHeight) {
        float frameTime = GetTimeFromFrame(fps, frameNumber);
        string frameTimeString = FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        return await IsTikTokFrame(input, frameTimeString, frameWidth, frameHeight);
    }

    private static bool IsTikTokOutroColor(byte r, byte g, byte b) {
        return r > 10 && r < 30 && g > 10 && g < 30 && b > 15 && b < 35;
    }

    private static async Task<string> ExecuteWithOutput(string exePath, string parameters) {
        string result = string.Empty;

        using (Process p = new Process()) {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = parameters;
            p.Start();
            await p.WaitForExitAsync();

            result = p.StandardOutput.ReadToEnd();
        }

        return result;
    }

    private static async Task Execute(string exePath, string parameters) {
        using (Process p = new Process()) {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = parameters;
            p.Start();
            await p.WaitForExitAsync();
        }
    }

}
