# TermClock

A small, colorful clock for the terminal. Run it directly from PowerShell, Windows Terminal, or Command Prompt.

## Run

After publishing, run `termclock.exe`. Press **Q** or **Esc** to exit.

```powershell
.\termclock.exe
.\termclock.exe --24-hour
.\termclock.exe --utc
.\termclock.exe --once
```

Options:

- `--24-hour` displays time in 24-hour format.
- `--utc` displays UTC.
- `--once` prints the time and exits, which is useful in scripts.
- `--help` and `--version` print app information.

## Publish for Windows

Requires the .NET 8 SDK:

```powershell
dotnet publish .\TermClock.csproj -c Release -r win-x64 --self-contained false
```

The publish folder is `bin\Release\net8.0\win-x64\publish`. The framework-dependent build requires the .NET 8 Runtime on the target computer.

## License

Licensed under the MIT License. See [LICENSE](LICENSE).

## WinGet

The draft manifests are in `winget/manifests/h/hasannuradhiwibowoo/TermClock/0.1.0`. The installer manifest expects a GitHub Release tagged `v0.1.0` with asset `TermClock-0.1.0-win-x64.zip` and declares the .NET 8 Runtime dependency. The release asset still needs to be uploaded before submitting the manifests to the WinGet community repository.
