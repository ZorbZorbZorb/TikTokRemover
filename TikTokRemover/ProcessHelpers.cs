using System.Diagnostics;

namespace TikTokRemover;

public static class ProcessHelpers {
    public static async Task<string> ExecuteAndReadOutputAsync(string exePath, string parameters) {
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

    public static async Task ExecuteAsync(string exePath, string parameters) {
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

