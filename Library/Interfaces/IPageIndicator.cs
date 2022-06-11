using AndroidX.ViewPager.Widget;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    public interface IPageIndicator : ViewPager.IOnPageChangeListener
    {
        void SetViewPager(ViewPager view);
        void SetViewPager(ViewPager view, int initialPosition);
        int CurrentItem { get; set; }
        void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener);
        void NotifyDataSetChanged();
    }
}
