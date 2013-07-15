using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Sample
{
    public class TestFragmentAdapter : FragmentPagerAdapter
    {
        public TestFragmentAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
        }

        public TestFragmentAdapter(FragmentManager p0) : base(p0) {
        }

        public override int Count
        {
            get { throw new NotImplementedException(); }
        }

        public override Fragment GetItem(int p0) { throw new NotImplementedException(); }

        
    }
}