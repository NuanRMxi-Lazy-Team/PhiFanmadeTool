using System;
using System.Collections.Generic;
using System.Linq;

namespace PhiFanmade.Core.PhiEdit
{
    public static partial class PhiEdit
    {
        public class JudgeLine
        {
            public List<Frame> SpeedFrames = new List<Frame>();
            public List<MoveFrame> MoveFrames = new List<MoveFrame>();
            public List<Frame> RotateFrames = new List<Frame>();
            public List<Frame> AlphaFrames = new List<Frame>();

            public List<Event> AlphaEvents = new List<Event>();
            public List<MoveEvent> MoveEvents = new List<MoveEvent>();
            public List<Event> RotateEvents = new List<Event>();

            public List<Note> NoteList = new List<Note>();

            public (float, float) GetMoveAtBeat(float beat)
            {
                MoveFrame curFrame = null;
                // 遍历Frame，寻找这个beat上一个使用的Frame
                for (int j = MoveFrames.Count - 1; j >= 0; j--)
                {
                    var frame = MoveFrames[j];
                    // 如果frame的Beat等于当前beat，直接返回（考虑float误差）
                    if (Math.Abs(frame.Beat - beat) < 0.0001f)
                        return (frame.XValue, frame.YValue);
                    // 如果当前beat大于frame的Beat，那么这个就是上一个使用的Frame
                    if (frame.Beat < beat)
                    {
                        curFrame = frame;
                        break;
                    }
                }

                // 与RPE不同的是，需要同时遍历Frame和Event
                for (int i = 0; i < MoveEvents.Count; i++)
                {
                    var e = MoveEvents[i];
                    if (beat >= e.StartBeat && beat <= e.EndBeat)
                    {
                        // 先别急！比对一下curFrame的Beat和上一个 当前拍大于Event的EndBeat的Event 谁大，谁大就用谁的值（Frame用Value、Event用EndValue）
                        var lastEvent = MoveEvents.LastOrDefault(ev => beat > ev.EndBeat);

                        if (lastEvent != null && (curFrame == null || lastEvent.EndBeat > curFrame.Beat))
                        {
                            // 上一个Event的EndBeat更大，说明上一个Event更接近当前拍，使用它的EndValue
                            return e.GetValueAtBeat(beat, lastEvent.EndXValue, lastEvent.EndYValue);
                        }
                        else if (curFrame != null)
                        {
                            // 上一个Frame的Beat更大，说明上一个Frame更接近当前拍，使用它的Value
                            return e.GetValueAtBeat(beat, curFrame.XValue, curFrame.YValue);
                        }
                        else
                        {
                            // 两者都为空，使用默认值0
                            return e.GetValueAtBeat(beat, 0, 0);
                        }
                    }

                    if (beat < e.StartBeat)
                    {
                        break;
                    }
                }

                var previousEvent = MoveEvents.LastOrDefault(ev => beat > ev.EndBeat);
                if (previousEvent != null && (curFrame == null || previousEvent.EndBeat > curFrame.Beat))
                {
                    // 上一个Event的EndBeat更大，说明上一个Event更接近当前拍，使用它的EndValue
                    return (previousEvent.EndXValue, previousEvent.EndYValue);
                }
                else if (curFrame != null)
                {
                    // 上一个Frame的Beat更大，说明上一个Frame更接近当前拍，使用它的Value
                    return (curFrame.XValue, curFrame.YValue);
                }
                else
                {
                    // 两者都为空，使用默认值0
                    return (0, 0);
                }
            }
        }
    }
}