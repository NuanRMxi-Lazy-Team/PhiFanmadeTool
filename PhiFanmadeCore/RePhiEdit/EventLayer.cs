using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class EventLayer
        {
            [JsonProperty("moveXEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> MoveXEvents; // 移动事件

            [JsonProperty("moveYEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> MoveYEvents; // 移动事件

            [JsonProperty("rotateEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> RotateEvents; // 旋转事件

            [JsonProperty("alphaEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<int>> AlphaEvents; // 透明度事件

            [JsonProperty("speedEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> SpeedEvents; // 速度事件

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
                var eventLists = new List<List<Event<float>>>
                {
                    MoveXEvents, MoveYEvents, RotateEvents, SpeedEvents
                };
                var alphaEventList = AlphaEvents;
                eventLists.ForEach(events =>
                {
                    events?.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
                });
                alphaEventList?.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }

            /// <summary>
            /// 克隆，使用深拷贝
            /// </summary>
            /// <returns>一个从里到外都新新的事件层级！</returns>
            public EventLayer Clone()
            {
                // 深拷贝，包括Event列表
                var clone = new EventLayer();
                // 保证列表中的元素也被深拷贝（通过LINQ调用Event的Clone方法）
                if (MoveXEvents != null)
                    clone.MoveXEvents = MoveXEvents.ConvertAll(e => e.Clone());
                if (MoveYEvents != null)
                    clone.MoveYEvents = MoveYEvents.ConvertAll(e => e.Clone());
                if (RotateEvents != null)
                    clone.RotateEvents = RotateEvents.ConvertAll(e => e.Clone());
                if (AlphaEvents != null)
                    clone.AlphaEvents = AlphaEvents.ConvertAll(e => e.Clone());
                if (SpeedEvents != null)
                    clone.SpeedEvents = SpeedEvents.ConvertAll(e => e.Clone());
                return clone;
            }

            /// <summary>
            /// 强行预期化，将空列表设置为null，保证Json序列化时不包含空列表
            /// </summary>
            public void Anticipation()
            {
                if (MoveXEvents != null && MoveXEvents.Count == 0)
                    MoveXEvents = null;
                if (MoveYEvents != null && MoveYEvents.Count == 0)
                    MoveYEvents = null;
                if (RotateEvents != null && RotateEvents.Count == 0)
                    RotateEvents = null;
                if (AlphaEvents != null && AlphaEvents.Count == 0)
                    AlphaEvents = null;
                if (SpeedEvents != null && SpeedEvents.Count == 0)
                    SpeedEvents = null;
            }
        }
    }
}