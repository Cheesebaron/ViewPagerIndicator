using System;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    public class PageScrollStateChangedEventArgs
        : EventArgs
    {
        public int State { get; set; }
    }

    public class PageScrolledEventArgs
        : EventArgs
    {
        public int Position { get; set; }
        public float PositionOffset { get; set; }
        public int PositionOffsetPixels { get; set; }
    }

    public class PageSelectedEventArgs
        : EventArgs
    {
        public int Position { get; set; }
    }

    public class TabReselectedEventArgs
        : EventArgs
    {
        public int Position { get; set; }
    }

    public delegate void PageScrollStateChangedEventHandler(object sender, PageScrollStateChangedEventArgs args);

    public delegate void PageScrolledEventHandler(object sender, PageScrolledEventArgs args);

    public delegate void PageSelectedEventHandler(object sender, PageSelectedEventArgs args);

    public delegate void TabReselectedEventHandler(object sender, TabReselectedEventArgs args);
}
