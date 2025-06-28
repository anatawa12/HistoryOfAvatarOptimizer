using System;
using UdonSharp;
using UnityEngine;

namespace HistoryOfAvatarOptimizer.ReleaseNoteCard
{
    public class TodayCard : UdonSharpBehaviour
    {
        public string epicDate = "2023-06-27";
        public float dayLength = 0.02f;

        void Start()
        {
            var epocDate = DateTime.Parse(epicDate);
            var today = DateTime.Now;
            var daysSinceEpic = (today - epocDate).TotalDays;
            transform.localPosition = new Vector3((float)daysSinceEpic * dayLength, 0, 0);
        }
    }
}
