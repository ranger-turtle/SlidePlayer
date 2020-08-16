using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;

namespace SlidePlayer
{
    public class SlidableSoundStream : IDisposable
    {
		private WaveOutEvent _waveOut;
		private SoundTouchWaveStream _processorStream;
		private readonly WaveChannel32 _inputStream;
		private readonly Action _onPause;
		private WaveRecorder _recorder;

		public double CurrentTime
		{
			get => _processorStream.CurrentTime.TotalMilliseconds;
			set => _processorStream.CurrentTime = TimeSpan.FromMilliseconds(value);
		}

		public float Volume
		{
			get => _waveOut.Volume;
			set => _waveOut.Volume = value;
		}

		public double Duration => _processorStream.TotalTime.TotalMilliseconds;

		public IWaveProvider WaveStream => _processorStream;

		public int SlidingDuration { get; set; }

		public SlidableSoundStream(string filename, Action pause)
		{
			WaveStream wav = AudioStreamFactory.CreateStream(filename);
			// don't pad, otherwise the stream never ends
			_inputStream = new WaveChannel32(wav) { PadWithZeroes = false };
			_processorStream = new SoundTouchWaveStream(_inputStream);

			_waveOut = new WaveOutEvent() { DesiredLatency = 100 };
			_waveOut.Init(_processorStream);
			_onPause = pause;
		}

		public async Task StopPlaybackGradually()
		{
			for (float rate = 0.990f; rate >= 0.010f; rate -= 0.010f)
			{
				_processorStream.Rate = rate;
				await Task.Delay(SlidingDuration / 100);
			}
			_waveOut.Pause();
			_onPause();
		}

		public void RestoreRate() => _processorStream.Rate = 1.0f;

		public void Play()
		{
			RestoreRate();
			_waveOut.Play();
		}

		public void Pause() => _waveOut.Pause();

		public void StopNormally()
		{
			_waveOut.Stop();
			CurrentTime = 0;
		}

		public void Rec()
		{
			_recorder = new WaveRecorder(_processorStream, "sound.tmp");
			_waveOut.Stop();
			_waveOut.Dispose();
			_waveOut.Init(_recorder);
			_waveOut.Play();
		}

		public void StopRec()
		{
			_waveOut.Stop();
			_waveOut.Dispose();
			_recorder.Dispose();
			_recorder = null;
			_waveOut.Init(_processorStream);
			_waveOut.Play();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_waveOut.Dispose();
					_processorStream.Dispose();
					_recorder?.Dispose();
				}

				// : free unmanaged resources (unmanaged objects) and override a finalizer below.
				// : set large fields to null.

				disposedValue = true;
			}
		}

		// : override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~SlidableSoundStream() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// : uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

	}
}
