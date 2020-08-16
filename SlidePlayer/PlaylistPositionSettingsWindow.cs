using System.Windows;

namespace SlidePlayer
{
	/// <summary>
	/// Interaction logic for PlaylistPositionSettings.xaml
	/// </summary>
	public partial class PlaylistPositionSettingsWindow : Window
	{
		public int ProbabilityFactor
		{
			get { return (int)GetValue(ProbabilityFactorProperty); }
			set { SetValue(ProbabilityFactorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ProbabilityFactor.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ProbabilityFactorProperty =
			DependencyProperty.Register("ProbabilityFactor", typeof(int), typeof(PlaylistPositionSettingsWindow));

		public PlaylistPositionSettingsWindow()
		{
			InitializeComponent();
		}

		public void OKResult(object sender, RoutedEventArgs e) => DialogResult = true;

		public void CancelResult(object sender, RoutedEventArgs e) => DialogResult = false;

	}
}
