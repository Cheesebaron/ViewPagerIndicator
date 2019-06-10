using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Underlines
{
    [Activity(Label = "Underlines/Styled Methods", Theme = "@style/LightTheme")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleUnderlinesStyledMethods : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_underlines);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            var indicator = FindViewById<UnderlinePageIndicator>(Resource.Id.indicator);
            indicator.SetViewPager(_pager);
            indicator.SelectedColor = Color.Argb(0x33, 0xCC, 0x00, 0x00);
            indicator.SetBackgroundColor(Color.Argb(0xFF, 0xCC, 0xCC, 0xCC));
            indicator.FadeLength = 1000;
            indicator.FadeDelay = 1000;
        }
    }
}