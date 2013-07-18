using Android.App;
using Android.OS;
using Android.Support.V4.View;
using Android.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Circles
{
    [Activity(Label = "Circles/With Listener")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleCirclesWithListener : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_circles);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            _indicator = FindViewById<CirclePageIndicator>(Resource.Id.indicator);
            _indicator.SetViewPager(_pager);

            ((CirclePageIndicator)_indicator).PageSelected += (s, e) =>
                                                              Toast.MakeText(this, "Changed to page " + e.Position,
                                                                             ToastLength.Short).Show();
        }
    }
}