using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Diagnostics;

namespace SlidePlayer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	// TODO Make buttons for taskbar thumbnail
	// TODO Add sound play on button press
	// TODO Design icon depicting play button riding a snowboard
	public partial class MainWindow : Window
	{
		public static RoutedCommand addToPlaylistCommand = new RoutedCommand("AddToPlaylist", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Shift | ModifierKeys.Control) });
		public static RoutedCommand removeFromPlaylistCommand = new RoutedCommand("RemoveFromPlaylist", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.Delete) });
		public static RoutedCommand loadPlaylistCommand = new RoutedCommand("LoadPlaylist", typeof(MainWindow), new InputGestureCollection() { new KeyGesture(Key.L, ModifierKeys.Control) });

		private readonly SolidColorBrush marked = new SolidColorBrush(Color.FromRgb(200, 200, 255));
		private readonly SolidColorBrush unmarked = new SolidColorBrush(Color.FromRgb(255, 255, 255));

		public DispatcherTimer timer;
		public SlidableSoundStream stream;

		private bool _thumbActivated;
		private bool _slidingPauseEnabled;
		private PanelForPlaylistItem _currentPlaylistPosition;
		private PlaybackMode _playbackMode;
		private string[] _allowedExtensions = { ".MP3", ".WAV" };

		private PlaylistRandomizer playlistRandomizer;

		public MainWindow()
		{
			InitializeComponent();
			_playbackMode = Properties.Settings.Default.PlaybackMode;
			_slidingPauseEnabled = Properties.Settings.Default.SlidingPauseEnabled;
			PlaybackModeBtn.Content = Enum.GetName(typeof(PlaybackMode), _playbackMode);
			playlistRandomizer = new PlaylistRandomizer(Playlist.Items);
			PlayPauseBtn.Focus();
		}

		public string SetTitle(string filepath) => Title = $"{filepath} - Slide Player";

		private string ChooseAudioFileAndGetFilePath(string title)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog()
			{
				Title = title,
				Filter = "MP3 audio file (*.mp3)|*.mp3|Wave audio file (*.wav)|*.wav"
			};
			return openFileDialog.ShowDialog(this) == true ? openFileDialog.FileName : string.Empty;
		}

		private void OpenAudioFile(string filename)
		{
			timer?.Stop();
			stream?.Dispose();
			try
			{
				stream = new SlidableSoundStream(filename, Pause)
				{
					Volume = (float)VolumeSlider.Value,
					SlidingDuration = Properties.Settings.Default.SlidingDuration
				};
				SetTitle(filename);
				MainSlider.Value = 0;
				MainSlider.Maximum = stream.Duration;
				if (PlayPauseBtn.Content as string == "Play")
					SwitchToPause();
				if (!_thumbActivated)
				{
					#region Dr. WPF's code (https://social.msdn.microsoft.com/Forums/vstudio/en-US/5fa7cbc2-c99f-4b71-b46c-f156bdf0a75a/making-the-slider-slide-with-one-click-anywhere-on-the-slider)e
					Thumb thumb = (MainSlider.Template.FindName("PART_Track", MainSlider) as Track).Thumb;
					thumb.MouseEnter += new MouseEventHandler(Thumb_MouseEnter);
					#endregion
					_thumbActivated = true;
					PlayPauseBtn.IsEnabled =
					StopBtn.IsEnabled =
					MainSlider.IsEnabled =
					VolumeSlider.IsEnabled =
					PrevBtn.IsEnabled =
					NextBtn.IsEnabled =
					RecBtn.IsEnabled =
					PlaybackModeBtn.IsEnabled =
					PrevThumbBtn.IsEnabled =
					PlayPauseThumbBtn.IsEnabled =
					NextThumbBtn.IsEnabled =
					MuteBtn.IsEnabled = true;
				}
				if (timer == null)
				{
					timer = new DispatcherTimer
					{
						Interval = TimeSpan.FromMilliseconds(20)
					};
					timer.Tick += UpdateMainSlider;
				}
				stream.Play();
				timer.Start();
			}
			catch (FileFormatException e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void UpdateMainSlider(object state = null, EventArgs args = null)
		{
			MainSlider.Value = stream.CurrentTime;
			if ((stream.CurrentTime / (float)stream.Duration) >= 0.995f)
			{
				switch (_playbackMode)
				{
					case PlaybackMode.PlayOne:
						StopButton_Click();
						break;
					case PlaybackMode.PlayNext:
						NextButton_Click();
						break;
					case PlaybackMode.LoopPlaylist:
						SetNewPlaylistPosition((Playlist.Items.IndexOf(_currentPlaylistPosition) + 1) % Playlist.Items.Count);
						OpenAudioFile(_currentPlaylistPosition.FileName);
						break;
					case PlaybackMode.LoopOne:
						MainSlider.Value = stream.CurrentTime = 0;
						break;
					case PlaybackMode.Random:
						PlayRandomPosition();
						break;
				}
			}
		}

		private void PlayRandomPosition()
		{
			SetNewPlaylistPosition(playlistRandomizer.Randomize());
			OpenAudioFile(_currentPlaylistPosition.FileName);
		}

		private PanelForPlaylistItem GeneratePlaylistItem(string filepath)
		{
			PanelForPlaylistItem listBoxItem = new PanelForPlaylistItem(filepath)
			{
				ToolTip = filepath,
				Width = Playlist.ActualWidth
			};
			listBoxItem.MouseDoubleClick += OpenFileFromPlaylist_Click;
			listBoxItem.ContextMenu = FindResource("cmPlaylistItem") as ContextMenu;
			return listBoxItem;
		}

		private void AddToPlaylist(PanelForPlaylistItem item)
		{
			Playlist.Items.Add(item);
			playlistRandomizer.EvaluateChances((a,b) => a + b, item.ProbabilityFactor);
		}

		private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			string filepath = ChooseAudioFileAndGetFilePath("Open audio file");
			if (filepath != string.Empty)
			{
				Playlist.Items.Clear();
				_currentPlaylistPosition = GeneratePlaylistItem(filepath);
				AddToPlaylist(_currentPlaylistPosition);
				_currentPlaylistPosition.Background = marked;
				OpenAudioFile(filepath);
			}
		}

		private void AddToPlaylistCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			string filepath = ChooseAudioFileAndGetFilePath("Add audio file to playlist");
			if (filepath != string.Empty)
			{
				AddToPlaylist(GeneratePlaylistItem(filepath));
			}
		}

		private void PauseThumbButton_Click(object sender, EventArgs e) => PrepareToPause();

		private void PauseButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			PrepareToPause();
			e.Handled = true;
		}

		private async void PrepareToPause()
		{
			if (_slidingPauseEnabled)
			{
				PlayPauseBtn.IsEnabled = false;
				StopBtn.IsEnabled = false;
				await stream.StopPlaybackGradually();
			}
			else
			{
				stream.Pause();
				timer.Stop();
				SwitchToPlay();
			}
		}

		private void PlayThumbButton_Click(object sender, EventArgs e) => PlayButton_Click(sender);

		private void PlayButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			stream.Play();
			timer.Start();
			SwitchToPause();
		}

		private void SwitchToPlay()
		{
			PlayPauseBtn.Click -= PauseButton_Click;
			PlayPauseBtn.Click += PlayButton_Click;
			PlayPauseBtn.Content = "Play";
			PlayPauseThumbBtn.Click -= PauseThumbButton_Click;
			PlayPauseThumbBtn.Click += PlayThumbButton_Click;
			PlayPauseThumbBtn.ImageSource = FindResource("playDrawingImage") as DrawingImage;
		}

		private void SwitchToPause()
		{
			PlayPauseBtn.Click += PauseButton_Click;
			PlayPauseBtn.Click -= PlayButton_Click;
			PlayPauseBtn.Content = "Pause";
			PlayPauseThumbBtn.Click += PauseThumbButton_Click;
			PlayPauseThumbBtn.Click -= PlayThumbButton_Click;
			PlayPauseThumbBtn.ImageSource = FindResource("pauseDrawingImage") as DrawingImage;
		}

		private void StopButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			stream.StopNormally();
			timer.Stop();
			MainSlider.Value = 0;
			SwitchToPlay();
		}

		private void PrevThumbButton_Click(object sender = null, EventArgs e = null) => PrevButton_Click();

		private void PrevButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			int index = Playlist.Items.IndexOf(_currentPlaylistPosition);
			index--;
			if (_playbackMode == PlaybackMode.Random)
			{
				PlayRandomPosition();
				OpenAudioFile(_currentPlaylistPosition.FileName);
			}
			else if (index > -1)
			{
				SetNewPlaylistPosition(index);
				OpenAudioFile(_currentPlaylistPosition.FileName);
			}
		}

		private void NextThumbButton_Click(object sender, EventArgs e) => NextButton_Click();

		private void NextButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			int index = Playlist.Items.IndexOf(_currentPlaylistPosition);
			index++;
			if (_playbackMode == PlaybackMode.Random)
			{
				PlayRandomPosition();
				OpenAudioFile(_currentPlaylistPosition.FileName);
			}
			else if (index < Playlist.Items.Count)
			{
				SetNewPlaylistPosition(index);
				OpenAudioFile(_currentPlaylistPosition.FileName);
			}
			else if (_playbackMode == PlaybackMode.LoopPlaylist)
			{
				SetNewPlaylistPosition(0);
				OpenAudioFile(_currentPlaylistPosition.FileName);
			}
		}

		private void RecButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			if (RecBtn.IsChecked == true)
			{
				stream.Rec();
			}
			else
			{
				stream.StopRec();
				SaveFileDialog saveFileDialog = new SaveFileDialog()
				{
					Filter = "*.wav record|*.wav",
					Title = "Save record"
				};
				if (saveFileDialog.ShowDialog() == true)
				{
					File.Copy("sound.tmp", saveFileDialog.FileName);
				}
				File.Delete("sound.tmp");
			}
		}

		private void SetNewPlaylistPosition(int index)
		{
			if (_currentPlaylistPosition != null)
				_currentPlaylistPosition.Background = unmarked;
			_currentPlaylistPosition = Playlist.Items[index] as PanelForPlaylistItem;
			_currentPlaylistPosition.Background = marked;
		}

		private void PlaybackModeButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			_playbackMode = Enum.IsDefined(typeof(PlaybackMode), _playbackMode + 1) ? _playbackMode + 1 : 0;
			Properties.Settings.Default.PlaybackMode = _playbackMode;
			PlaybackModeBtn.Content = Enum.GetName(typeof(PlaybackMode), _playbackMode);
		}

		private void MuteButton_Click(object sender = null, RoutedEventArgs e = null)
		{
			stream.Volume = MuteBtn.IsChecked == true ? 0 : (float) VolumeSlider.Value;
			if (MuteBtn.IsChecked == true)
				VolumeSlider.ValueChanged -= VolumeSlider_ValueChanged;
			else
				VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
		}

		private void CanPlayOrPause(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = stream != null;

		private void PlayOrPauseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			switch (PlayPauseBtn.Content as string)
			{
				case "Play":
					PlayButton_Click(sender, e);
					break;
				case "Pause":
					PauseButton_Click(sender, e);
					break;
				default: break;
			}
		}

		private void Pause()
		{
			timer.Stop();
			SwitchToPlay();
			PlayPauseBtn.IsEnabled = true;
			StopBtn.IsEnabled = true;
			PlayPauseBtn.Focus();
		}

		private void MainSlider_ThumbDragStart(object sender, DragStartedEventArgs e)
		{
			timer.Stop();
		}

		private void MainSlider_ThumbDragStop(object sender, DragCompletedEventArgs e)
		{
			stream.CurrentTime = MainSlider.Value;
			timer.Start();
		}

		private void Thumb_MouseEnter(object sender, MouseEventArgs e)
		{
			#region Dr. WPF's code (https://social.msdn.microsoft.com/Forums/vstudio/en-US/5fa7cbc2-c99f-4b71-b46c-f156bdf0a75a/making-the-slider-slide-with-one-click-anywhere-on-the-slider)
			if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
			{

				// the left button is pressed on mouse enter

				// but the mouse isn't captured, so the thumb

				// must have been moved under the mouse in response

				// to a click on the track.

				// Generate a MouseLeftButtonDown event.

				timer.Stop();
				MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
				{
					RoutedEvent = MouseLeftButtonDownEvent
				};

				(sender as Thumb)?.RaiseEvent(args);
			}
			#endregion
		}

		private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (stream != null)
				stream.Volume = (float)e.NewValue;
		}

		private void Playlist_DragEnter(object sender, DragEventArgs e)
		{
			bool dropEnabled = false;
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
			{
				string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				List<string> correctFilenames = filenames.Where(fn => _allowedExtensions.Contains(Path.GetExtension(fn).ToUpper())).ToList();
				dropEnabled = correctFilenames.Count == 0 ? false : true;
			}
			if (!dropEnabled)
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;
			}
		}

		private void OpenFileFromPlaylist_Click(object sender, RoutedEventArgs e)
		{
			SetNewPlaylistPosition(Playlist.Items.IndexOf(sender as PanelForPlaylistItem));
			OpenAudioFile(_currentPlaylistPosition.FileName);
		}

		private void Playlist_Drop(object sender, DragEventArgs e)
		{
			string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
			List<string> correctFilenames = filenames.Where(fn => _allowedExtensions.Contains(Path.GetExtension(fn).ToUpper())).ToList();
			foreach (string filename in correctFilenames)
			{
				AddToPlaylist(GeneratePlaylistItem(filename));
			}
		}

		private void EditPlaylistPosition_Click(object sender, RoutedEventArgs e)
		{
			PanelForPlaylistItem panelForPlaylistItem = Playlist.SelectedItem as PanelForPlaylistItem;
			PlaylistPositionSettingsWindow playlistPositionSettingsWindow = new PlaylistPositionSettingsWindow()
			{
				ProbabilityFactor = panelForPlaylistItem.ProbabilityFactor
			};
			playlistPositionSettingsWindow.ShowDialog();
			if (playlistPositionSettingsWindow.DialogResult == true)
			{
				int oldFactor = panelForPlaylistItem.ProbabilityFactor;
				panelForPlaylistItem.ProbabilityFactor = playlistPositionSettingsWindow.ProbabilityFactor;
				playlistRandomizer.ChangeChances(oldFactor, playlistPositionSettingsWindow.ProbabilityFactor);
			}
		}

		private void RemoveFileFromPlaylist_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			playlistRandomizer.EvaluateChances((a, b) => a - b, (Playlist.SelectedItem as PanelForPlaylistItem).ProbabilityFactor);
			Playlist.Items.RemoveAt(Playlist.Items.IndexOf(Playlist.SelectedItem));
		}

		private void RemoveFileFromPlaylist_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Playlist.SelectedItem != null;

		#region spp file handling
		private void LoadPlaylist_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog()
			{
				Title = "Open Slide Player playlist",
				Filter = "Slide Player playlist (*.spp)|*.spp"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				Playlist.Items.Clear();
				_currentPlaylistPosition = null;
				var newPlaylist = PlaylistFile.Read(openFileDialog.FileName);
				foreach (var item in newPlaylist)
				{
					item.MouseDoubleClick += OpenFileFromPlaylist_Click;
					item.ContextMenu = FindResource("cmPlaylistItem") as ContextMenu;
					AddToPlaylist(item);
				}
			}
		}

		private void SavePlaylist_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog()
			{
				Title = "Save Slide Player playlist",
				Filter = "Slide Player playlist (*.spp)|*.spp",
				FileName = "spplaylist.spp",
				OverwritePrompt = true
			};
			if (saveFileDialog.ShowDialog() == true)
			{
				PlaylistFile.Write(saveFileDialog.FileName, Playlist.Items.Cast<PanelForPlaylistItem>().ToList());
			}
		}

		private void CanSavePlaylist(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Playlist.Items.Count > 0;
		#endregion

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow()
			{
				SlidingPause = _slidingPauseEnabled,
				SlidingDuration = Properties.Settings.Default.SlidingDuration / 1000.0f
			};
			settingsWindow.ShowDialog();
			if (settingsWindow.DialogResult == true)
			{
				Properties.Settings.Default.SlidingPauseEnabled = _slidingPauseEnabled = settingsWindow.SlidingPause;
				Properties.Settings.Default.SlidingDuration = (int)Math.Round(settingsWindow.SlidingDuration * 1000.0f);
				if (stream != null)
					stream.SlidingDuration = Properties.Settings.Default.SlidingDuration;
			}
		}

		private void Exit(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.Save();
			stream?.Dispose();
			base.OnClosing(e);
		}
	}
}
