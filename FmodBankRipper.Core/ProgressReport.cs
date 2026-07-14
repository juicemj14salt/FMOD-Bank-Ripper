namespace FmodBankRipper.Core;

public class ProgressReport
{
    public string CurrentFile { get; set; } = string.Empty;
    public int CurrentFileIndex { get; set; }
    public int TotalFiles { get; set; }
    public int CurrentSampleIndex { get; set; }
    public int TotalSamples { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}