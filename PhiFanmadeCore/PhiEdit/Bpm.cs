namespace PhiFanmade.Core.PhiEdit
{
    public static partial class PhiEdit
    {
        public class Bpm
        {
            public float BeatPerMinute = 120f;
            public float StartBeat = 0f;

            public override string ToString()
            {
                return $"bp {StartBeat} {BeatPerMinute}";
            }
        }
        
    }
}