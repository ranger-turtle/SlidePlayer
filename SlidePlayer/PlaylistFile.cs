using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SlidePlayer
{
	class PlaylistFile
	{
		public static List<PanelForPlaylistItem> Read(string filename)
		{
			if (Path.GetExtension(filename) != ".spp")
				throw new FileFormatException("Only spp files are supported.");
			XDocument playlist = XDocument.Load(filename);
			return playlist.Root.Elements("position")
				.Select(e => new PanelForPlaylistItem(
					(string)e.Element("path"),
					(int)e.Element("factor"))
				).ToList();
		}

		public static void Write(string filename, List<PanelForPlaylistItem> items)
		{
			var XPlaylistItems = new XElement("playlist",
				items.Select(i => new XElement("position",
				new XElement("path", i.FileName),
				new XElement("factor", i.ProbabilityFactor))));
			XPlaylistItems.Save(filename);
		}
	}
}
