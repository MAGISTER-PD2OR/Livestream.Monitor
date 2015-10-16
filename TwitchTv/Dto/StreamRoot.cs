namespace TwitchTv.Dto
{
    /// <summary>
    /// JSON root for the stream information, contains additional data we dont care about
    /// </summary>
    public class StreamRoot
    {
        public Stream Stream { get; set; }
    }
}