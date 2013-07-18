using Android.App;
using Android.OS;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Titles
{
    [Activity(Label = "Titles/Triangle")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTitlesTriangle : BaseSampleActivity
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
            indicator.FooterIndicatorStyle = TitlePageIndicator.IndicatorStyle.Triangle;
        }
    }
}