using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using Ookii.Dialogs.Wpf;

namespace QuickPlay {
    public partial class MainWindow : INotifyPropertyChanged  {
        static DispatcherTimer _PollTimer;

        string _CurrentlyPlayingFileFormat;

        int _TrackMinSeparatorLength;
        string _TrackSeparator;
        int _ArtistMinSeparatorLength;
        string _ArtistSeparator;

        Song _LastSong;

        public MainWindow() {
            DataContext = this;
            InitializeComponent();
            CurrentlyPlayingFile.SelectedPath = $"{ExecutingLocation()}\\SpotifyNowPlaying.txt";
            CurrentTrackFile.SelectedPath = $"{ExecutingLocation()}\\SpotifyNowPlayingTrack.txt";
            CurrentTrackFileOverflow.SelectedPath = $"{ExecutingLocation()}\\SpotifyNowPlayingTrackOverflow.txt";
            CurrentArtistFile.SelectedPath = $"{ExecutingLocation()}\\SpotifyNowPlayingArtist.txt";
            CurrentArtistFileOverflow.SelectedPath = $"{ExecutingLocation()}\\SpotifyNowPlayingArtistOverflow.txt";

            _CurrentlyPlayingFileFormat = CurrentlyPlayingFormat.Text;

            _TrackMinSeparatorLength = (int)(CurrentTrackSeparatorMinLength.Value ?? 40d);
            _TrackSeparator = CurrentTrackSeparator.Text;

            _ArtistMinSeparatorLength = (int)(CurrentArtistSeparatorMinLength.Value ?? 40d);
            _ArtistSeparator = CurrentArtistSeparator.Text;

            CheckUseTrackSeparator.IsChecked = Properties.Settings.Default.TrackOverflow;
            CheckUseArtistSeparator.IsChecked = Properties.Settings.Default.ArtistOverflow;
        }

        #region File Management
        public static DirectoryInfo ExecutingLocation() => ExecutingApplication().Directory;

        public static FileInfo ExecutingApplication() => new FileInfo(Assembly.GetExecutingAssembly().Location);

        public static FileInfo DefaultSaveLocation(string FileName = "SpotifyNowPlaying.txt") => new FileInfo(ExecutingLocation().FullName + "\\" + FileName);

        public static FileInfo GetSaveLocation(string DefaultFileName = "SpotifyNowPlaying.txt") {
            VistaSaveFileDialog SaveDialog = new VistaSaveFileDialog {
                AddExtension = true,
                DefaultExt = ".txt",
                FileName = DefaultFileName,
                Filter = "Text File (*.txt)|*.txt|Any File (*.*)|*.*",
                FilterIndex = 0,
                InitialDirectory = ExecutingLocation().FullName,
                OverwritePrompt = true,
                Title = "Pick a save location",
                ValidateNames = true
            };

            switch (SaveDialog.ShowDialog()) {
                case true:
                    return new FileInfo(SaveDialog.FileName);
            }

            return DefaultSaveLocation(DefaultFileName);
        }
        #endregion

        #region Timer
        public void CreatePollTimer() {
            if (_PollTimer != null) {
                Debug.WriteLine("Timer is already running!", "Warning");
                return;
            }

            _PollTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = new TimeSpan((long)(Properties.Settings.Default.PollRate * 10000d)) };
            _PollTimer.Tick += _PollTimer_Tick;
            _PollTimer.Start();
        }

        void _PollTimer_Tick(object Sender, EventArgs E) => WriteSongFiles();
        #endregion

        #region Spotify Process Finding
        /// <summary>
        /// Returns the first found Spotify Process containing a title; or null
        /// </summary>
        /// <returns></returns>
        public static Process GetSpotifyProcess() => Process.GetProcessesByName(Properties.Settings.Default.SpotifyProcessName).FirstOrDefault(P => !string.IsNullOrEmpty(P.MainWindowTitle));

        public static Song? GetCurrentSong() => Song.GetSpotifySong(GetSpotifyProcess());

        #endregion

        #region Value Bindings
        bool _UseTrackSeparator;
        public bool UseTrackSeparator {
            get => _UseTrackSeparator;
            set {
                if (_UseTrackSeparator != value) {
                    Properties.Settings.Default.TrackOverflow = value;
                    Properties.Settings.Default.Save();
                    _UseTrackSeparator = value;
                    OnPropertyChanged();
                }
            }
        }

        bool _UseArtistSeparator;
        public bool UseArtistSeparator {
            get => _UseArtistSeparator;
            set {
                if (_UseArtistSeparator != value) {
                    Properties.Settings.Default.ArtistOverflow = value;
                    Properties.Settings.Default.Save();
                    _UseArtistSeparator = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        #endregion

        public void WriteSongFiles() {
            Song? LatestSong = GetCurrentSong();
            if (!LatestSong.HasValue || _LastSong == LatestSong) { return; }
            _LastSong = LatestSong.Value;
            (string TrackName, string TrackArtist) = LatestSong.Value.Tuple;

            string SaveTrackOv = CurrentTrackFileOverflow.SelectedPath;
            string SaveTrack = CurrentTrackFile.SelectedPath;
            string SaveArtistOv = CurrentArtistFileOverflow.SelectedPath;
            string SaveArtist = CurrentArtistFile.SelectedPath;
            string Save = CurrentlyPlayingFile.SelectedPath;

            if (UseTrackSeparator && _TrackMinSeparatorLength >= 0 && TrackName.Length >= _TrackMinSeparatorLength) {
                File.WriteAllText(SaveTrackOv, TrackName + _TrackSeparator);
                File.WriteAllText(SaveTrackOv, string.Empty);
            } else {
                File.WriteAllText(SaveTrackOv, string.Empty);
                File.WriteAllText(SaveTrack, TrackName);
            }

            if (UseArtistSeparator && _ArtistMinSeparatorLength >= 0 && TrackArtist.Length >= _ArtistMinSeparatorLength) {
                File.WriteAllText(SaveArtistOv, TrackArtist + _ArtistSeparator);
                File.WriteAllText(SaveArtist, string.Empty);
            } else {
                File.WriteAllText(SaveArtistOv, string.Empty);
                File.WriteAllText(SaveArtist, TrackArtist);
            }

            File.WriteAllText(Save, _CurrentlyPlayingFileFormat?.Replace("%%track%%", TrackName).Replace("%%artist%%", TrackArtist) ?? string.Empty);
            Debug.WriteLine("E");
        }

        #region XAML Value Updaters
        void CurrentlyPlayingFormat_TextInput(object Sender, System.Windows.Input.TextCompositionEventArgs E) {
            if (Sender != null && Sender is TextBlock Tb) {
                _CurrentlyPlayingFileFormat = Tb.Text;
            }
        }

        void CurrentTrackSeparatorMinLength_ValueChanged(object Sender, RoutedPropertyChangedEventArgs<double?> E) {
            if (Sender != null) {
                _TrackMinSeparatorLength = (E.NewValue.HasValue ? (int)E.NewValue.Value : -1);
            }
        }

        void CurrentArtistSeparatorMinLength_ValueChanged(object Sender, RoutedPropertyChangedEventArgs<double?> E) {
            if (Sender != null) {
                _ArtistMinSeparatorLength = (E.NewValue.HasValue ? (int)E.NewValue.Value : -1);
            }
        }

        void CurrentTrackSeparator_TextChanged(object Sender, TextChangedEventArgs E) {
            if (Sender != null && Sender is TextBox Tb) {
                _TrackSeparator = Tb.Text;
            }
        }

        void CurrentArtistSeparator_TextChanged(object Sender, TextChangedEventArgs E) {
            if (Sender != null && Sender is TextBox Tb) {
                _ArtistSeparator = Tb.Text;
            }
        }
        #endregion

        void StartButton_Click(object Sender, RoutedEventArgs E) {
            StartGrid.IsEnabled = false;

            GetSpotifyProcess();
            CreatePollTimer();
        }
    }
}
