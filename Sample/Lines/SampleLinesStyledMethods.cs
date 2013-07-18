using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Lines
{
    [Activity(Label = "Lines/Styled Methods")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleLinesStyledMethods : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_lines);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            _indicator = FindViewById<LinePageIndicator>(Resource.Id.indicator);
            _indicator.SetViewPager(_pager);

            var density = Resources.DisplayMetrics.Density;
            var indicator = (LinePageIndicator)_indicator;
            indicator.SelectedColor = Color.Argb(136, 255, 0, 0);
            indicator.UnselectedColor = Color.Argb(255, 136, 136, 136);
            indicator.StrokeWidth = 4 * density;
            indicator.LineWidth = 30 * density;
        }
    }
}