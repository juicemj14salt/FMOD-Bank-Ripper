namespace FmodBankRipper.Core;

public class ExtractionOptions
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public bool Recursive { get; set; } = false;
    public bool OverwriteExisting { get; set; } = false;
    public string? EncryptionKey { get; set; }
}