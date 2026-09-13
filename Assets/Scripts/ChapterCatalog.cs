using UnityEngine;

namespace RiskyDelivery
{
    public sealed class ChapterDefinition
    {
        public string Title { get; }
        public string Instructions { get; }
        public ChapterDefinition(string title, string instructions) { Title = title; Instructions = instructions; }
    }

    public static class ChapterCatalog
    {
        private static readonly ChapterDefinition[] chapters = {
            new ChapterDefinition("TRAINING", "Deliver the parcel to the mint zone.\nStop inside the zone to finish."),
            new ChapterDefinition("ROADWORKS", "Follow the mint gaps: RIGHT - LEFT - RIGHT.\nBrake before turns. Protect the parcel!"),
            new ChapterDefinition("RAIN RUN", "Blue road = low grip. Brake BEFORE puddles.\nPass the barriers on the RIGHT, then LEFT."),
            new ChapterDefinition("HILL DELIVERY", "Hold SPACE while moving to secure your load.\nClimb the hill. Avoid sudden speed changes."),
            new ChapterDefinition("NIGHT SHIFT", "Stop BEFORE the white lines. Watch the signals.\nCross on a fresh green. Avoid moving traffic.")
        };
        public static int Count => chapters.Length;
        public static ChapterDefinition Get(int chapter) => chapters[Mathf.Clamp(chapter, 1, Count) - 1];
    }
}
