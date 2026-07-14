namespace FmodBankRipper.Core;

public class ExtractionResult
{
    public string FilePath { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int FilesExtracted { get; set; }
    public int FilesFailed { get; set; }
    public List<string> ExtractedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}