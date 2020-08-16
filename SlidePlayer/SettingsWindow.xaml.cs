using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SlidePlayer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
	// TODO Make slider for setting sound slide duration
	// TODO Add button sound paths
    public partial class SettingsWindow : Window
    {
		public static readonly DependencyProperty SlidingPauseProperty =
			DependencyProperty.Register("SlidingPause", typeof(bool), typeof(SettingsWindow));

		public bool SlidingPause
		{
			get => (bool)GetValue(SlidingPauseProperty);
			set => SetValue(SlidingPauseProperty, value);
		}

		public static readonly DependencyProperty SoundSignalProperty =
			DependencyProperty.Register("SoundSignal", typeof(bool), typeof(SettingsWindow));

		public float SlidingDuration
		{
			get { return (float)GetValue(SlidingDurationProperty); }
			set { SetValue(SlidingDurationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SlidingDuration.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SlidingDurationProperty =
			DependencyProperty.Register("SlidingDuration", typeof(float), typeof(SettingsWindow));

		public bool SoundSignal
		{
			get => (bool)GetValue(SoundSignalProperty);
			set => SetValue(SoundSignalProperty, value);
		}

		public static readonly DependencyProperty PauseSoundPathProperty =
			DependencyProperty.Register("PauseSoundPath", typeof(string), typeof(SettingsWindow));

		public string PauseSoundPath
		{
			get => GetValue(SoundSignalProperty) as string;
			set => SetValue(SoundSignalProperty, value);
		}

		public void OKResult(object sender, RoutedEventArgs e) => DialogResult = true;

		public void CancelResult(object sender, RoutedEventArgs e) => DialogResult = false;

        public SettingsWindow()
        {
            InitializeComponent();
        }
	}
}
