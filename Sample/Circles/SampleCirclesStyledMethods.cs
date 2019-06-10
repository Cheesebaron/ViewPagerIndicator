using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Circles
{
    [Activity(Label = "Circles/Styled Methods", Theme = "@style/LightTheme")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleCirclesStyledMethods : BaseSampleActivity
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

            var density = Resources.DisplayMetrics.Density;
            var indicator = (CirclePageIndicator) _indicator;
            indicator.SetBackgroundColor(Color.Argb(255, 204, 204, 204));
            indicator.Radius = 10 * density;
            indicator.PageColor = Color.Argb(136, 0, 0, 255);
            indicator.FillColor = Color.Argb(255, 136, 136, 136);
            indicator.StrokeColor = Color.Argb(255, 0, 0, 0);
            indicator.StrokeWidth = 2 * density;
            indicator.ExtraSpacing = 30 * density;
        }
    }
}