# FMOD Bank Audio Extractor

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/juicemj14salt/FmodBankRipper)


A .NET 8.0 application that extracts audio files from **FMOD Studio Bank files** (`.bank`) and **FMOD Sound Bank files** (`.fsb`) into standard formats like `.wav` and `.ogg`.

Includes both a **command-line interface (CLI)** for scripting/automation and a **Windows Forms GUI** for easy point-and-click extraction.

---

## Features

- Extract audio from `.bank` and `.fsb` files
- Rebuilds FMOD 5 (FSB5) samples to standard WAV/OGG using [Fmod5Sharp](https://www.nuget.org/packages/Fmod5Sharp)
- Scans `.bank` containers for embedded FSB5 chunks automatically
- **CLI** — batch process, recursive folder scanning, automation-friendly
- **GUI** — drag-and-drop friendly with real-time progress bars
- Zero native dependencies — pure managed C#
- Supports recursive directory scanning
- Duplicate file handling with auto-renaming

---

## Screenshots

### GUI
<img width="782" height="597" alt="Screenshot 2026-07-13 214007" src="https://github.com/user-attachments/assets/430ebb2a-aa56-4b74-8f7c-3cea1bbe706c" />

### CLI
`FmodBankRipper.CLI -i "C:\Game\Audio" -o "C:\Output" -r`

in the cmd, type --help for more info because I dont wanna say anything, oh and it can only read .bank and .fsb files that specfically use version 5 of FMOD, incase FMOD Bank Tools somehow stops working for you, use this one, It's not the best but it gets the job done of exporting FMOD file contents to .wav or .ogg depending on what was in the file

### Credits
All libraries go to their rightful owners, including @SamboyCoding for the Fmod5Sharp library,


