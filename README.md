ViewPagerIndicator
==========================

[![Build Status](https://osteost.visualstudio.com/ViewPagerIndicator/_apis/build/status/Cheesebaron.ViewPagerIndicator?branchName=master)](https://osteost.visualstudio.com/ViewPagerIndicator/_build/latest?definitionId=7&branchName=master)

Paging indicator widgets that are compatible with the `ViewPager` from the
[Android Support Library][2] to improve discoverability of content.

> For Material Design look at [PagerSlidingTabStrip](https://github.com/jamesmontemagno/PagerSlidingTabStrip-for-Xamarin.Android)

Usage
=====

*For a working implementation of this project see the `sample` solution.*

  1. Include one of the widgets in your view. This should usually be placed
     adjacent to the `ViewPager` it represents.

        <dk.ostebaronen.droid.viewpagerindicator.TitlePageIndicator
            android:id="@+id/titles"
            android:layout_height="wrap_content"
            android:layout_width="match_parent" />

  2. In your `OnCreate` method (or `OnCreateView` for a fragment), bind the
     indicator to the `ViewPager`.

         //Set the pager with an adapter
         var pager = FindViewById<ViewPager>(Resource.Id.pager);
         pager.Adapter = new TestAdapter(SupportFragmentManager);

         //Bind the title indicator to the adapter
         var titleIndicator = FindViewById<TitlePageIndicator>(Resource.Id.titles);
         titleIndicator.SetViewPager(pager);

  3. *(Optional)* If you want to listen for a `PageChange` event, you should use it
	 on the `ViewPagerIndicator`, rather than setting an `OnPageChangeListener` on the
	 `ViewPager`, otherwise the `ViewPagerIndicator` will not update.

         //continued from above
         titleIndicator.PageChange += MyPageChangeEventHandler;


Theming
-------

There are three ways to style the look of the indicators.

 1. **Theme XML**. An attribute for each type of indicator is provided in which
    you can specify a custom style.
 2. **Layout XML**. Through the use of a custom namespace you can include any
    desired styles.
 3. **Object methods**. Both styles have Properties for each style
    attribute which can be changed at any point.

Each indicator has a demo which creates the same look using each of these
methods.


Including In Your Project
-------------------------

ViewPagerIndicator is an Android Library project, which you can reference in
your Android Project.

This project depends on the `ViewPager` class which is available in the
[Android Support Library][2].

Ported to Xamarin.Android By
============

 * Tomasz Cielecki


Originally Developed By
============

 * Jake Wharton - <jakewharton@gmail.com>


License
=======

	Copyright 2013 Tomasz Cielecki
    Copyright 2012 Jake Wharton
    Copyright 2011 Patrik Åkerfeldt
    Copyright 2011 Francisco Figueiredo Jr.

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.


 [1]: https://github.com/pakerfeldt
 [2]: http://developer.android.com/sdk/compatibility-library.html
 [3]: http://actionbarsherlock.com
 [4]: https://github.com/pakerfeldt/android-viewflow
 [5]: https://github.com/franciscojunior
 [6]: https://gist.github.com/1122947
 [7]: http://developer.android.com/guide/developing/projects/projects-eclipse.html
 [8]: http://developer.android.com/guide/developing/projects/projects-eclipse.html#ReferencingLibraryProject
 [9]: https://raw.github.com/JakeWharton/Android-ViewPagerIndicator/master/sample/screens.png
 [10]: https://play.google.com/store/apps/details?id=com.viewpagerindicator.sample
