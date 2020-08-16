using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SlidePlayer
{
	class PlaylistRandomizer
	{
		private List<float> _checkpoints = new List<float>();
		private int _factorSum;
		private ItemCollection items;

		public PlaylistRandomizer(ItemCollection items)
		{
			this.items = items;
		}

		public void EvaluateChances(Func<int, int, int> operation, int factor)
		{
			_factorSum = operation(_factorSum, factor);
			float sum = 0;
			_checkpoints.Add(0);
			for (int i = 0; i < items.Count; i++)
			{
				_checkpoints[i] = sum;
				sum += (float)(items[i] as PanelForPlaylistItem).ProbabilityFactor / _factorSum;
			}
		}

		public void ChangeChances(int oldFactor, int newFactor)
		{
			_factorSum += newFactor - oldFactor;
			float sum = 0;
			for (int i = 0; i < items.Count; i++)
			{
				_checkpoints[i] = sum;
				sum += (float)(items[i] as PanelForPlaylistItem).ProbabilityFactor / _factorSum;
			}
		}

		public int Randomize()
		{
			Random random = new Random();
			double chance = random.NextDouble();
			return _checkpoints.IndexOf(_checkpoints.Last(c => c < chance));
		}
	}
}
