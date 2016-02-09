using System;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator.Interfaces;
using Java.Lang;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    [Register("dk.ostebaronen.droid.viewpagerindicator.TabPageIndicator")]
    public class TabPageIndicator 
        : HorizontalScrollView
        , IPageIndicator
    {
        public static readonly ICharSequence EmptyTitle = new Java.Lang.String("");

        private Runnable _tabSelector;
        private readonly IcsLinearLayout _tabLayout;

        private ViewPager _viewPager;
        private ViewPager.IOnPageChangeListener _listener;

        private int _maxTabWidth;
        private int _selectedTabIndex;

        public event TabReselectedEventHandler TabReselected;
        public event PageScrollStateChangedEventHandler PageScrollStateChanged;
        public event PageSelectedEventHandler PageSelected;
        public event PageScrolledEventHandler PageScrolled;

        public TabPageIndicator(Context context)
            : this(context, null)
        {}

        public TabPageIndicator(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            HorizontalScrollBarEnabled = false;

            _tabLayout = new IcsLinearLayout(context, Resource.Attribute.vpiTabPageIndicatorStyle);
            AddView(_tabLayout, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.MatchParent));
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
            var lockedExpanded = widthMode == MeasureSpecMode.Exactly;
            FillViewport = lockedExpanded;

            var childCount = _tabLayout.ChildCount;
            if (childCount > 1 && (widthMode == MeasureSpecMode.Exactly || widthMode == MeasureSpecMode.AtMost))
            {
                if (childCount > 2)
                    _maxTabWidth = (int)(MeasureSpec.GetSize(widthMeasureSpec) * 0.4f);
                else
                    _maxTabWidth = MeasureSpec.GetSize(widthMeasureSpec) / 2;
            }
            else
                _maxTabWidth = -1;

            var oldWidth = MeasuredWidth;
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            var newWidth = MeasuredWidth;

            if (lockedExpanded && oldWidth != newWidth)
                CurrentItem = _selectedTabIndex;
        }

        private void AnimateToTab(int position)
        {
            var iconView = _tabLayout.GetChildAt(position);
            if (_tabSelector != null)
                RemoveCallbacks(_tabSelector);

            _tabSelector = new Runnable(() =>
            {
                var scrollPos = iconView.Left - (Width - iconView.Width) / 2;
                SmoothScrollTo(scrollPos, 0);
                _tabSelector = null;
            });
            Post(_tabSelector);
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            if (_tabSelector != null)
                Post(_tabSelector);
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            if (_tabSelector != null)
                RemoveCallbacks(_tabSelector);
        }

        private void AddTab(int index, ICharSequence text, int iconResId)
        {
            var tabView = new TabView(Context, this) {Focusable = true, Index = index, TextFormatted = text};
            tabView.Click += (sender, args) =>
            {
                var tabview = (TabView)sender;
                var oldSelected = _viewPager.CurrentItem;
                var newSelected = tabview.Index;

                _viewPager.CurrentItem = newSelected;
                if(oldSelected == newSelected && TabReselected != null)
                    TabReselected(this, new TabReselectedEventArgs { Position = newSelected });
            };

            if (iconResId != 0)
                tabView.SetCompoundDrawablesWithIntrinsicBounds(iconResId, 0, 0, 0);

            _tabLayout.AddView(tabView, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 1));
        }

        public void OnPageScrollStateChanged(int state)
        {
            if (_listener != null)
                _listener.OnPageScrollStateChanged(state);

            if (null != PageScrollStateChanged)
                PageScrollStateChanged(this, new PageScrollStateChangedEventArgs { State = state });
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            if (_listener != null)
                _listener.OnPageScrolled(position, positionOffset, positionOffsetPixels);

            if (null != PageScrolled)
                PageScrolled(this,
                             new PageScrolledEventArgs
                             {
                                 Position = position,
                                 PositionOffset = positionOffset,
                                 PositionOffsetPixels = positionOffsetPixels
                             });
        }

        public void OnPageSelected(int position)
        {
            CurrentItem = position;
            if (_listener != null)
                _listener.OnPageSelected(position);

            if (null != PageSelected)
                PageSelected(this, new PageSelectedEventArgs { Position = position });
        }

        public void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener) { _listener = listener; }

        public void SetViewPager(ViewPager view)
        {
            if (_viewPager == view) return;

            if (null != _viewPager)
				_viewPager.ClearOnPageChangeListeners();

            if (null == view.Adapter)
                throw new InvalidOperationException("ViewPager does not have an Adapter instance.");

            _viewPager = view;
			_viewPager.AddOnPageChangeListener(this);
            NotifyDataSetChanged();
        }

        public void SetViewPager(ViewPager view, int initialPosition)
        {
            SetViewPager(view);
            CurrentItem = initialPosition;
        }

        public void NotifyDataSetChanged()
        {
            _tabLayout.RemoveAllViews();
            var adapter = _viewPager.Adapter;
            IIconPageAdapter iconAdapter = null;
            if (adapter is IIconPageAdapter)
                iconAdapter = (IIconPageAdapter)adapter;

            var count = adapter.Count;
            for(var i = 0; i < count; i++)
            {
                var title = adapter.GetPageTitleFormatted(i) ?? EmptyTitle;

                var iconResId = 0;
                if (iconAdapter != null)
                    iconResId = iconAdapter.GetIconResId(i);
                AddTab(i, title, iconResId);
            }
            if (_selectedTabIndex > count)
                _selectedTabIndex = count - 1;
            CurrentItem = _selectedTabIndex;
            RequestLayout();
        }

        public int CurrentItem
        {
            get { return _selectedTabIndex; }
            set
            {
                if (null == _viewPager)
                    throw new InvalidOperationException("ViewPager has not been bound.");

                _viewPager.CurrentItem = value;
                _selectedTabIndex = value;

                var tabCount = _tabLayout.ChildCount;
                for (var i = 0; i < tabCount; i++)
                {
                    var child = _tabLayout.GetChildAt(i);
                    var selected = (i == value);
                    child.Selected = selected;
                    if (selected)
                        AnimateToTab(value);
                }
            }
        }

        private class TabView : TextView
        {
            private readonly TabPageIndicator _indicator;

            public TabView(Context context, TabPageIndicator indicator)
                : base(context, null, Resource.Attribute.vpiTabPageIndicatorStyle) { _indicator = indicator; }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

                //Re-measure if we went beyond our maximum size.
                if (_indicator._maxTabWidth > 0 && MeasuredWidth > _indicator._maxTabWidth)
                    base.OnMeasure(MeasureSpec.MakeMeasureSpec(_indicator._maxTabWidth, MeasureSpecMode.Exactly),
                                   heightMeasureSpec);
            }

            public int Index { get; set; }
        }
    }
}