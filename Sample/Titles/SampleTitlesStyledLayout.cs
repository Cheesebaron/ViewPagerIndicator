using Android.App;
using Android.OS;
using Android.Support.V4.View;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Titles
{
    [Activity(Label = "Titles/Styled Layout", Theme = "@style/LightTheme")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTitlesStyledLayout : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.themed_titles);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            _indicator = FindViewById<TitlePageIndicator>(Resource.Id.indicator);
            _indicator.SetViewPager(_pager);
        }
    }
}