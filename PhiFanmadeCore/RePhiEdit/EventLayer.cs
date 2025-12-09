using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public class EventLayer
    {
        [JsonProperty("moveXEvents")] public Event[] MoveXEvents = {}; // 移动事件
        [JsonProperty("moveYEvents")] public Event[] MoveYEvents = {}; // 移动事件
        [JsonProperty("rotateEvents")] public Event[] RotateEvents = {}; // 旋转事件
        [JsonProperty("alphaEvents")] public Event[] AlphaEvents = {}; // 透明度事件
        [JsonProperty("speedEvents")] public Event[] SpeedEvents = {}; // 速度事件

        /// <summary>
        /// 获取某个拍时，指定事件层级指定事件列表的数值
        /// </summary>
        /// <param name="events">事件数组</param>
        /// <param name="beat">指定拍</param>
        /// <returns>在指定拍时，指定事件列表的数值</returns>
        public float GetValueAtBeat(Event[] events, Beat beat)
        {
            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (beat >= e.StartBeat && beat <= e.EndBeat)
                {
                    return e.GetValueAtBeat(beat);
                }
    
                if (beat < e.StartBeat)
                {
                    break;
                }
            }
    
            var previousEvent = Array.FindLast(events, e => beat > e.EndBeat);
            return previousEvent?.End ?? 0;
        }
    }
}