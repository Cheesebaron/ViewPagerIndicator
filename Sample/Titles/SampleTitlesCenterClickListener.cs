using Android.App;
using Android.OS;
using Android.Support.V4.View;
using Android.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Titles
{
    [Activity(Label = "Titles/Center Click Listener")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleTitlesCenterClickListener : BaseSampleActivity
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
            indicator.FooterIndicatorStyle = TitlePageIndicator.IndicatorStyle.Underline;
            indicator.CenterItemClick +=
                (sender, args) => Toast.MakeText(this, "You clicked the center title!", ToastLength.Short).Show();
            _indicator = indicator;
        }
    }
}