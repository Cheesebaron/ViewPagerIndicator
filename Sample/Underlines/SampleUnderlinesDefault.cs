using Android.App;
using Android.OS;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample.Underlines
{
    [Activity(Label = "Underlines/Default")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "dk.ostebaronen.viewpagerindicator.droid.sample" })]
    public class SampleUnderlinesDefault : BaseSampleActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.simple_underlines);

            _adapter = new TestFragmentAdapter(SupportFragmentManager);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.Adapter = _adapter;

            _indicator = FindViewById<UnderlinePageIndicator>(Resource.Id.indicator);
            _indicator.SetViewPager(_pager);
        }
    }
}