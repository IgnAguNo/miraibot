namespace DiscordBot
{
    struct SongData
    {
        public string Name;
        public string Uri;

        public SongData(string Name, string Uri)
        {
            this.Name = Name;
            this.Uri = Uri;
        }
    }
}
