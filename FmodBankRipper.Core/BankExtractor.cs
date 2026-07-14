// FmodBankRipper.Core/BankExtractor.cs
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using System.Text;

namespace FmodBankRipper.Core;

public sealed class BankExtractor
{
    public event EventHandler<ProgressReport>? ProgressChanged;

    public async Task<List<ExtractionResult>> ExtractAsync(
        ExtractionOptions options,
        CancellationToken ct = default)
    {
        var results = new List<ExtractionResult>();
        var files = ResolveFiles(options.InputPath, options.Recursive);
        int totalFiles = files.Count;

        Directory.CreateDirectory(options.OutputDirectory);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            Report(file, i + 1, totalFiles, 0, 0, $"Reading: {Path.GetFileName(file)}");

            var result = await ExtractSingleAsync(file, options, ct);
            results.Add(result);

            Report(file, i + 1, totalFiles, 0, 0,
                result.Success
                    ? $"Done: {result.FilesExtracted} samples"
                    : $"Failed: {string.Join(", ", result.Errors.Take(2))}",
                i == totalFiles - 1);
        }

        return results;
    }

    private async Task<ExtractionResult> ExtractSingleAsync(
        string filePath,
        ExtractionOptions options,
        CancellationToken ct)
    {
        var result = new ExtractionResult { FilePath = filePath };
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string outDir = Path.Combine(options.OutputDirectory, fileName);
        Directory.CreateDirectory(outDir);

        try
        {
            byte[] data = await File.ReadAllBytesAsync(filePath, ct);
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            result.Errors.Add($"DEBUG: File size = {data.Length} bytes");

            List<FsbChunk> fsbChunks;

            if (ext == ".fsb")
            {
                var version = DetectFsbVersion(data);
                result.Errors.Add($"DEBUG: Detected as raw {version} file");
                fsbChunks = new List<FsbChunk> { new FsbChunk(version, data) };
            }
            else
            {
                fsbChunks = FsbScanner.ExtractChunks(data);
                result.Errors.Add($"DEBUG: Found {fsbChunks.Count} FSB chunk(s)");
                foreach (var c in fsbChunks)
                    result.Errors.Add($"DEBUG:   {c.Version} chunk, {c.Data.Length} bytes");
            }

            if (fsbChunks.Count == 0)
            {
                string hex = BitConverter.ToString(data.Take(Math.Min(32, data.Length)).ToArray());
                string ascii = Encoding.ASCII.GetString(data.Take(Math.Min(32, data.Length)).ToArray()).Replace("\0", ".");
                result.Errors.Add($"DEBUG: No FSB magic found");
                result.Errors.Add($"DEBUG: First 32 bytes (hex): {hex}");
                result.Errors.Add($"DEBUG: First 32 bytes (ASCII): {ascii}");
                return result;
            }

            int totalSamples = 0;
            foreach (var chunk in fsbChunks)
            {
                bool loaded = TryLoadFsb(chunk, out var bank, out string error);
                result.Errors.Add($"DEBUG: Load {chunk.Version} ({chunk.Data.Length} bytes): {error}");
                if (loaded && bank != null)
                    totalSamples += bank.Samples.Count;
            }

            result.Errors.Add($"DEBUG: Total parseable samples = {totalSamples}");

            if (totalSamples == 0)
            {
                result.Errors.Add("No samples could be parsed from any chunk");
                return result;
            }

            int sampleIdx = 0;
            foreach (var chunk in fsbChunks)
            {
                if (!TryLoadFsb(chunk, out var bank, out _))
                    continue;

                // Get audio type from bank header for format detection
                FmodAudioType audioType = bank.Header.AudioType;

                foreach (var sample in bank.Samples)
                {
                    ct.ThrowIfCancellationRequested();
                    Report(filePath, 0, 0, sampleIdx + 1, totalSamples,
                        $"Rebuilding: {sample.Name ?? $"sample_{sampleIdx:D4}"}");

                    string safeName = Sanitize(sample.Name ?? $"sample_{sampleIdx:D4}");
                    string outPath = Path.Combine(outDir, safeName);

                    int dup = 1;
                    while (File.Exists(outPath + ".wav") && !options.OverwriteExisting)
                        outPath = Path.Combine(outDir, $"{safeName}_{dup++}");

                    // Try to rebuild with Fmod5Sharp
                    bool ok = sample.RebuildAsStandardFileFormat(out var bytes, out var extn);

                    string finalExt;
                    byte[] finalBytes;

                    if (!ok || bytes == null)
                    {
                        // Fallback: try to extract raw PCM and wrap in WAV
                        result.Errors.Add($"DEBUG: Rebuild failed for {safeName}, trying PCM fallback");
                        var pcmResult = TryExtractPcmWav(sample, audioType);
                        if (pcmResult != null)
                        {
                            finalBytes = pcmResult;
                            finalExt = "wav";
                            result.Errors.Add($"DEBUG: PCM fallback succeeded");
                        }
                        else
                        {
                            result.FilesFailed++;
                            result.Errors.Add($"Rebuild and PCM fallback both failed: {safeName}");
                            sampleIdx++;
                            continue;
                        }
                    }
                    else
                    {
                        finalBytes = bytes;
                        // Use bank header audio type to determine real format
                        finalExt = DetermineExtension(audioType, extn);
                        result.Errors.Add($"DEBUG: Rebuild OK, type={audioType}, ext={extn}, final={finalExt}");
                    }

                    string finalPath = outPath + "." + finalExt;
                    await File.WriteAllBytesAsync(finalPath, finalBytes, ct);
                    result.ExtractedFiles.Add(finalPath);
                    result.FilesExtracted++;
                    sampleIdx++;
                }
            }

            result.Success = result.FilesExtracted > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Determines file extension from bank header audio type and rebuild result
    /// </summary>
    private static string DetermineExtension(FmodAudioType audioType, string? rebuildExt)
    {
        // If rebuild gave us a known format, trust it
        if (!string.IsNullOrEmpty(rebuildExt))
        {
            string ext = rebuildExt.ToLowerInvariant();
            if (ext == "wav" || ext == "ogg" || ext == "mp3" || ext == "flac")
                return ext;
        }

        // Map FmodAudioType to extension — CORRECTED enum names
        return audioType switch
        {
            FmodAudioType.PCM8 => "wav",
            FmodAudioType.PCM16 => "wav",
            FmodAudioType.PCM24 => "wav",
            FmodAudioType.PCM32 => "wav",
            FmodAudioType.PCMFLOAT => "wav",
            FmodAudioType.IMAADPCM => "wav",
            FmodAudioType.GCADPCM => "wav",
            FmodAudioType.FADPCM => "wav",
            FmodAudioType.VORBIS => "ogg",
            FmodAudioType.MPEG => "mp3",
            FmodAudioType.CELT => "ogg",
            FmodAudioType.AT9 => "at9",
            FmodAudioType.XMA => "xma",
            FmodAudioType.HEVAG => "vag",
            FmodAudioType.VAG => "vag",
            FmodAudioType.XWMA => "xwma",
            FmodAudioType.OPUS => "opus",
            _ => "wav" // Default to wav for unknown
        };
    }

    /// <summary>
    /// Fallback: Extract raw PCM data and wrap it in a standard WAV header
    /// </summary>
    private static byte[]? TryExtractPcmWav(FmodSample sample, FmodAudioType audioType)
    {
        try
        {
            // Only works for PCM formats — CORRECTED enum names
            if (audioType != FmodAudioType.PCM8 &&
                audioType != FmodAudioType.PCM16 &&
                audioType != FmodAudioType.PCM24 &&
                audioType != FmodAudioType.PCM32 &&
                audioType != FmodAudioType.PCMFLOAT)
            {
                return null; // Can't do PCM fallback for compressed formats
            }

            // Get the raw sample data
            var rawData = sample.SampleBytes;
            if (rawData == null || rawData.Length == 0)
                return null;

            var meta = sample.Metadata;
            int channels = (int)meta.Channels;        // PascalCase!
            int frequency = meta.Frequency;             // PascalCase!

            // Determine bit depth from audio type — CORRECTED enum names
            int bitsPerSample = audioType switch
            {
                FmodAudioType.PCM8 => 8,
                FmodAudioType.PCM16 => 16,
                FmodAudioType.PCM24 => 24,
                FmodAudioType.PCM32 or FmodAudioType.PCMFLOAT => 32,
                _ => 16
            };

            // Calculate WAV parameters
            int byteRate = frequency * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;
            int dataChunkSize = rawData.Length;
            int totalSize = 36 + dataChunkSize;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(totalSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size
            writer.Write((ushort)(audioType == FmodAudioType.PCMFLOAT ? 3 : 1)); // AudioFormat: 1=PCM, 3=Float
            writer.Write((ushort)channels);
            writer.Write(frequency);
            writer.Write(byteRate);
            writer.Write((ushort)blockAlign);
            writer.Write((ushort)bitsPerSample);

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataChunkSize);
            writer.Write(rawData);

            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static bool TryLoadFsb(FsbChunk chunk, out FmodSoundBank bank, out string error)
    {
        try
        {
            bool ok = FsbLoader.TryLoadFsbFromByteArray(chunk.Data, out bank);
            error = ok ? "Success" : "FsbLoader returned false";
            return ok;
        }
        catch (Exception ex)
        {
            bank = null!;
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    private static FsbVersion DetectFsbVersion(byte[] data)
    {
        if (data.Length < 4) return FsbVersion.Fsb5;

        string magic = Encoding.ASCII.GetString(data, 0, 4);
        return magic switch
        {
            "FSB3" => FsbVersion.Fsb3,
            "FSB4" => FsbVersion.Fsb4,
            "FSB5" => FsbVersion.Fsb5,
            _ => FsbVersion.Fsb5
        };
    }

    private void Report(string file, int fIdx, int fTot, int sIdx, int sTot, string msg, bool done = false)
    {
        ProgressChanged?.Invoke(this, new ProgressReport
        {
            CurrentFile = file,
            CurrentFileIndex = fIdx,
            TotalFiles = fTot,
            CurrentSampleIndex = sIdx,
            TotalSamples = sTot,
            StatusMessage = msg,
            IsComplete = done
        });
    }

    private static List<string> ResolveFiles(string path, bool recursive)
    {
        if (File.Exists(path)) return new List<string> { path };
        if (!Directory.Exists(path)) return new List<string>();

        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(path, "*.bank", opt));
        files.AddRange(Directory.GetFiles(path, "*.fsb", opt));
        return files;
    }

    private static string Sanitize(string name)
    {
        var sb = new StringBuilder(name);
        foreach (char c in Path.GetInvalidFileNameChars())
            sb.Replace(c, '_');
        return sb.ToString().Trim();
    }
}