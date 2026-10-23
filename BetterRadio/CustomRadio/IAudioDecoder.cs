namespace BetterRadio.CustomRadio
{
    // Common interface for the custom radio's format decoders.
    // Implementations must be safe to call from a background thread - no Unity API calls inside Decode.
    public interface IAudioDecoder
    {
        bool CanDecode(string path);

        DecodedAudio Decode(string path);
    }
}
