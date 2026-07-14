// FmodBankRipper.GUI/MainForm.cs
using FmodBankRipper.Core;
using System.ComponentModel;

namespace FmodBankRipper.GUI;

public class MainForm : Form
{
    private TextBox txtInput = null!;
    private TextBox txtOutput = null!;
    private Button btnBrowseIn = null!;
    private Button btnBrowseOut = null!;
    private Button btnExtract = null!;
    private Button btnCancel = null!;
    private CheckBox chkRecursive = null!;
    private CheckBox chkOverwrite = null!;
    private ProgressBar barFiles = null!;
    private ProgressBar barSamples = null!;
    private Label lblStatus = null!;
    private ListView lvResults = null!;
    private CancellationTokenSource? cts;

    public MainForm()
    {
        Text = "FMOD Bank Audio Extractor";
        Size = new Size(900, 650);
        MinimumSize = new Size(700, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(20);
        InitializeLayout();
    }

    private void InitializeLayout()
    {
        int margin = 20;
        int labelWidth = 110;
        int btnWidth = 100;
        int rowHeight = 32;
        int y = 20;

        // === INPUT ROW ===
        var lblIn = new Label
        {
            Text = "Input File/Folder:",
            Location = new Point(margin, y + 4),
            Size = new Size(labelWidth, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtInput = new TextBox
        {
            Location = new Point(margin + labelWidth + 10, y),
            Size = new Size(600, 25),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        btnBrowseIn = new Button
        {
            Text = "Browse...",
            Location = new Point(ClientSize.Width - margin - btnWidth, y - 1),
            Size = new Size(btnWidth, 27),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnBrowseIn.Click += (s, e) => BrowseInput();

        // === OUTPUT ROW ===
        y += rowHeight + 12;
        var lblOut = new Label
        {
            Text = "Output Folder:",
            Location = new Point(margin, y + 4),
            Size = new Size(labelWidth, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtOutput = new TextBox
        {
            Location = new Point(margin + labelWidth + 10, y),
            Size = new Size(600, 25),
            Text = "ExtractedAudio",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        btnBrowseOut = new Button
        {
            Text = "Browse...",
            Location = new Point(ClientSize.Width - margin - btnWidth, y - 1),
            Size = new Size(btnWidth, 27),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnBrowseOut.Click += (s, e) => BrowseOutput();

        // === OPTIONS ROW ===
        y += rowHeight + 15;
        chkRecursive = new CheckBox
        {
            Text = "Recursive (include subfolders)",
            Location = new Point(margin + labelWidth + 10, y),
            AutoSize = true
        };

        chkOverwrite = new CheckBox
        {
            Text = "Overwrite existing files",
            Location = new Point(margin + labelWidth + 260, y),
            AutoSize = true
        };

        // === BUTTONS ROW ===
        y += rowHeight + 10;
        btnExtract = new Button
        {
            Text = "Extract Audio",
            Location = new Point(margin + labelWidth + 10, y),
            Size = new Size(140, 36),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnExtract.Click += (s, e) => RunExtraction();

        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(margin + labelWidth + 165, y),
            Size = new Size(100, 36),
            Enabled = false
        };
        btnCancel.Click += (s, e) => cts?.Cancel();

        // === PROGRESS BARS ===
        y += 50;
        var lblProgress = new Label
        {
            Text = "Progress:",
            Location = new Point(margin, y),
            AutoSize = true
        };

        y += 22;
        barFiles = new ProgressBar
        {
            Location = new Point(margin, y),
            Size = new Size(ClientSize.Width - margin * 2, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        y += 30;
        barSamples = new ProgressBar
        {
            Location = new Point(margin, y),
            Size = new Size(ClientSize.Width - margin * 2, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // === STATUS ===
        y += 32;
        lblStatus = new Label
        {
            Text = "Ready",
            Location = new Point(margin, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            ForeColor = Color.DimGray
        };

        // === RESULTS LIST ===
        y += 30;
        lvResults = new ListView
        {
            Location = new Point(margin, y),
            Size = new Size(ClientSize.Width - margin * 2, ClientSize.Height - y - margin),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        lvResults.Columns.Add("Source File", 280);
        lvResults.Columns.Add("Status", 80);
        lvResults.Columns.Add("Extracted", 70);
        lvResults.Columns.Add("Failed", 60);
        lvResults.Columns.Add("Output Folder", 350);

        // === ADD ALL CONTROLS ===
        Controls.Add(lblIn);
        Controls.Add(txtInput);
        Controls.Add(btnBrowseIn);
        Controls.Add(lblOut);
        Controls.Add(txtOutput);
        Controls.Add(btnBrowseOut);
        Controls.Add(chkRecursive);
        Controls.Add(chkOverwrite);
        Controls.Add(btnExtract);
        Controls.Add(btnCancel);
        Controls.Add(lblProgress);
        Controls.Add(barFiles);
        Controls.Add(barSamples);
        Controls.Add(lblStatus);
        Controls.Add(lvResults);
    }

    private void BrowseInput()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select FMOD Bank or FSB file",
            Filter = "FMOD Files (*.bank;*.fsb)|*.bank;*.fsb|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtInput.Text = dlg.FileName;
            return;
        }

        using var fbd = new FolderBrowserDialog
        {
            Description = "Select folder containing .bank/.fsb files"
        };

        if (fbd.ShowDialog() == DialogResult.OK)
            txtInput.Text = fbd.SelectedPath;
    }

    private void BrowseOutput()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Select output folder"
        };

        if (fbd.ShowDialog() == DialogResult.OK)
            txtOutput.Text = fbd.SelectedPath;
    }

    private async void RunExtraction()
    {
        if (string.IsNullOrWhiteSpace(txtInput.Text))
        {
            MessageBox.Show("Please select an input file or folder.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        btnExtract.Enabled = false;
        btnCancel.Enabled = true;
        lvResults.Items.Clear();
        cts = new CancellationTokenSource();

        var extractor = new BankExtractor();
        extractor.ProgressChanged += (s, e) => Invoke(() => UpdateUI(e));

        try
        {
            var results = await extractor.ExtractAsync(new ExtractionOptions
            {
                InputPath = txtInput.Text,
                OutputDirectory = txtOutput.Text,
                Recursive = chkRecursive.Checked,
                OverwriteExisting = chkOverwrite.Checked
            }, cts.Token);

            foreach (var r in results)
            {
                var item = new ListViewItem(Path.GetFileName(r.FilePath));
                item.SubItems.Add(r.Success ? "OK" : "FAIL");
                item.SubItems.Add(r.FilesExtracted.ToString());
                item.SubItems.Add(r.FilesFailed.ToString());
                item.SubItems.Add(r.ExtractedFiles.FirstOrDefault() is string f
                    ? Path.GetDirectoryName(f) ?? ""
                    : "");

                if (!r.Success)
                {
                    item.ForeColor = Color.Red;
                    // Add tooltip-like subitem with first error
                    if (r.Errors.Count > 0)
                    {
                        item.SubItems[1].Text = $"FAIL: {r.Errors.First(e => !e.StartsWith("File size") && !e.StartsWith("Extension"))}";
                    }
                }

                // Store full errors in Tag for potential detail view
                item.Tag = r.Errors;
                lvResults.Items.Add(item);
                lvResults.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var item = lvResults.GetItemAt(e.X, e.Y);
                        if (item?.Tag is List<string> errors)
                        {
                            MessageBox.Show(string.Join("\n", errors), "Error Details",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                };
            }

            // After the loop, if any failed, show detailed error dialog
            var failedResults = results.Where(r => !r.Success).ToList();
            if (failedResults.Any())
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Detailed error log:");
                sb.AppendLine();
                foreach (var r in failedResults)
                {
                    sb.AppendLine($"=== {Path.GetFileName(r.FilePath)} ===");
                    foreach (var err in r.Errors)
                        sb.AppendLine($"  {err}");
                    sb.AppendLine();
                }

                // You can MessageBox.Show this, or better - add a "View Details" button
                Console.WriteLine(sb.ToString()); // Or log to file
            }
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Cancelled by user.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExtract.Enabled = true;
            btnCancel.Enabled = false;
            barFiles.Value = 0;
            barSamples.Value = 0;
            cts?.Dispose();
            cts = null;
        }
    }

    private void UpdateUI(ProgressReport report)
    {
        if (report.TotalFiles > 0)
        {
            barFiles.Maximum = Math.Max(report.TotalFiles, 1);
            barFiles.Value = Math.Min(report.CurrentFileIndex, barFiles.Maximum);
        }
        if (report.TotalSamples > 0)
        {
            barSamples.Maximum = Math.Max(report.TotalSamples, 1);
            barSamples.Value = Math.Min(report.CurrentSampleIndex, barSamples.Maximum);
        }
        lblStatus.Text = report.StatusMessage;
    }
}