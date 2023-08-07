namespace TikTokRemover;

public static class FfmpegHelpers {
    public static string FFMPEGPath = "ffmpeg.exe";
    public static string FFProbePath = "ffprobe.exe";

    public static float GetTimeFromFrame(float fps, int frameNumber) {
        float frameInterval = 1000f / fps;
        return frameNumber * frameInterval;
    }

    public static async Task<int> CountVideoFrames(string input) {
        string args = $"-v error -select_streams v:0 -count_frames -show_entries stream=nb_read_frames -of csv=p=0 {input}";
        string result = await ProcessHelpers.ExecuteAndReadOutputAsync(FFProbePath, args);
        if (int.TryParse(result, out var frames)) {
            return frames;
        }
        throw new InvalidOperationException();
    }

    public static async Task<(int, int)> GetVideoDimensions(string input) {
        string args = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {input}";
        string result = await ProcessHelpers.ExecuteAndReadOutputAsync(FFProbePath, args);
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

    public static async Task<(byte, byte, byte)> GetPixelAsync(string input, string frameTime, int pixelX, int pixelY) {
        // Get pixel at position
        string tempOutputFile = Path.Join(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.yuv");
        string args = $"-i {input} -vf \"crop=1:1:{pixelX}:{pixelY}:exact=1\" -vframes 1 -ss {frameTime} -y \"{tempOutputFile}\"";
        await ProcessHelpers.ExecuteAsync(FFMPEGPath, args);
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

    public static async Task<byte[]> GetFrameAsync(string input, string frameTime) {
        // Get pixel at position
        string tempOutputFile = Path.Join(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.png");
        string args = $"-i {input} -vframes 1 -ss {frameTime} -y \"{tempOutputFile}\"";
        await ProcessHelpers.ExecuteAsync(FFMPEGPath, args);
        byte[] bytes = await File.ReadAllBytesAsync(tempOutputFile);
        File.Delete(tempOutputFile);
        return bytes;
    }

    public static async Task<byte[]> GetFrameAsync(string input, int frameNumber, float fps) {
        float frameTime = GetTimeFromFrame(fps, frameNumber);
        string frameTimeString = FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        return await GetFrameAsync(input, frameTimeString);
    }

    public static string FormatMillisecondsTimeAsFfmpegSeek(float time) {
        int minutes = (int)(time / 1000f / 60f);
        int seconds = (int)(time / 1000f) % 60;
        int milliseconds = (int)time % 1000;

        return minutes.ToString() + ':' + seconds.ToString() + '.' + milliseconds.ToString();
    }

    public static async Task<float> GetVideoDurationAsync(string input) {
        string args = "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 " + input;
        var durationString = await ProcessHelpers.ExecuteAndReadOutputAsync(FFProbePath, args);
        if (float.TryParse(durationString, out float duration)) {
            return duration;
        }
        throw new ArgumentException();
    }

}
