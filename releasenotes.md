# New in 0.3.0
- Added the possibility to add extra spacing between circles in CirclePageIndicator, the default spacing is now 5dp, adjust it by using the `ExtraSpacing` property or the `extraSpacing` attribute on the view
- Fixed usage of deprecated methods to calculate touches and more
- Fixed underline fade not taking selected color alpha into consideration always jumping to alpha 0xFF before animating
- Fixed usage of `fill_parent` and now using `match_parent` in layouts and README

# New in 0.2.2
- Switched to Xamarin.Android.Support.ViewPager 28.0.0.1 instead of Xamarin.Android.Support.v4 25.4.0.2
- Updated to TargetFramework 28 (9.0)