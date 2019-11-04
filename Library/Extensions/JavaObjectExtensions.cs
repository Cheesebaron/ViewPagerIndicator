using System;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator.Extensions
{
    internal static class JavaObjectExtensions
    {
        internal static bool IsNull(this Java.Lang.Object @object)
        {
            if (@object == null)
                return true;

            if (@object.Handle == IntPtr.Zero)
                return true;

            return false;
        }
    }
}