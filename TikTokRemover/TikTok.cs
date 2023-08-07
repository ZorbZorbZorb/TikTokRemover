namespace TikTokRemover;

public class TikTok {
    public static async Task RemoveTikTokOutro(string input, string output) {
        float duration = await FfmpegHelpers.GetVideoDurationAsync(input);
        int frameCount = await FfmpegHelpers.CountVideoFrames(input);
        float fps = (float)frameCount / (float)duration;

        var videoDimensions = await FfmpegHelpers.GetVideoDimensions(input);
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

        float frameTime = FfmpegHelpers.GetTimeFromFrame(fps, targetFrame - 1);
        string frameTimeString = FfmpegHelpers.FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        string args = $"-i {input} -y -ss 0 -t {frameTimeString} -c:v copy -c:a copy {output}";
        await ProcessHelpers.ExecuteAndReadOutputAsync(FfmpegHelpers.FFMPEGPath, args);
    }

    public static async Task<bool> IsTikTokFrame(string input, string timespanString, int frameWidth, int frameHeight) {
        (byte, byte, byte) color1 = await FfmpegHelpers.GetPixelAsync(input, timespanString, 1, 1);
        if (!IsTikTokOutroColor(color1.Item1, color1.Item2, color1.Item3)) {
            return false;
        }
        (byte, byte, byte) color2 = await FfmpegHelpers.GetPixelAsync(input, timespanString, frameWidth, 1);
        if (!IsTikTokOutroColor(color2.Item1, color2.Item2, color2.Item3)) {
            return false;
        }
        (byte, byte, byte) color3 = await FfmpegHelpers.GetPixelAsync(input, timespanString, 1, frameWidth);
        if (!IsTikTokOutroColor(color1.Item3, color3.Item2, color3.Item3)) {
            return false;
        }
        (byte, byte, byte) color4 = await FfmpegHelpers.GetPixelAsync(input, timespanString, frameWidth, frameHeight);
        if (!IsTikTokOutroColor(color4.Item1, color4.Item2, color4.Item3)) {
            return false;
        }
        return true;
    }

    public static async Task<bool> IsTikTokFrame(string input, int frameNumber, float fps, int frameWidth, int frameHeight) {
        float frameTime = FfmpegHelpers.GetTimeFromFrame(fps, frameNumber);
        string frameTimeString = FfmpegHelpers.FormatMillisecondsTimeAsFfmpegSeek(frameTime);
        return await IsTikTokFrame(input, frameTimeString, frameWidth, frameHeight);
    }

    public static bool IsTikTokOutroColor(byte r, byte g, byte b) {
        return r > 10 && r < 30 && g > 10 && g < 30 && b > 15 && b < 35;
    }
}
