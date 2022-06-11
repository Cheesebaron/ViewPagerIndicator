using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator.Extensions;
using Java.Interop;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    public class CenterItemClickEventArgs
        : EventArgs
    {
        public int Position { get; set; }
    }

    public delegate void CenterItemClickEventHander(object sender, CenterItemClickEventArgs args);

    [Register("dk.ostebaronen.droid.viewpagerindicator.TitlePageIndicator")]
    public class TitlePageIndicator
        : View
        , IPageIndicator
    {
        private const float SelectionFadePercentage = 0.25f;
        private const float BoldFadePercentage = 0.05f;
        private const string EmptyTitle = "";

        public enum IndicatorStyle
        {
            None = 0,
            Triangle = 1,
            Underline = 2
        }

        public enum LinePosition
        {
            Bottom = 0,
            Top = 1
        }

        private ViewPager _viewPager;
        private ViewPager.IOnPageChangeListener _listener;
        private int _currentPage = -1;
        private float _pageOffset;
        private int _scrollState;
        private readonly Paint _paintText = new Paint(PaintFlags.AntiAlias);
        private bool _boldText;
        private Color _colorText;
        private Color _colorSelected;
        private readonly Path _path = new Path();
        private readonly Rect _bounds = new Rect();
        private readonly Paint _paintFooterLine = new Paint(PaintFlags.AntiAlias);
        private IndicatorStyle _footerIndicatorStyle;
        private LinePosition _linePosition;
        private readonly Paint _paintFooterIndicator = new Paint(PaintFlags.AntiAlias);
        private float _footerIndicatorHeight;
        private readonly float _footerIndicatorUnderlinePadding;
        private float _footerPadding;
        private float _titlePadding;
        private float _topPadding;
        private float _clipPadding;
        private float _footerLineHeight;

        private const int InvalidPointer = -1;

        private readonly int _touchSlop;
        private float _lastMotionX = -1;
        private int _activePointerId = InvalidPointer;
        private bool _isDragging;

        public event CenterItemClickEventHander CenterItemClick;
        public event PageScrollStateChangedEventHandler PageScrollStateChanged;
        public event PageSelectedEventHandler PageSelected;
        public event PageScrolledEventHandler PageScrolled;

        public TitlePageIndicator(Context context)
            : this(context, null)
        {
        }

        public TitlePageIndicator(Context context, IAttributeSet attrs)
            : this(context, attrs, Resource.Attribute.vpiTitlePageIndicatorStyle)
        {
        }

        public TitlePageIndicator(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            if (IsInEditMode) return;

            var res = Resources;

            //Load defaults from resources
            var defaultFooterColor = ContextCompat.GetColor(context, Resource.Color.default_title_indicator_footer_color);
            var defaultFooterLineHeight = res.GetDimension(Resource.Dimension.default_title_indicator_footer_line_height);
            var defaultFooterIndicatorStyle =
                res.GetInteger(Resource.Integer.default_title_indicator_footer_indicator_style);
            var defaultFooterIndicatorHeight =
                res.GetDimension(Resource.Dimension.default_title_indicator_footer_indicator_height);
            var defaultFooterIndicatorUnderlinePadding =
                res.GetDimension(Resource.Dimension.default_title_indicator_footer_indicator_underline_padding);
            var defaultFooterPadding = res.GetDimension(Resource.Dimension.default_title_indicator_footer_padding);
            var defaultLinePosition = res.GetInteger(Resource.Integer.default_title_indicator_line_position);
            var defaultSelectedColor = ContextCompat.GetColor(context, Resource.Color.default_title_indicator_selected_color);
            var defaultSelectedBold = res.GetBoolean(Resource.Boolean.default_title_indicator_selected_bold);
            var defaultTextColor = ContextCompat.GetColor(context, Resource.Color.default_title_indicator_text_color);
            var defaultTextSize = res.GetDimension(Resource.Dimension.default_title_indicator_text_size);
            var defaultTitlePadding = res.GetDimension(Resource.Dimension.default_title_indicator_title_padding);
            var defaultClipPadding = res.GetDimension(Resource.Dimension.default_title_indicator_clip_padding);
            var defaultTopPadding = res.GetDimension(Resource.Dimension.default_title_indicator_top_padding);

            using (var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.TitlePageIndicator, defStyle, 0))
            {
                _footerLineHeight = a.GetDimension(Resource.Styleable.TitlePageIndicator_footerLineHeight,
                                                   defaultFooterLineHeight);
                _footerIndicatorStyle =
                    (IndicatorStyle)a.GetInteger(Resource.Styleable.TitlePageIndicator_footerIndicatorStyle, defaultFooterIndicatorStyle);
                _footerIndicatorHeight = a.GetDimension(Resource.Styleable.TitlePageIndicator_footerIndicatorHeight,
                                                        defaultFooterIndicatorHeight);
                _footerIndicatorUnderlinePadding =
                    a.GetDimension(Resource.Styleable.TitlePageIndicator_footerIndicatorUnderlinePadding,
                                   defaultFooterIndicatorUnderlinePadding);
                _footerPadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_footerPadding, defaultFooterPadding);
                _linePosition =
                    (LinePosition)a.GetInteger(Resource.Styleable.TitlePageIndicator_linePosition, defaultLinePosition);
                _topPadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_topPadding, defaultTopPadding);
                _titlePadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_titlePadding, defaultTitlePadding);
                _clipPadding = a.GetDimension(Resource.Styleable.TitlePageIndicator_clipPadding, defaultClipPadding);
                _colorSelected = a.GetColor(Resource.Styleable.TitlePageIndicator_selectedColor, defaultSelectedColor);
                _colorText = a.GetColor(Resource.Styleable.TitlePageIndicator_android_textColor, defaultTextColor);
                _boldText = a.GetBoolean(Resource.Styleable.TitlePageIndicator_selectedBold, defaultSelectedBold);

                var textSize = a.GetDimension(Resource.Styleable.TitlePageIndicator_android_textSize, defaultTextSize);
                var footerColor = a.GetColor(Resource.Styleable.TitlePageIndicator_footerColor, defaultFooterColor);
                _paintText.TextSize = textSize;
                _paintFooterLine.SetStyle(Paint.Style.FillAndStroke);
                _paintFooterLine.StrokeWidth = _footerLineHeight;
                _paintFooterLine.Color = footerColor;
                _paintFooterIndicator.SetStyle(Paint.Style.FillAndStroke);
                _paintFooterIndicator.Color = footerColor;

                var background = a.GetDrawable(Resource.Styleable.TitlePageIndicator_android_background);
                if (null != background)
                    Background = background;

                a.Recycle();
            }

            using (var configuration = ViewConfiguration.Get(context))
                _touchSlop = configuration.ScaledPagingTouchSlop;
        }

        private ViewPager ViewPager
        {
            get
            {
                if (_viewPager.IsNull())
                    return null;

                return _viewPager;
            }
        }

        public Color FooterColor
        {
            get => _paintFooterLine.Color;
            set
            {
                _paintFooterLine.Color = value;
                _paintFooterIndicator.Color = value;
                Invalidate();
            }
        }

        public float FooterLineHeight
        {
            get => _footerLineHeight;
            set
            {
                _footerLineHeight = value;
                _paintFooterLine.StrokeWidth = value;
                Invalidate();
            }
        }

        public float FooterIndicatorHeight
        {
            get => _footerIndicatorHeight;
            set
            {
                _footerIndicatorHeight = value;
                Invalidate();
            }
        }

        public float FooterIndicatorPadding
        {
            get => _footerPadding;
            set
            {
                _footerPadding = value;
                Invalidate();
            }
        }

        public IndicatorStyle FooterIndicatorStyle
        {
            get => _footerIndicatorStyle;
            set
            {
                _footerIndicatorStyle = value;
                Invalidate();
            }
        }

        public LinePosition IndicatorLinePosition
        {
            get => _linePosition;
            set
            {
                _linePosition = value;
                Invalidate();
            }
        }

        public Color SelectedColor
        {
            get => _colorSelected;
            set
            {
                _colorSelected = value;
                Invalidate();
            }
        }

        public bool IsSelectedBold
        {
            get => _boldText;
            set
            {
                _boldText = value;
                Invalidate();
            }
        }

        public Color TextColor
        {
            get => _colorText;
            set
            {
                _colorText = value;
                Invalidate();
            }
        }

        public float TextSize
        {
            get => _paintText.TextSize;
            set
            {
                _paintText.TextSize = value;
                Invalidate();
            }
        }

        public float TitlePadding
        {
            get => _titlePadding;
            set
            {
                _titlePadding = value;
                Invalidate();
            }
        }

        public float TopPadding
        {
            get => _topPadding;
            set
            {
                _topPadding = value;
                Invalidate();
            }
        }

        public float ClipPadding
        {
            get => _clipPadding;
            set
            {
                _clipPadding = value;
                Invalidate();
            }
        }

        public Typeface Typeface
        {
            get => _paintText.Typeface;
            set
            {
                _paintText.SetTypeface(value);
                Invalidate();
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (null == ViewPager) return;

            var count = _viewPager.Adapter.Count;

            if (0 == count) return;

            if (-1 == _currentPage && ViewPager != null)
                _currentPage = _viewPager.CurrentItem;

            var bounds = CalculateAllBounds(_paintText);
            var boundsSize = bounds.Count;

            if (_currentPage >= boundsSize)
            {
                CurrentItem = boundsSize - 1;
                return;
            }

            var countMinusOne = count - 1;
            var halfWidth = Width / 2f;
            var leftClip = Left + ClipPadding;
            var right = Left + Width;
            var rightClip = right - ClipPadding;
            var height = Height;
            var left = Left;

            var page = _currentPage;
            float offsetPercent;
            if (_pageOffset <= 0.5)
                offsetPercent = _pageOffset;
            else
            {
                page += 1;
                offsetPercent = 1 - _pageOffset;
            }

            var currentSelected = (offsetPercent <= SelectionFadePercentage);
            var currentBold = (offsetPercent <= BoldFadePercentage);
            var selectedPercent = (SelectionFadePercentage - offsetPercent) / SelectionFadePercentage;

            //Verify if the current view must be clipped to the screen
            var curPageBound = bounds[_currentPage];
            var curPageWidth = curPageBound.Right - curPageBound.Left;
            if (curPageBound.Left < leftClip)
                ClipViewOnTheLeft(curPageBound, curPageWidth, left);
            if (curPageBound.Right > rightClip)
                ClipViewOnTheRight(curPageBound, curPageWidth, right);

            //Left view is starting from the current position
            if (_currentPage > 0)
            {
                for (var i = _currentPage - 1; i >= 0; i--)
                {
                    var bound = bounds[i];
                    //Is left side outside the screen?
                    if (bound.Left < leftClip)
                    {
                        var w = bound.Right - bound.Left;
                        //Clip to the left screen side
                        ClipViewOnTheLeft(bound, w, left);

                        var rightBound = bounds[i + 1];
                        //Except if there is an intersection with the right view
                        if (bound.Right + TitlePadding > rightBound.Left)
                        {
                            bound.Left = (int)(rightBound.Left - w - TitlePadding);
                            bound.Right = bound.Left + w;
                        }
                    }
                }
            }

            //Right view is starting from the current position
            if (_currentPage < countMinusOne)
            {
                for (var i = _currentPage + 1; i < count; i++)
                {
                    var bound = bounds[i];
                    //Is right side outside the screen?
                    if (bound.Right > rightClip)
                    {
                        var w = bound.Right - bound.Left;
                        //Clip to the right screen side
                        ClipViewOnTheRight(bound, w, Right);

                        var leftBound = bounds[i - 1];
                        //Except if there is an intersection with the left view
                        if (bound.Left - TitlePadding < leftBound.Right)
                        {
                            bound.Left = (int)(leftBound.Right + TitlePadding);
                            bound.Right = bound.Left + w;
                        }
                    }
                }
            }

            //Now draw views!
            var colorTextAlpha = _colorText.A;
            for (var i = 0; i < count; i++)
            {
                //Get the title
                var bound = bounds[i];
                //Only if one side is visible
                if ((bound.Left > left && bound.Left < right) || (bound.Right > left && bound.Right < right))
                {
                    var currentPage = (i == page);
                    var pageTitle = GetTitle(i);

                    //Only set bold if we are within bounds
                    _paintText.FakeBoldText = currentPage && currentBold && _boldText;

                    //Draw text as unselected
                    _paintText.Color = _colorText;
                    if (currentPage && currentSelected)
                        //Fade out/in unselected text as the selected text fades in/out
                        _paintText.Alpha = colorTextAlpha - (int)(colorTextAlpha * selectedPercent);

                    //Except if there is an intersection with the right view
                    if (i < boundsSize - 1)
                    {
                        var rightBound = bounds[i + 1];
                        if (bound.Right + TitlePadding > rightBound.Left)
                        {
                            var w = bound.Right - bound.Left;
                            bound.Left = (int)(rightBound.Left - w - TitlePadding);
                            bound.Right = bound.Left + w;
                        }
                    }
                    canvas.DrawText(pageTitle, 0, pageTitle.Length, bound.Left, bound.Bottom + TopPadding, _paintText);

                    //If we are within the selected bound draw the selected text
                    if (currentPage && currentSelected)
                    {
                        _paintText.Color = _colorSelected;
                        _paintText.Alpha = (int)(_colorSelected.A * selectedPercent);
                        canvas.DrawText(pageTitle, 0, pageTitle.Length, bound.Left, bound.Bottom + TopPadding, _paintText);
                    }
                }
            }

            //If we want the line on the top change height to zero and invert line height to trick the drawing code
            var footerLineHeight = _footerLineHeight;
            var footerIndicatorLineHeight = _footerIndicatorHeight;
            if (_linePosition == LinePosition.Top)
            {
                height = 0;
                footerLineHeight = -footerLineHeight;
                footerIndicatorLineHeight = -footerIndicatorLineHeight;
            }

            //Draw the footer line
            _path.Reset();
            _path.MoveTo(0, height - footerLineHeight / 2f);
            _path.MoveTo(Width, height - footerLineHeight / 2f);
            _path.Close();
            canvas.DrawPath(_path, _paintFooterLine);

            var heightMinusLine = height - footerLineHeight;
            switch (_footerIndicatorStyle)
            {
                case IndicatorStyle.Triangle:
                    _path.Reset();
                    _path.MoveTo(halfWidth, heightMinusLine - footerIndicatorLineHeight);
                    _path.LineTo(halfWidth + footerIndicatorLineHeight, heightMinusLine);
                    _path.LineTo(halfWidth - footerIndicatorLineHeight, heightMinusLine);
                    _path.Close();
                    canvas.DrawPath(_path, _paintFooterIndicator);
                    break;
                case IndicatorStyle.Underline:
                    if (!currentSelected || page >= boundsSize)
                        break;

                    var underlineBounds = bounds[page];
                    var rightPlusPadding = underlineBounds.Right + _footerIndicatorUnderlinePadding;
                    var leftMinusPadding = underlineBounds.Left - _footerIndicatorUnderlinePadding;
                    var heightMinusLineMinusIndicator = heightMinusLine - footerIndicatorLineHeight;

                    _path.Reset();
                    _path.MoveTo(leftMinusPadding, heightMinusLine);
                    _path.LineTo(rightPlusPadding, heightMinusLine);
                    _path.LineTo(rightPlusPadding, heightMinusLineMinusIndicator);
                    _path.LineTo(leftMinusPadding, heightMinusLineMinusIndicator);
                    _path.Close();

                    _paintFooterIndicator.Alpha = (int)(0xFF * selectedPercent);
                    canvas.DrawPath(_path, _paintFooterIndicator);
                    _paintFooterIndicator.Alpha = 0xFF;
                    break;
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (base.OnTouchEvent(e)) return true;
            if (ViewPager?.Adapter.Count == 0) return false;

            var action = e.ActionMasked;
            switch (action)
            {
                case MotionEventActions.Down:
                    _activePointerId = e.GetPointerId(0);
                    _lastMotionX = e.GetX();
                    break;
                case MotionEventActions.Move:
                    var activePointerIndex = e.FindPointerIndex(_activePointerId);
                    var x = e.GetX(activePointerIndex);
                    var deltaX = x - _lastMotionX;

                    if (!_isDragging)
                        if (Math.Abs(deltaX) > _touchSlop)
                            _isDragging = true;

                    if (_isDragging)
                    {
                        _lastMotionX = x;
                        if (ViewPager != null && (_viewPager.IsFakeDragging || _viewPager.BeginFakeDrag()))
                            _viewPager.FakeDragBy(deltaX);
                    }

                    break;

                case MotionEventActions.Cancel:
                case MotionEventActions.Up:
                    if (!_isDragging)
                    {
                        var count = _viewPager.Adapter.Count;
                        var halfWidth = Width / 2f;
                        var sixthWidth = Width / 6f;
                        var leftThird = halfWidth - sixthWidth;
                        var rightThird = halfWidth + sixthWidth;
                        var eventX = e.GetX();

                        if (eventX < leftThird)
                        {
                            if (_currentPage > 0)
                            {
                                if (action != MotionEventActions.Cancel)
                                    _viewPager.CurrentItem = _currentPage - 1;
                                return true;
                            }
                        }
                        else if (eventX > rightThird)
                        {
                            if (_currentPage < count - 1)
                            {
                                if (action != MotionEventActions.Cancel)
                                    _viewPager.CurrentItem = _currentPage + 1;
                                return true;
                            }
                        }
                        else
                        {
                            //Middle third
                            if (null != CenterItemClick && action != MotionEventActions.Cancel)
                                CenterItemClick(this, new CenterItemClickEventArgs { Position = _currentPage });
                        }
                    }

                    _isDragging = false;
                    _activePointerId = InvalidPointer;
                    if (ViewPager?.IsFakeDragging ?? false) ViewPager.EndFakeDrag();
                    break;

                case MotionEventActions.PointerDown:
                    {
                        var pointerIndex = e.ActionIndex;
                        _lastMotionX = e.GetX(pointerIndex);
                        _activePointerId = e.GetPointerId(pointerIndex);
                        break;
                    }

                case MotionEventActions.PointerUp:
                    {
                        var pointerIndex = e.ActionIndex;
                        var pointerId = e.GetPointerId(pointerIndex);
                        if (pointerId == _activePointerId)
                        {
                            var newPointerIndex = pointerIndex == 0 ? 1 : 0;
                            _activePointerId = e.GetPointerId(newPointerIndex);
                        }
                        _lastMotionX = e.GetX(e.FindPointerIndex(_activePointerId));
                        break;
                    }
            }

            return true;
        }

        private void ClipViewOnTheRight(Rect curViewBound, float curViewWidth, int right)
        {
            curViewBound.Right = (int)(right - ClipPadding);
            curViewBound.Left = (int)(curViewBound.Right - curViewWidth);
        }

        private void ClipViewOnTheLeft(Rect curViewBound, float curViewWidth, int left)
        {
            curViewBound.Left = (int)(left + ClipPadding);
            curViewBound.Right = (int)(ClipPadding + curViewWidth);
        }

        private IList<Rect> CalculateAllBounds(Paint paint)
        {
            var list = new List<Rect>();

            var count = _viewPager.Adapter.Count;
            var halfWidth = Width / 2;
            for (var i = 0; i < count; i++)
            {
                var bounds = CalcBounds(i, paint);
                var w = bounds.Right - bounds.Left;
                var h = bounds.Bottom - bounds.Top;
                bounds.Left = (int)(halfWidth - (w / 2f) + ((i - _currentPage - _pageOffset) * Width));
                bounds.Right = bounds.Left + w;
                bounds.Top = 0;
                bounds.Bottom = h;
                list.Add(bounds);
            }

            return list;
        }

        private Rect CalcBounds(int index, Paint paint)
        {
            var bounds = new Rect();
            var title = GetTitle(index);
            bounds.Right = (int)paint.MeasureText(title);
            bounds.Bottom = (int)(paint.Descent() - paint.Ascent());
            return bounds;
        }

        private String GetTitle(int i)
        {
            var title = _viewPager.Adapter.GetPageTitle(i);
            return title ?? EmptyTitle;
        }

        public int CurrentItem
        {
            get { return _currentPage; }
            set
            {
                if (null == _viewPager)
                    throw new InvalidOperationException("ViewPager has not been bound.");

                _viewPager.SetCurrentItem(value, true);
                _currentPage = value;
                Invalidate();
            }
        }

        public void SetViewPager(ViewPager view)
        {
            if (_viewPager == view) return;

            if (null != ViewPager)
                _viewPager.ClearOnPageChangeListeners();

            if (null == view.Adapter)
                throw new InvalidOperationException("ViewPager does not have an Adapter instance.");

            _viewPager = view;
            _viewPager.AddOnPageChangeListener(this);
            Invalidate();
        }

        public void SetViewPager(ViewPager view, int initialPosition)
        {
            SetViewPager(view);
            CurrentItem = initialPosition;
        }

        public void NotifyDataSetChanged()
        {
            Invalidate();
        }

        public void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener) { _listener = listener; }

        public void OnPageScrollStateChanged(int state)
        {
            _scrollState = state;

            if (null != _listener)
                _listener.OnPageScrollStateChanged(state);

            PageScrollStateChanged?.Invoke(this, new PageScrollStateChangedEventArgs { State = state });
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            _currentPage = position;
            _pageOffset = positionOffset;
            Invalidate();

            if (null != _listener)
                _listener.OnPageScrolled(position, positionOffset, positionOffsetPixels);

            PageScrolled?.Invoke(this,
                new PageScrolledEventArgs
                {
                    Position = position,
                    PositionOffset = positionOffset,
                    PositionOffsetPixels = positionOffsetPixels
                });
        }

        public void OnPageSelected(int position)
        {
            if (_scrollState == ViewPager.ScrollStateIdle)
            {
                _currentPage = position;
                Invalidate();
            }

            if (null != _listener)
                _listener.OnPageSelected(position);

            PageSelected?.Invoke(this, new PageSelectedEventArgs { Position = position });
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var measuredWidth = MeasureSpec.GetSize(widthMeasureSpec);

            //Determine the height
            float height;
            var heightSpecMode = MeasureSpec.GetMode(heightMeasureSpec);
            if (heightSpecMode == MeasureSpecMode.Exactly)
                height = MeasureSpec.GetSize(heightMeasureSpec);
            else
            {
                //Calculate text bounds
                _bounds.SetEmpty();
                _bounds.Bottom = (int)(_paintText.Descent() - _paintText.Ascent());
                height = _bounds.Bottom - _bounds.Top + _footerLineHeight + _footerPadding + _topPadding;
                if (_footerIndicatorStyle != IndicatorStyle.None)
                    height += _footerIndicatorHeight;
            }
            var measuredHeight = (int)height;

            SetMeasuredDimension(measuredWidth, measuredHeight);
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            var savedState = state as TitleSavedState;
            if (savedState != null)
            {
                base.OnRestoreInstanceState(savedState.SuperState);
                _currentPage = savedState.CurrentPage;
            }
            else
                base.OnRestoreInstanceState(state);
            RequestLayout();
        }

        protected override IParcelable OnSaveInstanceState()
        {
            var superState = base.OnSaveInstanceState();
            var savedState = new TitleSavedState(superState)
            {
                CurrentPage = _currentPage
            };
            return savedState;
        }

        public class TitleSavedState
            : BaseSavedState
        {
            public int CurrentPage { get; set; }

            public TitleSavedState(IParcelable superState)
                : base(superState)
            {
            }

            private TitleSavedState(Parcel parcel)
                : base(parcel)
            {
                CurrentPage = parcel.ReadInt();
            }

            public override void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);
                dest.WriteInt(CurrentPage);
            }
            [ExportField("CREATOR")]
            public static TitleSavedStateCreator InitializeCreator()
            {
                return new TitleSavedStateCreator();
            }

            public class TitleSavedStateCreator : Java.Lang.Object, IParcelableCreator
            {
                public Java.Lang.Object CreateFromParcel(Parcel source)
                {
                    return new TitleSavedState(source);
                }

                public Java.Lang.Object[] NewArray(int size)
                {
                    return new TitleSavedState[size];
                }
            }
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _paintText?.Dispose();
                _paintFooterIndicator?.Dispose();
                _paintFooterLine?.Dispose();

                _path?.Dispose();

                if (_viewPager != null)
                {
                    _viewPager.RemoveOnPageChangeListener(this);
                    _viewPager = null;
                }
            }

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
}
