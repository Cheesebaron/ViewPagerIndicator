using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Titles
{
    [Activity(Label = "Titles/With Listener")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTitlesWithListener : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_titles);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            var indicator = FindViewById<TitlePageIndicator>(Resource.Id.indicator);
            indicator.SetViewPager(_pager);
            indicator.PageSelected +=
                (sender, args) => Toast.MakeText(this, "Changed to page " + args.Position, ToastLength.Short).Show();
        }
    }
}
