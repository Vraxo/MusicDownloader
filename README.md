# 🎵 MusicMan

Console app that builds and maintains a music library from a CSV track list. It downloads audio with
yt-dlp, converts it with ffmpeg, embeds tags and cover art, and re-checks everything against the CSV
on later runs.

## 📋 Requirements

| Dependency | Purpose |
| --- | --- |
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Build and run |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Downloading audio |
| [ffmpeg](https://ffmpeg.org/) | Conversion, trimming, looping |

Both tools must be on `PATH` or pointed to from the settings file. The default executable names
(`yt-dlp.exe`, `ffmpeg.exe`) target Windows.

## 🚀 First run

```bash
dotnet run --project MusicMan
```

The first run creates `Data/settings.json` and an empty `Music/base/tracks.csv`, then exits.
Fill in the CSV and run the command again.

## 🎧 Modes

| Command | Behavior |
| --- | --- |
| `dotnet run --project MusicMan` | Sync the library: download missing tracks, repair broken files, update tags and covers |
| `dotnet run --project MusicMan -- process` | Manually process one `.flac` from `Data/`: optional tempo change and trim, written as `<name>_processed.flac` |
| `dotnet run --project MusicMan -- playlist` | Write one `.m3u` playlist per tag into `Music/tracks/` |

`process` prompts for a file, a tempo percentage, and a trim range (`00:15-01:30` or `15-90`).

## 📄 Track database

The database is a CSV file with a header row. Column order does not matter, but column names do.
Fields containing commas or quotes must be quoted as usual.

| Column | Description |
| --- | --- |
| `source` | URL passed to yt-dlp. Also stored in the file's Comment tag, so renamed or moved files are recognized |
| `title` | Track title, used as the output file name |
| `artist` | Performer tag |
| `album` | Album tag and the subfolder name under `Music/tracks/` |
| `date` | Release date; only the first four characters (the year) reach the tag |
| `track_number` | Track number tag |
| `album_artist` | Album artist tag |
| `composer` | Composer tag |
| `cover` | Image file name inside `Music/covers/`. Blank means `<title>.png`; `none` embeds no cover |
| `range` | Trim points separated by `;`, e.g. `00:15;01:30`. Leave blank for the full track |
| `tempo` | Speed in percent: `110` is 10% faster, `95` is 5% slower |
| `loop` | Repeat count. Blank or `1` means no looping |
| `tags` | Semicolon-separated list, written as genres and used to generate playlists |

```csv
source,title,artist,album,date,track_number,album_artist,composer,cover,range,tempo,loop,tags
https://www.youtube.com/watch?v=aaaaaaaaaaa,Opening Theme,Example Artist,First Album,2024-03-01,1,Example Artist,Someone Else,opening.png,00:00;01:30,100,2,Favorites;Live
https://www.youtube.com/watch?v=bbbbbbbbbbb,Second Track,Example Artist,First Album,2024-03-01,2,,,,,,,Favorites
```

## ⚙️ Settings

`Data/settings.json` is created with defaults on first run.

| Key | Default | Description |
| --- | --- | --- |
| `DatabaseDir` | `Music/base` | Folder containing `tracks.csv` |
| `BaseDataDir` | `Music/tracks` | Library root for audio files and playlists |
| `CoversDir` | `Music/covers` | Cover art folder |
| `YtDlpExe` | `yt-dlp.exe` | yt-dlp executable name |
| `YtDlpDir` | `""` | Folder containing yt-dlp; blank searches `PATH` |
| `FfmpegExe` | `ffmpeg.exe` | ffmpeg executable name |
| `FfmpegDir` | `""` | Folder containing ffmpeg; blank searches `PATH` |
| `CookiesBrowser` | `firefox` | Browser to read cookies from. Set to `""` to use `CookieFile` instead |
| `CookieFile` | `Data/cookies.txt` | Cookie file, used when `CookiesBrowser` is empty |
| `PreservePitchWhenChangingTempo` | `false` | `false` resamples, so pitch changes with speed; `true` keeps the pitch |
| `DelayBetweenDownloadsMs` | `2500` | Pause after each successful download |

If yt-dlp cannot read browser cookies, export a `cookies.txt` instead: set `CookiesBrowser` to `""`
and place the file at `CookieFile`.

## 📁 Output layout

```
Data/
  settings.json
  cookies.txt            (optional)
Music/
  base/
    tracks.csv
  covers/
    opening.png
  tracks/
    First Album/
      Opening Theme.opus
    Favorites.m3u
```

## 🔄 How a sync works

1. Read `Music/base/tracks.csv`.
2. Scan the library and match files by the source URL in their Comment tag, moving files whose album
   or title changed.
3. Compare tags and cover art with the CSV. Up-to-date files are skipped, metadata mismatches are
   fixed in place, and unreadable files are deleted and downloaded again.
4. Download missing tracks, retry a failed download once, convert and tag the audio, then move it into
   `Music/tracks/<album>/`.
5. Print a summary of downloaded, updated, failed, and up-to-date tracks.

## 🛠️ Build and test

```bash
dotnet build MusicMan/MusicMan.csproj
dotnet test MusicMan.Tests/MusicMan.Tests.csproj
```

Tests cover CSV parsing, file name handling, and the generated yt-dlp/ffmpeg arguments. They need no
network access or media files. Build output goes to `artifacts/`, and warnings are treated as errors.

| Folder | Contents |
| --- | --- |
| `MusicMan/Stages` | Fetching, processing, and storage steps |
| `MusicMan/Orchestration` | Queue handling, library scanning, cover art |
| `MusicMan/Infrastructure` | Settings, logging, process execution, paths |
| `MusicMan/Core` | Settings and track models |
| `MusicMan.Tests` | xUnit tests |

## ⚠️ Limitations

- `range`, `tempo`, and `loop` are applied while downloading. Editing them for an existing file does
  not re-render it; delete the file to force a fresh download.
- Tag comparison is strict, so edits made in another tag editor are overwritten on the next run.
- Downloads run one at a time, with a delay between them.
- Only download content you have the rights to, and follow the terms of the sites you download from.
