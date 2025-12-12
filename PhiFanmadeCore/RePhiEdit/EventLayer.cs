using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class EventLayer
        {
            [JsonProperty("moveXEvents")] public List<Event<float>> MoveXEvents = new List<Event<float>>(); // 移动事件
            [JsonProperty("moveYEvents")] public List<Event<float>> MoveYEvents = new List<Event<float>>(); // 移动事件
            [JsonProperty("rotateEvents")] public List<Event<float>> RotateEvents = new List<Event<float>>(); // 旋转事件
            [JsonProperty("alphaEvents")] public List<Event<float>> AlphaEvents = new List<Event<float>>(); // 透明度事件
            [JsonProperty("speedEvents")] public List<Event<float>> SpeedEvents = new List<Event<float>>(); // 速度事件

            /// <summary>
            /// 获取某个拍时，指定事件层级指定事件列表的数值
            /// </summary>
            /// <param name="events">事件数组</param>
            /// <param name="beat">指定拍</param>
            /// <returns>在指定拍时，指定事件列表的数值</returns>
            public T GetValueAtBeat<T>(List<Event<T>> events, Beat beat)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    var e = events[i];
                    // 如果当前拍在事件范围内，返回插值结果
                    if (beat >= e.StartBeat && beat <= e.EndBeat)
                        return e.GetValueAtBeat(beat);
                    // 如果当前拍小于事件的开始拍，说明后续事件都不符合条件，跳出循环
                    if (beat < e.StartBeat)
                        break;
                }

                var previousEvent = events.FindLast(e => beat > e.EndBeat);
                return previousEvent != null ? previousEvent.EndValue : default;
            }

            /// <summary>
            /// 按事件的开始时间排序所有事件
            /// </summary>
            public void Sort()
            {
                MoveXEvents.Sort((a, b) =>
                {
                    if (a.StartBeat < b.StartBeat) return -1;
                    if (a.StartBeat > b.StartBeat) return 1;
                    return 0;
                });
                MoveYEvents.Sort((a, b) =>
                {
                    if (a.StartBeat < b.StartBeat) return -1;
                    if (a.StartBeat > b.StartBeat) return 1;
                    return 0;
                });
                RotateEvents.Sort((a, b) =>
                {
                    if (a.StartBeat < b.StartBeat) return -1;
                    if (a.StartBeat > b.StartBeat) return 1;
                    return 0;
                });
                AlphaEvents.Sort((a, b) =>
                {
                    if (a.StartBeat < b.StartBeat) return -1;
                    if (a.StartBeat > b.StartBeat) return 1;
                    return 0;
                });
                SpeedEvents.Sort((a, b) =>
                {
                    if (a.StartBeat < b.StartBeat) return -1;
                    if (a.StartBeat > b.StartBeat) return 1;
                    return 0;
                });
            }
        }
    }
}