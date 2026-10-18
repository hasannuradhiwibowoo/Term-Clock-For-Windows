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

- The live clock uses 24-hour format by default.
- `--12-hour` displays time in 12-hour format.
- `--utc` displays UTC.
- `--once` prints the time and exits, which is useful in scripts.
- `--help` and `--version` print app information.

## Publish for Windows

Requires the .NET 8 SDK:

```powershell
dotnet publish .\TermClock.csproj -c Release -r win-x64 --self-contained false
```

The publish folder is `bin\Release\net8.0\win-x64\publish`. This framework-dependent build is small and requires the .NET 8 Runtime on the target computer. A WinGet manifest can declare that runtime as a package dependency.

## WinGet

The project can be distributed as a portable WinGet package. Before submission, it needs a stable public release URL and a unique package identifier under the publisher's name. The app name and package ID are provisional until chosen.
