using Newtonsoft.Json;
using System.Collections.Generic;
namespace PhiFanmade.Core.PhiFans
{
    public static partial class PhiFans
    {
        public class Line
        {
            /// <summary>
            /// 判定线事件组
            /// </summary>
            [JsonProperty("props")] public Props Props = new Props();
            /// <summary>
            /// 判定线音符列表
            /// </summary>
            [JsonProperty("notes")] public List<Note> NoteList = new List<Note>();
        }
    }
}