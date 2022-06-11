using AndroidX.Fragment.App;
using DK.Ostebaronen.Droid.ViewPagerIndicator.Interfaces;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace Sample
{
    public class TestFragmentAdapter : FragmentPagerAdapter, IIconPageAdapter
    {
        private static readonly string[] Content = {"This", "Is", "A", "Test"};
        private static readonly int[] Icons =
        {
            Resource.Drawable.perm_group_calendar,
            Resource.Drawable.perm_group_camera,
            Resource.Drawable.perm_group_device_alarms,
            Resource.Drawable.perm_group_location
        };

        private int _count = Content.Length;

        public TestFragmentAdapter(FragmentManager p0) 
            : base(p0) 
        {
        }

        public override int Count
        {
            get { return _count; }
        }

        public override Fragment GetItem(int position)
        {
            return TestFragment.NewInstance(Content[position % Content.Length]);
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int p0)
        {
            return new Java.Lang.String(Content[p0 % Content.Length]);
        }

        public void SetCount(int count)
        {
            if (count <= 0 || count > 10) return;

            _count = count;
            NotifyDataSetChanged();
        }

        public int GetIconResId(int index)
        {
            return Icons[index % Icons.Length];
        }
    }
}