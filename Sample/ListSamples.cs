using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace Sample
{
    [Activity(Label = "ViewPagerIndicator Sample", MainLauncher = true)]
    public class ListSamples : ListActivity
    {
        public const string SampleCategory = "dk.ostebaronen.viewpagerindicator.droid.sample";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var prefix = Intent.GetStringExtra("dk.ostebaronen.viewpagerindicator.droid.Path") ?? string.Empty;

            var activities = GetDemoActivities(prefix);

            var items = GetMenuItems(activities, prefix);

            ListAdapter = new ArrayAdapter<ActivityListItem>(this, Android.Resource.Layout.SimpleListItem1, Android.Resource.Id.Text1, items);

            ListView.ItemClick += (s, e) =>
            {
                var item = (ActivityListItem)(s as ListView).GetItemAtPosition(e.Position);
                LaunchActivityItem(item);
            };
        }

        private List<ActivityListItem> GetDemoActivities(string prefix)
        {
            var results = new List<ActivityListItem>();

            // Create an intent to query the package manager with,
            // we are looking for ActionMain with our custom category
            var query = new Intent(Intent.ActionMain, null);
            query.AddCategory(SampleCategory);

            var list = PackageManager.QueryIntentActivities(query, 0);

            // If there were no results, bail
            if (list == null)
                return results;

            results.AddRange(from resolve in list
                             let category = resolve.LoadLabel(PackageManager)
                             let type =
                                 string.Format("{0}:{1}", resolve.ActivityInfo.ApplicationInfo.PackageName,
                                               resolve.ActivityInfo.Name)
                             where
                                 string.IsNullOrWhiteSpace(prefix) ||
                                 category.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                             select new ActivityListItem(prefix, category, type));

            return results;
        }

        private List<ActivityListItem> GetMenuItems(List<ActivityListItem> activities, string prefix)
        {
            // Get menu items at this level
            var items = activities.Where(a => a.IsMenuItem);

            // Get Submenus at this level, but we only need 1 of each
            var submenus = activities.Where(a => a.IsSubMenu).Distinct(new ActivityListItem.NameComparer());

            // Combine, sort, return
            return items.Union(submenus).OrderBy(a => a.Name).ToList();
        }

        private void LaunchActivityItem(ActivityListItem item)
        {
            if (item.IsSubMenu)
            {
                // Launch this menu activity again with an updated prefix
                var result = new Intent();

                result.SetClass(this, typeof(ListSamples));
                result.PutExtra("dk.ostebaronen.viewpagerindicator.droid.Path", string.Format("{0}/{1}", item.Prefix, item.Name).Trim('/'));

                StartActivity(result);
            }
            else
            {
                // Launch the item activity
                var result = new Intent();
                result.SetClassName(item.Package, item.Component);

                StartActivity(result);
            }
        }
    }
}