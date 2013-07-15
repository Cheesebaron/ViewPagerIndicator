using System;

namespace Dk.Ostebaronen.Android.ViewPagerIndicator
{
    public class CenterItemClickEventArgs : EventArgs
    {
        public int CurrentPage { get; set; }
    }

    public delegate void CenterItemClickEventHandler(object sender, CenterItemClickEventArgs args);
}