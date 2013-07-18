using System;
using Android.Content;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator.Interfaces;
using Java.Lang;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    public class IconPageIndicator : HorizontalScrollView, IPageIndicator
    {
        private readonly IcsLinearLayout _iconsLayout;

        private ViewPager _viewPager;
        private ViewPager.IOnPageChangeListener _listener;
        private Runnable _iconSelector;
        private int _selectedIndex;

        public event PageScrollStateChangedEventHandler PageScrollStateChanged;
        public event PageSelectedEventHandler PageSelected;
        public event PageScrolledEventHandler PageScrolled;

        public IconPageIndicator(Context context)
            : this(context, null)
        {
        }

        public IconPageIndicator(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            HorizontalScrollBarEnabled = false;

            _iconsLayout = new IcsLinearLayout(context, Resource.Attribute.vpiIconPageIndicatorStyle);
            AddView(_iconsLayout, new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.FillParent, GravityFlags.Center));
        }

        private void AnimateToIcon(int position)
        {
            var iconView = _iconsLayout.GetChildAt(position);
            if (_iconSelector != null)
                RemoveCallbacks(_iconSelector);

            _iconSelector = new Runnable(() =>
            {
                var scrollPos = iconView.Left - (Width - iconView.Width) / 2;
                SmoothScrollTo(scrollPos, 0);
                _iconSelector = null;
            });
            Post(_iconSelector);
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            if (_iconSelector != null)
                Post(_iconSelector);
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            if (_iconSelector != null)
                RemoveCallbacks(_iconSelector);
        }

        public void SetViewPager(ViewPager view)
        {
            if (_viewPager == view) return;

            if (null != _viewPager)
                _viewPager.SetOnPageChangeListener(null);

            if (null == view.Adapter)
                throw new InvalidOperationException("ViewPager does not have an Adapter instance.");

            _viewPager = view;
            _viewPager.SetOnPageChangeListener(this);
            NotifyDataSetChanged();
        }

        public void SetViewPager(ViewPager view, int initialPosition)
        {
            SetViewPager(view);
            CurrentItem = initialPosition;
        }

        public void NotifyDataSetChanged()
        {
            _iconsLayout.RemoveAllViews();
            var iconAdapter = (IIconPageAdapter)_viewPager.Adapter;
            var count = iconAdapter.Count;
            for(var i = 0; i < count; i++)
            {
                var view = new ImageView(Context, null, Resource.Attribute.vpiIconPageIndicatorStyle);
                view.SetImageResource(iconAdapter.GetIconResId(i));
                _iconsLayout.AddView(view);
            }
            if (_selectedIndex > count)
                _selectedIndex = count - 1;
            CurrentItem = _selectedIndex;
            RequestLayout();
        }

        public void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener) { _listener = listener; }

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

        public int CurrentItem
        {
            get { return _selectedIndex; }
            set
            {
                if (null == _viewPager)
                    throw new InvalidOperationException("ViewPager has not been bound.");

                _viewPager.CurrentItem = value;
                _selectedIndex = value;

                var tabCount = _iconsLayout.ChildCount;
                for(var i = 0; i < tabCount; i++)
                {
                    var child = _iconsLayout.GetChildAt(i);
                    var selected = (i == value);
                    child.Selected = selected;
                    if (selected)
                        AnimateToIcon(value);
                }
            }
        }
    }
}