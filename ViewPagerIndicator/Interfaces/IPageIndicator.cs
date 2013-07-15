using Android.Support.V4.View;

namespace Dk.Ostebaronen.Android.ViewPagerIndicator.Interfaces
{
    public interface IPageIndicator : ViewPager.IOnPageChangeListener
    {
        void SetViewPager(ViewPager view);
        void SetViewPager(ViewPager view, int initialPosition);
        void SetCurrentItem(int item);
        void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener);
        void NotifyDataSetChanged();
    }
}