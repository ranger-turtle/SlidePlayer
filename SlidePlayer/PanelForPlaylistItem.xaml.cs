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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlidePlayer
{
	/// <summary>
	/// Interaction logic for PanelForPlaylistItem.xaml
	/// </summary>
	// TODO Try converting it to Data Template
	public partial class PanelForPlaylistItem : ListBoxItem
	{
		public int ProbabilityFactor
		{
			get { return (int)GetValue(ProbabilityProperty); }
			set { SetValue(ProbabilityProperty, value); }
		}

		public static readonly DependencyProperty ProbabilityProperty =
			DependencyProperty.Register("Probability", typeof(int), typeof(PanelForPlaylistItem));

		public string FileName
		{
			get { return (string)GetValue(FilenameProperty); }
			set { SetValue(FilenameProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FilenameProperty =
			DependencyProperty.Register("Filename", typeof(string), typeof(PanelForPlaylistItem));

		public PanelForPlaylistItem(string filename, int probabilityFactor = 1)
		{
			InitializeComponent();
			FileName = filename;
			ProbabilityFactor = probabilityFactor;
		}
	}
}
