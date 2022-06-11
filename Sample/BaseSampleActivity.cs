using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator;

namespace Sample
{
    public abstract class BaseSampleActivity : AppCompatActivity
    {
        private static readonly Random _random = new Random();

        protected TestFragmentAdapter _adapter;
        protected ViewPager _pager;
        protected IPageIndicator _indicator;

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.random:
                    var page = _random.Next(_adapter.Count);
                    Toast.MakeText(this, "Changing to page " + page, ToastLength.Short).Show();
                    _pager.CurrentItem = page;
                    return true;

                case Resource.Id.add_page:
                    if (_adapter.Count < 10)
                    {
                        _adapter.SetCount(_adapter.Count + 1);
                        _indicator.NotifyDataSetChanged();
                    }
                    return true;

                case Resource.Id.remove_page:
                    if (_adapter.Count > 1)
                    {
                        _adapter.SetCount(_adapter.Count - 1);
                        _indicator.NotifyDataSetChanged();
                    }
                    return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }
}