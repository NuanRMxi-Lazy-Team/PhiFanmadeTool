namespace PhiFanmade.Core.PhiEdit
{

        public class Bpm
        {
            public float BeatPerMinute = 120f;
            public float StartBeat = 0f;

            public override string ToString()
            {
                return $"bp {StartBeat} {BeatPerMinute}";
            }
            
            public Bpm Clone()
            {
                return new Bpm
                {
                    BeatPerMinute = BeatPerMinute,
                    StartBeat = StartBeat
                };
            }
        }
        
    
}