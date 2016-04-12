using Android.App;
using Android.OS;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;
using DK.Ostebaronen.Droid.ViewPagerIndicator.Interfaces;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Android.Views;

namespace Sample.Tabs
{
    [Activity(Label = "Tabs/With Icons", Theme = "@style/StyledIndicators")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTabsWithIcons : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_tabs);

            var adapter = new GoogleMusicAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = adapter;

            _indicator = FindViewById<TabPageIndicator>(Resource.Id.indicator);
            _indicator.SetViewPager(_pager);
        }

        private class GoogleMusicAdapter : TestFragmentAdapter, IIconPageAdapter
        // TODO uncomment to see IIconGravityProvider effect (the icon renders on top of the tab title)
        //, IIconGravityProvider
        {
            private static readonly string[] Content =
            {
                "Calendar", "Camera", "Alarms", "Location"
            };

            private static readonly int[] Icons =
            {
                Resource.Drawable.perm_group_calendar,
                Resource.Drawable.perm_group_camera,
                Resource.Drawable.perm_group_device_alarms,
                Resource.Drawable.perm_group_location
            };

            public GoogleMusicAdapter(FragmentManager p0) 
                : base(p0) 
            { }

            public new int GetIconResId(int index) { return Icons[index]; }

            public override int Count
            {
                get { return Content.Length; }
            }

            public override Fragment GetItem(int p0) { return TestFragment.NewInstance(Content[p0 % Content.Length]); }

            public override Java.Lang.ICharSequence GetPageTitleFormatted(int p0) { return new Java.Lang.String(Content[p0 % Content.Length].ToUpper()); }

            public GravityFlags IconGravity
            {
                get { return GravityFlags.Top; }
            }
        }
    }
}