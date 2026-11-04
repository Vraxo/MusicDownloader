namespace MusicMan.Core;

/// <summary>
/// How a freshly downloaded thumbnail is shaped before it is stored in the covers folder.
/// Existing cover files are never re-processed, so changing this only affects new downloads.
/// </summary>
internal enum CoverFitMode
{
    /// <summary>Store the thumbnail byte-for-byte as downloaded (default).</summary>
    Original,

    /// <summary>Square image, centred crop. Loses the parts of the frame outside the crop.</summary>
    SquareCrop,

    /// <summary>Square image, scaled to fit and letterboxed on a solid background. Loses nothing.</summary>
    SquarePad
}
