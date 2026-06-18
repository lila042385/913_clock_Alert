// v1.01 20260617 23:55
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PortableAlarmClock
{
    public class Alarm
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Hour { get; set; }
        public int Minute { get; set; }
        public string Title { get; set; } = string.Empty;
        
        // Days of week for repeating. If empty, it's a one-time alarm.
        // C# DayOfWeek: 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        public List<DayOfWeek> Weekdays { get; set; } = new List<DayOfWeek>();
        
        public bool IsEnabled { get; set; } = true;
        public string SoundName { get; set; } = "Default";
        public int SnoozeMinutes { get; set; } = 10;
        public int SortOrder { get; set; }
        public DateTime? LastTriggered { get; set; }

        [JsonIgnore]
        public DateTime? SnoozeUntil { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Visual helper for weekdays description (e.g. "Mon, Tue, Wed" or "Once")
        // Since we need to follow Japanese style as requested: "月、火、水、木、金" or "1回限り"
        public string WeekdaysString
        {
            get
            {
                if (Weekdays == null || Weekdays.Count == 0)
                {
                    return "1回限り";
                }
                if (Weekdays.Count == 7)
                {
                    return "毎日";
                }
                
                // Sort by Mon..Sun (standard in Windows Alarm)
                // Windows standard starts with Monday, but let's just sort as Mon..Sun
                var sorted = new List<DayOfWeek>(Weekdays);
                sorted.Sort((a, b) =>
                {
                    int valA = a == DayOfWeek.Sunday ? 7 : (int)a;
                    int valB = b == DayOfWeek.Sunday ? 7 : (int)b;
                    return valA.CompareTo(valB);
                });

                var names = new List<string>();
                foreach (var day in sorted)
                {
                    names.Add(GetDayShortName(day));
                }
                return string.Join("、", names);
            }
        }

        public string TimeString => $"{Hour:D2}:{Minute:D2}";

        private static string GetDayShortName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Sunday => "日",
                DayOfWeek.Monday => "月",
                DayOfWeek.Tuesday => "火",
                DayOfWeek.Wednesday => "水",
                DayOfWeek.Thursday => "木",
                DayOfWeek.Friday => "金",
                DayOfWeek.Saturday => "土",
                _ => string.Empty
            };
        }
    }
}
// v1.01 20260617 23:55
