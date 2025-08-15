using MusicDownloader;
using System;

namespace PlaylistSorterApp;

internal class Program
{
    static void Main(string[] args)
    {
        PlaylistSorter.SortAllPlaylists();

        Console.WriteLine();
        Log.Info("Press any key to exit...");
        Console.ReadKey();
    }
}