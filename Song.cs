using System;
using System.Diagnostics;

namespace QuickPlay {
    public readonly struct Song : IEquatable<Song> {
        public readonly string Name;
        public readonly string Artist;
        public Song(string Name = null, string Artist = null) {
            this.Name = Name ?? "Never Gonna Give You Up";
            this.Artist = Artist ?? @"Rick Astley";
        }

        /// <summary>
        /// Returns a new song using the Spotify format "Artist - Track"
        /// </summary>
        /// <param name="SpotifyProcessTitle"></param>
        public Song(string SpotifyProcessTitle) {
            int FirstIndexer = SpotifyProcessTitle.IndexOf(" - ", StringComparison.Ordinal);
            if (FirstIndexer < 0) {
                Name = SpotifyProcessTitle;
                Artist = "";
                return;
            }
            Name = SpotifyProcessTitle.Substring(FirstIndexer + 3).Trim(' ');
            Artist = SpotifyProcessTitle.Substring(0, FirstIndexer).Trim(' ');
        }

        /// <summary>
        /// Finds the MainWindowTitle of the given SpotifyProcess and parses it to the Spotify Song constructor
        /// </summary>
        /// <param name="SpotifyProcess"></param>
        /// <returns></returns>
        public static Song? GetSpotifySong(Process SpotifyProcess) {
            if (SpotifyProcess == null) { return null; }
            try {
                string ProcessTitle = SpotifyProcess.MainWindowTitle;
                if (string.IsNullOrEmpty(ProcessTitle)) { return null; }
                return new Song(ProcessTitle);
#pragma warning disable CA1031 // Do not catch general exception types
            } catch (ArgumentException) { //Process doesn't exist
#pragma warning restore CA1031 // Do not catch general exception types
                return null;
            }
        }

        public (string Track, string Artist) Tuple => (Name, Artist);

        #region Generic Overrides
        public override string ToString() => $"\"{Name}\" by {Artist}";

        public override bool Equals(object Obj) => Obj is Song Song && Equals(Song);

        public override int GetHashCode() {
            unchecked { return((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Artist != null ? Artist.GetHashCode() : 0); }
        }

        public bool Equals(Song Other) =>
            string.Equals(Name, Other.Name, StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(Artist, Other.Artist, StringComparison.InvariantCultureIgnoreCase);

        public static bool operator ==(Song Left, Song Right) => Left.Equals(Right);

        public static bool operator !=(Song Left, Song Right) => !(Left.Equals(Right));
        #endregion
    }
}
