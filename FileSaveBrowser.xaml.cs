using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using Ookii.Dialogs.Wpf;

namespace QuickPlay {
    public partial class FileSaveBrowser {
        public FileSaveBrowser() {
            InitializeComponent();

        }
        public static DirectoryInfo ExecutingLocation() => ExecutingApplication().Directory;

        public static FileInfo ExecutingApplication() => new FileInfo(Assembly.GetExecutingAssembly().Location);

        void BrowseButton_Click(object Sender, RoutedEventArgs E) {
            VistaSaveFileDialog SaveDialog = new VistaSaveFileDialog {
                AddExtension = true,
                DefaultExt = ".txt",
                Filter = "Text File (*.txt)|*.txt|Any File (*.*)|*.*",
                FilterIndex = 0,
                FileName = SelectedPath,
                InitialDirectory = ExecutingLocation().FullName,
                OverwritePrompt = true,
                Title = "Pick a save location",
                ValidateNames = true
            };

            switch (SaveDialog.ShowDialog()) {
                case true:
                    PathTextBox.Text = SaveDialog.FileName;
                    break;
            }
        }

        #region Dependency Properties

        public string SelectedPath {
            get => (string)GetValue(SelectedPathProperty);
            set => SetValue(SelectedPathProperty, value);
        }

        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register(
                "SelectedPath",
                typeof(string),
                typeof(FileSaveBrowser),
                new FrameworkPropertyMetadata(SelectedPathChanged) {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

        static void SelectedPathChanged(DependencyObject D, DependencyPropertyChangedEventArgs E) {
            ((FileSaveBrowser)D).PathTextBox.Text = E.NewValue.ToString();
        }

        #endregion

        void PathTextBox_LostKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E) {
            SelectedPath = PathTextBox.Text;
        }
    }
}
