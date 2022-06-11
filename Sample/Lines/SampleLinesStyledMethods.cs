using Android.App;
using Android.Graphics;
using Android.OS;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Lines
{
    [Activity(Label = "Lines/Styled Methods", Theme = "@style/LightTheme")]
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

            var indicator = FindViewById<LinePageIndicator>(Resource.Id.indicator);
            indicator.SetViewPager(_pager);

            var density = Resources.DisplayMetrics.Density;
            indicator.SelectedColor = Color.Argb(136, 255, 0, 0);
            indicator.UnselectedColor = Color.Argb(255, 136, 136, 136);
            indicator.StrokeWidth = 4 * density;
            indicator.LineWidth = 30 * density;
        }
    }
}
