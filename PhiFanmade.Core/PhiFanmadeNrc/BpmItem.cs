using System;
using JetBrains.Annotations;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public class BpmItem
    {
        private float _bpm = 120f;
        public float Bpm
        {
            get => _bpm;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Bpm), "BPM must be greater than 0.");
                }
                _bpm = value;
            }
        }
        [PublicAPI]
        public Beat StartBeat = new Beat(new[] { 0, 0, 1 });

        public BpmItem Clone()
        {
            return new BpmItem()
            {
                Bpm = Bpm,
                StartBeat = new Beat((int[])StartBeat)
            };
        }
    }
}