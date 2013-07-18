using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Sample.Tabs
{
    [Activity(Label = "Tabs/Default", Theme = "@style/Theme.PageIndicatorDefaults")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTabsDefault : BaseSampleActivity
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

        private class GoogleMusicAdapter : FragmentPagerAdapter
        {
            private static readonly string[] Content = new[]
            {
                "Recent", "Artists", "Album", "Songs", "Playlists", "Genres"
            };

            public GoogleMusicAdapter(FragmentManager p0) 
                : base(p0) 
            { }

            public override int Count
            {
                get { return Content.Length; }
            }

            public override Fragment GetItem(int p0) { return TestFragment.NewInstance(Content[p0 % Content.Length]); }

            public override Java.Lang.ICharSequence GetPageTitleFormatted(int p0) { return new Java.Lang.String(Content[p0 % Content.Length].ToUpper()); }
        }
    }
}