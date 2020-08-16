using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidePlayer
{
    class AudioStreamFactory
    {
		public static WaveStream CreateStream(string streamType)
		{
			switch (Path.GetExtension(streamType))
			{
				case ".wav": return new WaveFileReader(streamType);
				case ".mp3": return new MediaFoundationReader(streamType);
				default: throw new FileFormatException("It must be mp3 or wav file.");
			}
		}
    }
}
