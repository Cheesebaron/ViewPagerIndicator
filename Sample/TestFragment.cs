using System.Text;
using Android.OS;
using Android.Views;
using Android.Widget;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace Sample
{
    public class TestFragment : Fragment
    {
        private const string KeyContent = "TestFragment:Content";
        private string _content = "???";

        public static TestFragment NewInstance(string content)
        {
            var fragment = new TestFragment();

            var builder = new StringBuilder();
            for (var i = 0; i < 20; i++)
                builder.Append(content).Append(" ");
            builder.Remove(builder.Length -1, 1);
            fragment._content = builder.ToString();

            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if ((savedInstanceState != null) && savedInstanceState.ContainsKey(KeyContent))
                _content = savedInstanceState.GetString(KeyContent);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var text = new TextView(Activity)
                {
                    Gravity = GravityFlags.Center,
                    Text = _content,
                    TextSize = 20 * Resources.DisplayMetrics.Density
                };
            text.SetPadding(20, 20, 20, 20);

            var layout = new LinearLayout(Activity)
                {
                    LayoutParameters =
                        new ViewGroup.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent)
                };
            layout.SetGravity(GravityFlags.Center);
            layout.AddView(text);

            return layout;
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutString(KeyContent, _content);
        }
    }
}