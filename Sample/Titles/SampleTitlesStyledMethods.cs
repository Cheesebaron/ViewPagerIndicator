using Android.App;
using Android.Graphics;
using Android.OS;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Titles
{
    [Activity(Label = "Titles/Styled Methods", Theme = "@style/LightTheme")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTitlesStyledMethods : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_titles);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            var density = Resources.DisplayMetrics.Density;
            var indicator = FindViewById<TitlePageIndicator>(Resource.Id.indicator);
            indicator.SetViewPager(_pager);
            indicator.SetBackgroundColor(Color.Argb(0x18, 0xFF, 0x00, 0x00));
            indicator.FooterColor = Color.Argb(0xFF, 0xAA, 0x22, 0x22);
            indicator.FooterLineHeight = 1 * density;
            indicator.FooterIndicatorHeight = 3 * density;
            indicator.FooterIndicatorStyle = TitlePageIndicator.IndicatorStyle.Underline;
            indicator.TextColor = Color.Argb(0xAA, 0xFF, 0xFF, 0xFF);
            indicator.SelectedColor = Color.Argb(0xFF, 0xFF, 0xFF, 0xFF);
            indicator.IsSelectedBold = true;
        }
    }
}
