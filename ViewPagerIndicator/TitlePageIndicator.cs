using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Dk.Ostebaronen.Android.ViewPagerIndicator.Interfaces;
using Java.Interop;
using Java.Lang;

namespace Dk.Ostebaronen.Android.ViewPagerIndicator
{
    public class TitlePageIndicator : View, IPageIndicator
    {
        public enum IndicatorStyle
        {
            None = 0,
            Triangle = 1,
            Underline = 2
        }

        /**
	     * Percentage indicating what percentage of the screen width away from
	     * center should the underline be fully faded. A value of 0.25 means that
	     * halfway between the center of the screen and an edge.
	     */
        private const float SelectionFadePercentage = 0.25f;

        /**
         * Percentage indicating what percentage of the screen width away from
         * center should the selected text bold turn off. A value of 0.05 means
         * that 10% between the center and an edge.
         */
        private const float BoldFadePercentage = 0.05f;
        private const int InvalidPointer = -1;
        private readonly float _footerIndicatorUnderlinePadding;
        private readonly Paint _paintFooterIndicator = new Paint();
        private readonly Paint _paintFooterLine = new Paint();

        /**
         * Interface for a callback when the center item has been clicked.
         */

        private readonly Paint _paintText = new Paint();
        private readonly int _touchSlop;
        private int _activePointerId = InvalidPointer;
        private bool _boldText;
        private IOnCenterItemClickListener _centerItemClickListener;
        private float _clipPadding;
        private Color _colorSelected;
        private Color _colorText;
        private int _currentOffset;
        private int _currentPage;
        private float _footerIndicatorHeight;
        private IndicatorStyle _footerIndicatorStyle;
        private float _footerLineHeight;
        private float _footerPadding;
        private bool _isDragging;
        private float _lastMotionX = -1;
        private ViewPager.IOnPageChangeListener _listener;
        private Path _path;
        private int _scrollState;
        private float _titlePadding;
        private ITitleProvider _titleProvider;
        private float _topPadding;
        private ViewPager _viewPager;
        /** Left and right side padding for not active view titles. */

        public event CenterItemClickEventHandler CenterItemClick;

        public TitlePageIndicator(Context context)
            : this(context, null) { }

        public TitlePageIndicator(Context context, IAttributeSet attrs)
            : this(context, attrs, Resource.Attribute.vpiTitlePageIndicatorStyle) { }

        public TitlePageIndicator(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            //Load defaults from resources
            var res = Resources;
            var defaultFooterColor = res.GetColor(Resource.Color.default_title_indicator_footer_color);
            var defaultFooterLineHeight =
                res.GetDimension(Resource.Dimension.default_title_indicator_footer_line_height);
            var defaultFooterIndicatorStyle =
                res.GetInteger(Resource.Integer.default_title_indicator_footer_indicator_style);
            var defaultFooterIndicatorHeight =
                res.GetDimension(Resource.Dimension.default_title_indicator_footer_indicator_height);
            var defaultFooterIndicatorUnderlinePadding =
                res.GetDimension(Resource.Dimension.default_title_indicator_footer_indicator_underline_padding);
            var defaultFooterPadding = res.GetDimension(Resource.Dimension.default_title_indicator_footer_padding);
            var defaultSelectedColor = res.GetColor(Resource.Color.default_title_indicator_selected_color);
            var defaultSelectedBold = res.GetBoolean(Resource.Boolean.default_title_indicator_selected_bold);
            var defaultTextColor = res.GetColor(Resource.Color.default_title_indicator_text_color);
            var defaultTextSize = res.GetDimension(Resource.Dimension.default_title_indicator_text_size);
            var defaultTitlePadding = res.GetDimension(Resource.Dimension.default_title_indicator_title_padding);
            var defaultClipPadding = res.GetDimension(Resource.Dimension.default_title_indicator_clip_padding);
            var defaultTopPadding = res.GetDimension(Resource.Dimension.default_title_indicator_top_padding);

            //Retrieve styles attributes
            var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.TitlePageIndicator, defStyle,
                                                          Resource.Style.Widget_TitlePageIndicator);

            //Retrieve the colors to be used for this view and apply them.
            _footerLineHeight = a.GetDimension(Resource.Styleable.TitlePageIndicator_footerLineHeight,
                                               defaultFooterLineHeight);
            _footerIndicatorStyle =
                (IndicatorStyle)
                a.GetInteger(Resource.Styleable.TitlePageIndicator_footerIndicatorStyle, defaultFooterIndicatorStyle);
            _footerIndicatorHeight = a.GetDimension(Resource.Styleable.TitlePageIndicator_footerIndicatorHeight,
                                                    defaultFooterIndicatorHeight);
            _footerIndicatorUnderlinePadding =
                a.GetDimension(Resource.Styleable.TitlePageIndicator_footerIndicatorUnderlinePadding,
                               defaultFooterIndicatorUnderlinePadding);
            _footerPadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_footerPadding, defaultFooterPadding);
            _topPadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_topPadding, defaultTopPadding);
            _titlePadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_titlePadding, defaultTitlePadding);
            _clipPadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_clipPadding, defaultClipPadding);
            _colorSelected = a.GetColor(Resource.Styleable.TitlePageIndicator_selectedColor, defaultSelectedColor);
            _colorText = a.GetColor(Resource.Styleable.TitlePageIndicator_textColor, defaultTextColor);
            _boldText = a.GetBoolean(Resource.Styleable.TitlePageIndicator_selectedBold, defaultSelectedBold);

            var textSize = a.GetDimension(Resource.Styleable.TitlePageIndicator_textSize, defaultTextSize);
            var footerColor = a.GetColor(Resource.Styleable.TitlePageIndicator_footerColor, defaultFooterColor);
            _paintText.TextSize = textSize;
            _paintText.AntiAlias = true;
            _paintFooterLine.SetStyle(Paint.Style.FillAndStroke);
            _paintFooterLine.StrokeWidth = _footerLineHeight;
            _paintFooterLine.Color = footerColor;
            _paintFooterIndicator.SetStyle(Paint.Style.FillAndStroke);
            _paintFooterIndicator.Color = footerColor;

            var background = a.GetDrawable(Resource.Styleable.TitlePageIndicator_android_background);
            if (null != background)
                SetBackgroundDrawable(background);

            a.Recycle();

            var configuration = ViewConfiguration.Get(context);
            _touchSlop = ViewConfigurationCompat.GetScaledPagingTouchSlop(configuration);
        }

        public void OnPageScrollStateChanged(int state)
        {
            _scrollState = state;

            if(_listener != null)
            {
                _listener.OnPageScrollStateChanged(state);
            }
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            _currentPage = position;
            _currentOffset = positionOffsetPixels;
            Invalidate();

            if(_listener != null)
            {
                _listener.OnPageScrolled(position, positionOffset, positionOffsetPixels);
            }
        }

        public void OnPageSelected(int position)
        {
            if(_scrollState == ViewPager.ScrollStateIdle)
            {
                _currentPage = position;
                Invalidate();
            }

            if(_listener != null)
            {
                _listener.OnPageSelected(position);
            }
        }

        public void SetViewPager(ViewPager view)
        {
            PagerAdapter adapter = view.Adapter;
            if(adapter == null)
            {
                throw new IllegalStateException("ViewPager does not have adapter instance.");
            }
            if(!(adapter is ITitleProvider))
            {
                throw new IllegalStateException(
                    "ViewPager adapter must implement TitleProvider to be used with TitlePageIndicator.");
            }
            _viewPager = view;
            _viewPager.SetOnPageChangeListener(this);
            _titleProvider = (ITitleProvider)adapter;
            Invalidate();
        }

        public void SetViewPager(ViewPager view, int initialPosition)
        {
            SetViewPager(view);
            SetCurrentItem(initialPosition);
        }

        public void SetCurrentItem(int item)
        {
            if(_viewPager == null)
            {
                throw new IllegalStateException("ViewPager has not been bound.");
            }
            _viewPager.CurrentItem = item;
            _currentPage = item;
            Invalidate();
        }

        public void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener) { _listener = listener; }

        public void NotifyDataSetChanged() { Invalidate(); }

        public int GetFooterColor() { return _paintFooterLine.Color; }

        public void SetFooterColor(Color footerColor)
        {
            _paintFooterLine.Color = footerColor;
            _paintFooterIndicator.Color = footerColor;
            Invalidate();
        }

        public float GetFooterLineHeight() { return _footerLineHeight; }

        public void SetFooterLineHeight(float footerLineHeight)
        {
            _footerLineHeight = footerLineHeight;
            _paintFooterLine.StrokeWidth = _footerLineHeight;
            Invalidate();
        }

        public float GetFooterIndicatorHeight() { return _footerIndicatorHeight; }

        public void SetFooterIndicatorHeight(float footerTriangleHeight)
        {
            _footerIndicatorHeight = footerTriangleHeight;
            Invalidate();
        }

        public float GetFooterIndicatorPadding() { return _footerPadding; }

        public void SetFooterIndicatorPadding(float footerIndicatorPadding)
        {
            _footerPadding = footerIndicatorPadding;
            Invalidate();
        }

        public IndicatorStyle GetFooterIndicatorStyle() { return _footerIndicatorStyle; }

        public void SetFooterIndicatorStyle(IndicatorStyle indicatorStyle)
        {
            _footerIndicatorStyle = indicatorStyle;
            Invalidate();
        }

        public Color GetSelectedColor() { return _colorSelected; }

        public void SetSelectedColor(Color selectedColor)
        {
            _colorSelected = selectedColor;
            Invalidate();
        }

        public bool IsSelectedBold() { return _boldText; }

        public void SetSelectedBold(bool selectedBold)
        {
            _boldText = selectedBold;
            Invalidate();
        }

        public int GetTextColor() { return _colorText; }

        public void SetTextColor(Color textColor)
        {
            _paintText.Color = textColor;
            _colorText = textColor;
            Invalidate();
        }

        public float GetTextSize() { return _paintText.TextSize; }

        public void SetTextSize(float textSize)
        {
            _paintText.TextSize = textSize;
            Invalidate();
        }

        public float GetTitlePadding() { return _titlePadding; }

        public void SetTitlePadding(float titlePadding)
        {
            _titlePadding = titlePadding;
            Invalidate();
        }

        public float GetTopPadding() { return _topPadding; }

        public void SetTopPadding(float topPadding)
        {
            _topPadding = topPadding;
            Invalidate();
        }

        public float GetClipPadding() { return _clipPadding; }

        public void SetClipPadding(float clipPadding)
        {
            _clipPadding = clipPadding;
            Invalidate();
        }

        public void SetTypeface(Typeface typeface)
        {
            _paintText.SetTypeface(typeface);
            Invalidate();
        }

        public Typeface GetTypeface() { return _paintText.Typeface; }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if(_viewPager == null)
            {
                return;
            }
            var count = _viewPager.Adapter.Count;
            if(count == 0)
            {
                return;
            }

            //Calculate views bounds
            var bounds = CalculateAllBounds(_paintText);
            var boundsSize = bounds.Count;

            //Make sure we're on a page that still exists
            if(_currentPage >= boundsSize)
            {
                SetCurrentItem(boundsSize - 1);
                return;
            }

            var countMinusOne = count - 1;
            var halfWidth = Width / 2f;
            var left = Left;
            var leftClip = left + _clipPadding;
            var width = Width;
            var height = Height;
            var right = left + width;
            var rightClip = right - _clipPadding;

            var page = _currentPage;
            float offsetPercent;
            if(_currentOffset <= halfWidth)
            {
                offsetPercent = 1.0f * _currentOffset / width;
            }
            else
            {
                page += 1;
                offsetPercent = 1.0f * (width - _currentOffset) / width;
            }
            var currentSelected = (offsetPercent <= SelectionFadePercentage);
            var currentBold = (offsetPercent <= BoldFadePercentage);
            var selectedPercent = (SelectionFadePercentage - offsetPercent) / SelectionFadePercentage;

            //Verify if the current view must be clipped to the screen
            var curPageBound = bounds[_currentPage];
            var curPageWidth = curPageBound.Right - curPageBound.Left;
            if(curPageBound.Left < leftClip)
            {
                //Try to clip to the screen (left side)
                ClipViewOnTheLeft(curPageBound, curPageWidth, left);
            }
            if(curPageBound.Right > rightClip)
            {
                //Try to clip to the screen (right side)
                ClipViewOnTheRight(curPageBound, curPageWidth, right);
            }

            //Left views starting from the current position
            if(_currentPage > 0)
            {
                for(var i = _currentPage - 1; i >= 0; i--)
                {
                    var bound = bounds[i];
                    //Is left side is outside the screen
                    if(bound.Left < leftClip)
                    {
                        var w = bound.Right - bound.Left;
                        //Try to clip to the screen (left side)
                        ClipViewOnTheLeft(bound, w, left);
                        //Except if there's an intersection with the right view
                        var rightBound = bounds[i + 1];
                        //Intersection
                        if(bound.Right + _titlePadding > rightBound.Left)
                        {
                            bound.Left = rightBound.Left - w - _titlePadding;
                            bound.Right = bound.Left + w;
                        }
                    }
                }
            }
            //Right views starting from the current position
            if(_currentPage < countMinusOne)
            {
                for(var i = _currentPage + 1; i < count; i++)
                {
                    var bound = bounds[i];
                    //If right side is outside the screen
                    if(bound.Right > rightClip)
                    {
                        var w = bound.Right - bound.Left;
                        //Try to clip to the screen (right side)
                        ClipViewOnTheRight(bound, w, right);
                        //Except if there's an intersection with the left view
                        var leftBound = bounds[i - 1];
                        //Intersection
                        if(bound.Left - _titlePadding < leftBound.Right)
                        {
                            bound.Left = leftBound.Right + _titlePadding;
                            bound.Right = bound.Left + w;
                        }
                    }
                }
            }

            //Now draw views
            //int colorTextAlpha = _colorText >>> 24;
            var colorTextAlpha = _colorText >> 24;
            for(var i = 0; i < count; i++)
            {
                //Get the title
                var bound = bounds[i];
                //Only if one side is visible
                if((bound.Left > left && bound.Left < right) || (bound.Right > left && bound.Right < right))
                {
                    var currentPage = (i == page);
                    //Only set bold if we are within bounds
                    _paintText.FakeBoldText = (currentPage && currentBold && _boldText);

                    //Draw text as unselected
                    _paintText.Color = (_colorText);
                    if(currentPage && currentSelected)
                    {
                        //Fade out/in unselected text as the selected text fades in/out
                        _paintText.Alpha = (colorTextAlpha - (int)(colorTextAlpha * selectedPercent));
                    }

                    //Except if there's an intersection with the right view
                    if(i < boundsSize - 1)
                    {
                        var rightBound = bounds[i + 1];
                        //Intersection
                        if(bound.Right + _titlePadding > rightBound.Left)
                        {
                            var w = bound.Right - bound.Left;
                            bound.Left = (int)(rightBound.Left - w - _titlePadding);
                            bound.Right = bound.Left + w;
                        }
                    }
                    canvas.DrawText(_titleProvider.GetTitle(i), bound.Left, bound.Bottom + _topPadding, _paintText);

                    //If we are within the selected bounds draw the selected text
                    if(currentPage && currentSelected)
                    {
                        _paintText.Color = _colorSelected;
                        _paintText.Alpha = ((int)((_colorSelected >> 24) * selectedPercent));
                        canvas.DrawText(_titleProvider.GetTitle(i), bound.Left, bound.Bottom + _topPadding, _paintText);
                    }
                }
            }

            //Draw the footer line
            _path = new Path();
            _path.MoveTo(0, height - _footerLineHeight / 2f);
            _path.LineTo(width, height - _footerLineHeight / 2f);
            _path.Close();
            canvas.DrawPath(_path, _paintFooterLine);

            switch(_footerIndicatorStyle)
            {
                case IndicatorStyle.Triangle:
                    _path = new Path();
                    _path.MoveTo(halfWidth, height - _footerLineHeight - _footerIndicatorHeight);
                    _path.LineTo(halfWidth + _footerIndicatorHeight, height - _footerLineHeight);
                    _path.LineTo(halfWidth - _footerIndicatorHeight, height - _footerLineHeight);
                    _path.Close();
                    canvas.DrawPath(_path, _paintFooterIndicator);
                    break;

                case IndicatorStyle.Underline:
                    if(!currentSelected || page >= boundsSize)
                    {
                        break;
                    }

                    var underlineBounds = bounds[page];
                    _path = new Path();
                    _path.MoveTo(underlineBounds.Left - _footerIndicatorUnderlinePadding, height - _footerLineHeight);
                    _path.LineTo(underlineBounds.Right + _footerIndicatorUnderlinePadding, height - _footerLineHeight);
                    _path.LineTo(underlineBounds.Right + _footerIndicatorUnderlinePadding,
                                 height - _footerLineHeight - _footerIndicatorHeight);
                    _path.LineTo(underlineBounds.Left - _footerIndicatorUnderlinePadding,
                                 height - _footerLineHeight - _footerIndicatorHeight);
                    _path.Close();

                    _paintFooterIndicator.Alpha = ((int)(0xFF * selectedPercent));
                    canvas.DrawPath(_path, _paintFooterIndicator);
                    _paintFooterIndicator.Alpha = (0xFF);
                    break;
            }
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            if(base.OnTouchEvent(ev))
            {
                return true;
            }
            if((_viewPager == null) || (_viewPager.Adapter.Count == 0))
            {
                return false;
            }

            var action = ev.Action;

            switch((int)action & MotionEventCompat.ActionMask)
            {
                case (int)MotionEventActions.Down:
                    _activePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    _lastMotionX = ev.GetX();
                    break;

                case (int)MotionEventActions.Move:
                {
                    var activePointerIndex = MotionEventCompat.FindPointerIndex(ev, _activePointerId);
                    var x = MotionEventCompat.GetX(ev, activePointerIndex);
                    var deltaX = x - _lastMotionX;

                    if(!_isDragging)
                    {
                        if(Math.Abs(deltaX) > _touchSlop)
                        {
                            _isDragging = true;
                        }
                    }

                    if(_isDragging)
                    {
                        if(!_viewPager.IsFakeDragging)
                        {
                            _viewPager.BeginFakeDrag();
                        }

                        _lastMotionX = x;

                        _viewPager.FakeDragBy(deltaX);
                    }

                    break;
                }

                case (int)MotionEventActions.Cancel:
                case (int)MotionEventActions.Up:
                    if(!_isDragging)
                    {
                        var count = _viewPager.Adapter.Count;
                        var width = Width;
                        var halfWidth = width / 2f;
                        var sixthWidth = width / 6f;
                        var leftThird = halfWidth - sixthWidth;
                        var rightThird = halfWidth + sixthWidth;
                        var eventX = ev.GetX();

                        if(eventX < leftThird)
                        {
                            if(_currentPage > 0)
                            {
                                _viewPager.CurrentItem = _currentPage - 1;
                                return true;
                            }
                        }
                        else if(eventX > rightThird)
                        {
                            if(_currentPage < count - 1)
                            {
                                _viewPager.CurrentItem = _currentPage + 1;
                                return true;
                            }
                        }
                        else
                        {
                            //Middle third
                            if(_centerItemClickListener != null)
                            {
                                _centerItemClickListener.OnCenterItemClick(_currentPage);
                            }

                            if(CenterItemClick != null)
                                CenterItemClick(this, new CenterItemClickEventArgs { CurrentPage = _currentPage });
                        }
                    }


                    _isDragging = false;
                    _activePointerId = InvalidPointer;
                    if(_viewPager.IsFakeDragging)
                        _viewPager.EndFakeDrag();
                    break;

                case MotionEventCompat.ActionPointerDown:
                {
                    var index = MotionEventCompat.GetActionIndex(ev);
                    var x = MotionEventCompat.GetX(ev, index);
                    _lastMotionX = x;
                    _activePointerId = MotionEventCompat.GetPointerId(ev, index);
                    break;
                }

                case MotionEventCompat.ActionPointerUp:
                    var pointerIndex = MotionEventCompat.GetActionIndex(ev);
                    var pointerId = MotionEventCompat.GetPointerId(ev, pointerIndex);
                    if(pointerId == _activePointerId)
                    {
                        var newPointerIndex = pointerIndex == 0 ? 1 : 0;
                        _activePointerId = MotionEventCompat.GetPointerId(ev, newPointerIndex);
                    }
                    _lastMotionX = MotionEventCompat.GetX(ev, MotionEventCompat.FindPointerIndex(ev, _activePointerId));
                    break;
            }

            return true;
        }

        /**
	     * Set bounds for the right textView including clip padding.
	     *
	     * @param curViewBound
	     *            current bounds.
	     * @param curViewWidth
	     *            width of the view.
	     */

        private void ClipViewOnTheRight(RectF curViewBound, float curViewWidth, int right)
        {
            curViewBound.Right = right - _clipPadding;
            curViewBound.Left = curViewBound.Right - curViewWidth;
        }

        /**
         * Set bounds for the left textView including clip padding.
         *
         * @param curViewBound
         *            current bounds.
         * @param curViewWidth
         *            width of the view.
         */

        private void ClipViewOnTheLeft(RectF curViewBound, float curViewWidth, int left)
        {
            curViewBound.Left = left + _clipPadding;
            curViewBound.Right = _clipPadding + curViewWidth;
        }

        /**
	     * Calculate views bounds and scroll them according to the current index
	     *
	     * @param paint
	     * @param currentIndex
	     * @return
	     */

        private List<RectF> CalculateAllBounds(Paint paint)
        {
            var list = new List<RectF>();
            //For each views (If no values then add a fake one)
            var count = _viewPager.Adapter.Count;
            var width = Width;
            var halfWidth = width / 2;

            for(var i = 0; i < count; i++)
            {
                var bounds = CalcBounds(i, paint);
                var w = (bounds.Right - bounds.Left);
                var h = (bounds.Bottom - bounds.Top);
                bounds.Left = (halfWidth) - (w / 2) - _currentOffset + ((i - _currentPage) * width);
                bounds.Right = bounds.Left + w;
                bounds.Top = 0;
                bounds.Bottom = h;
                list.Add(bounds);
            }

            return list;
        }

        /**
         * Calculate the bounds for a view's title
         *
         * @param index
         * @param paint
         * @return
         */

        private RectF CalcBounds(int index, Paint paint)
        {
            //Calculate the text bounds
            var bounds = new RectF
            {
                Right = paint.MeasureText(_titleProvider.GetTitle(index)),
                Bottom = paint.Descent() - paint.Ascent()
            };
            return bounds;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            //Measure our width in whatever mode specified
            var measuredWidth = MeasureSpec.GetSize(widthMeasureSpec);

            //Determine our height
            float height;
            var heightSpecMode = MeasureSpec.GetMode(heightMeasureSpec);
            if(heightSpecMode == MeasureSpecMode.Exactly)
            {
                //We were told how big to be
                height = MeasureSpec.GetSize(heightMeasureSpec);
            }
            else
            {
                //Calculate the text bounds
                var bounds = new RectF {Bottom = _paintText.Descent() - _paintText.Ascent()};
                height = bounds.Bottom - bounds.Top + _footerLineHeight + _footerPadding + _topPadding;
                if(_footerIndicatorStyle != IndicatorStyle.None)
                {
                    height += _footerIndicatorHeight;
                }
            }
            var measuredHeight = (int)height;

            SetMeasuredDimension(measuredWidth, measuredHeight);
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            try
            {
                var savedState = (SavedState)state;
                base.OnRestoreInstanceState(savedState.SuperState);
                _currentPage = savedState.CurrentPage;
            }
            catch
            {
                base.OnRestoreInstanceState(state);
                // Ignore, this needs to support IParcelable...
            }
            RequestLayout();
        }

        protected override IParcelable OnSaveInstanceState()
        {
            var superState = base.OnSaveInstanceState();
            var savedState = new SavedState(superState)
            {
                CurrentPage = _currentPage
            };
            return savedState;
        }

        public interface IOnCenterItemClickListener
        {
            /**
             * Callback when the center item has been clicked.
             *
             * @param position Position of the current center item.
             */
            void OnCenterItemClick(int position);
        }

        public void SetOnCenterItemClickListener(IOnCenterItemClickListener listener)
        {
            _centerItemClickListener = listener;
        }

        public class SavedState : BaseSavedState
        {
            public SavedState(IParcelable superState)
                : base(superState) { }

            public SavedState(Parcel parcel)
                : base(parcel) { CurrentPage = parcel.ReadInt(); }

            public int CurrentPage { get; set; }

            public override void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);
                dest.WriteInt(CurrentPage);
            }

            [ExportField("CREATOR")]
            private static SavedStateCreator InitializeCreator() { return new SavedStateCreator(); }

            private class SavedStateCreator : Object, IParcelableCreator
            {
                public Object CreateFromParcel(Parcel source) { return new SavedState(source); }

                public Object[] NewArray(int size) { return new SavedState[size]; }
            }
        }
    }
}
