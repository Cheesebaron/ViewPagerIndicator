using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.ViewPager.Widget;
using DK.Ostebaronen.Droid.ViewPagerIndicator.Extensions;
using Java.Interop;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    [Register("dk.ostebaronen.droid.viewpagerindicator.CirclePageIndicator")]
    public class CirclePageIndicator
        : View
        , IPageIndicator
    {
        private const int InvalidPointer = -1;

        private float _radius;
        private readonly Paint _paintPageFill = new(PaintFlags.AntiAlias);
        private readonly Paint _paintStroke = new(PaintFlags.AntiAlias);
        private readonly Paint _paintFill = new(PaintFlags.AntiAlias);
        private ViewPager _viewPager;
        private ViewPager.IOnPageChangeListener _listener;
        private int _currentPage;
        private float _pageOffset;
        private int _scrollState;
        private int _orientation;
        private bool _centered;
        private bool _snap;

        private readonly int _touchSlop;
        private float _lastMotionX = -1;
        private int _activePointerId = InvalidPointer;
        private bool _isDragging;
        private float _extraSpacing;

        public event PageScrollStateChangedEventHandler PageScrollStateChanged;
        public event PageSelectedEventHandler PageSelected;
        public event PageScrolledEventHandler PageScrolled;

        public CirclePageIndicator(Context context)
            : this(context, null)
        {
        }

        public CirclePageIndicator(Context context, IAttributeSet attrs)
            : this(context, attrs, Resource.Attribute.vpiCirclePageIndicatorStyle)
        {
        }

        public CirclePageIndicator(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            if (IsInEditMode) return;

            var defaultPageColor = ContextCompat.GetColor(context, Resource.Color.default_circle_indicator_page_color);
            var defaultFillColor = ContextCompat.GetColor(context, Resource.Color.default_circle_indicator_fill_color);
            var defaultOrientation = Resources.GetInteger(Resource.Integer.default_circle_indicator_orientation);
            var defaultStrokeColor = ContextCompat.GetColor(context, Resource.Color.default_circle_indicator_stroke_color);
            var defaultStrokeWidth = Resources.GetDimension(Resource.Dimension.default_circle_indicator_stroke_width);
            var defaultRadius = Resources.GetDimension(Resource.Dimension.default_circle_indicator_radius);
            var defaultCentered = Resources.GetBoolean(Resource.Boolean.default_circle_indicator_centered);
            var defaultSnap = Resources.GetBoolean(Resource.Boolean.default_circle_indicator_snap);
            var defaultExtraSpacing = Resources.GetDimension(Resource.Dimension.default_circle_indicator_extra_spacing);

            using (var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.CirclePageIndicator, defStyle, 0))
            {
                _centered = a.GetBoolean(Resource.Styleable.CirclePageIndicator_centered, defaultCentered);
                _orientation = a.GetInt(Resource.Styleable.CirclePageIndicator_android_orientation, defaultOrientation);
                _paintPageFill.SetStyle(Paint.Style.Fill);
                _paintPageFill.Color = a.GetColor(Resource.Styleable.CirclePageIndicator_pageColor, defaultPageColor);
                _paintStroke.SetStyle(Paint.Style.Stroke);
                _paintStroke.Color = a.GetColor(Resource.Styleable.CirclePageIndicator_strokeColor, defaultStrokeColor);
                _paintStroke.StrokeWidth = a.GetDimension(Resource.Styleable.CirclePageIndicator_strokeWidth,
                                                          defaultStrokeWidth);
                _paintFill.SetStyle(Paint.Style.Fill);
                _paintFill.Color = a.GetColor(Resource.Styleable.CirclePageIndicator_fillColor, defaultFillColor);
                _radius = a.GetDimension(Resource.Styleable.CirclePageIndicator_radius, defaultRadius);
                _snap = a.GetBoolean(Resource.Styleable.CirclePageIndicator_snap, defaultSnap);
                _extraSpacing = a.GetDimension(Resource.Styleable.CirclePageIndicator_extraSpacing, defaultExtraSpacing);

                var background = a.GetDrawable(Resource.Styleable.CirclePageIndicator_android_background);
                if (null != background)
                    Background = background;

                a.Recycle();
            }

            using (var configuration = ViewConfiguration.Get(context))
                _touchSlop = configuration?.ScaledPagingTouchSlop ?? 0;
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

        public bool Centered
        {
            get => _centered;
            set
            {
                _centered = value;
                Invalidate();
            }
        }

        public Color PageColor
        {
            get => _paintFill.Color;
            set
            {
                _paintFill.Color = value;
                Invalidate();
            }
        }

        public Color FillColor
        {
            get => _paintPageFill.Color;
            set
            {
                _paintPageFill.Color = value;
                Invalidate();
            }
        }

        public int CircleOrientation
        {
            get => _orientation;
            set
            {
                switch (value)
                {
                    case (int)Orientation.Horizontal:
                    case (int)Orientation.Vertical:
                        _orientation = value;
                        RequestLayout();
                        break;
                    default:
                        throw new ArgumentException("Orientation must either be Horizontal or Vertical");
                }
            }
        }

        public Color StrokeColor
        {
            get => _paintStroke.Color;
            set
            {
                _paintStroke.Color = value;
                Invalidate();
            }
        }

        public float StrokeWidth
        {
            get => _paintStroke.StrokeWidth;
            set
            {
                _paintStroke.StrokeWidth = value;
                Invalidate();
            }
        }

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Invalidate();
            }
        }

        public bool Snap
        {
            get => _snap;
            set
            {
                _snap = value;
                Invalidate();
            }
        }

        public float ExtraSpacing
        {
            get => _extraSpacing;
            set
            {
                _extraSpacing = value;
                Invalidate();
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (_isDisposed)
                return;

            var count = ViewPager?.Adapter?.Count ?? 0;
            if (0 == count)
                return;

            if (_currentPage >= count)
            {
                CurrentItem = count - 1;
                return;
            }

            int longSize;
            int longPaddingBefore;
            int longPaddingAfter;
            int shortPaddingBefore;
            if (_orientation == (int)Orientation.Horizontal)
            {
                longSize = Width;
                longPaddingBefore = PaddingLeft;
                longPaddingAfter = PaddingRight;
                shortPaddingBefore = PaddingTop;
            }
            else
            {
                longSize = Height;
                longPaddingBefore = PaddingTop;
                longPaddingAfter = PaddingBottom;
                shortPaddingBefore = PaddingLeft;
            }

            var threeRadius = _radius * 2 + _extraSpacing;
            var shortOffset = shortPaddingBefore + _radius;
            var longOffset = longPaddingBefore + _radius;
            if (_centered)
            {
                longOffset += ((longSize - longPaddingBefore - longPaddingAfter) / 2.0f) - (((count - 1) * threeRadius + _radius * 2) / 2.0f);
            }


            float dX;
            float dY;

            var pageFillRadius = _radius;
            if (_paintStroke.StrokeWidth > 0)
                pageFillRadius -= _paintStroke.StrokeWidth / 2.0f;

            //Draw stroked circles
            for (var iLoop = 0; iLoop < count; iLoop++)
            {
                var drawLong = longOffset + (iLoop * threeRadius);
                if (_orientation == (int)Orientation.Horizontal)
                {
                    dX = drawLong;
                    dY = shortOffset;
                }
                else
                {
                    dX = shortOffset;
                    dY = drawLong;
                }

                //Only paint fill if not completely transparent
                if (_paintPageFill.Alpha > 0)
                    canvas.DrawCircle(dX, dY, pageFillRadius, _paintPageFill);

                //Only paint stroke if a stroke width was non-zero
                if (pageFillRadius != _radius)
                    canvas.DrawCircle(dX, dY, _radius, _paintStroke);
            }

            //Draw the filled circle according to the current scroll
            var cx = _currentPage * threeRadius;
            if (_snap)
                cx += _pageOffset * threeRadius;

            if (_orientation == (int)Orientation.Horizontal)
            {
                dX = longOffset + cx;
                dY = shortOffset;
            }
            else
            {
                dX = shortOffset;
                dY = longOffset + cx;
            }
            canvas.DrawCircle(dX, dY, _radius, _paintFill);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (base.OnTouchEvent(e))
                return true;

            if (_isDisposed)
                return false;

            if (0 == (ViewPager?.Adapter?.Count ?? 0))
                return false;

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
                        if (ViewPager != null && (ViewPager.IsFakeDragging || ViewPager.BeginFakeDrag()))
                            ViewPager.FakeDragBy(deltaX);
                    }

                    break;

                case MotionEventActions.Cancel:
                case MotionEventActions.Up:
                    if (!_isDragging)
                    {
                        var count = ViewPager.Adapter.Count;
                        var halfWidth = Width / 2f;
                        var sixthWidth = Width / 6f;

                        if ((_currentPage > 0) && (e.GetX() < halfWidth - sixthWidth))
                        {
                            if (action != MotionEventActions.Cancel)
                                ViewPager.CurrentItem = _currentPage - 1;
                            return true;
                        }
                        if ((_currentPage < count - 1) && (e.GetX() > halfWidth + sixthWidth))
                        {
                            if (action != MotionEventActions.Cancel)
                                ViewPager.CurrentItem = _currentPage + 1;
                            return true;
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

        public void SetViewPager(ViewPager view)
        {
            if (_isDisposed)
                return;

            if (_viewPager == view)
                return;

            if (!_viewPager.IsNull())
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

        public int CurrentItem
        {
            get => _currentPage;
            set
            {
                if (ViewPager == null)
                    throw new InvalidOperationException("ViewPager has not been bound.");

                ViewPager.CurrentItem = value;
                _currentPage = value;
                Invalidate();
            }
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
            if (_snap || _scrollState == ViewPager.ScrollStateIdle)
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
            if (_orientation == (int)Orientation.Horizontal)
                SetMeasuredDimension(MeasureLong(widthMeasureSpec), MeasureShort(heightMeasureSpec));
            else
                SetMeasuredDimension(MeasureShort(widthMeasureSpec), MeasureLong(heightMeasureSpec));
        }

        private int MeasureLong(int measureSpec)
        {
            int result;
            var specMode = MeasureSpec.GetMode(measureSpec);
            var specSize = MeasureSpec.GetSize(measureSpec);

            if (specMode == MeasureSpecMode.Exactly || null == ViewPager)
                result = specSize;
            else
            {
                //Calculate the width according to the views count
                var count = _viewPager.Adapter?.Count ?? 0;
                result = (int)(PaddingLeft + PaddingRight + (count * 2 * _radius) + (count - 1) * (_radius + _extraSpacing) + 1 + _extraSpacing);
                if (specMode == MeasureSpecMode.AtMost)
                    result = Math.Min(result, specSize);
            }
            return result;
        }

        private int MeasureShort(int measureSpec)
        {
            int result;
            var specMode = MeasureSpec.GetMode(measureSpec);
            var specSize = MeasureSpec.GetSize(measureSpec);

            if (specMode == MeasureSpecMode.Exactly)
                result = specSize;
            else
            {
                //Measure the height
                result = (int)(2 * _radius + PaddingTop + PaddingBottom + 1);
                //Respect AtMost value
                if (specMode == MeasureSpecMode.AtMost)
                    result = Math.Min(result, specSize);
            }
            return result;
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            var savedState = state as CircleSavedState;
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
            var savedState = new CircleSavedState(superState)
            {
                CurrentPage = _currentPage
            };
            return savedState;
        }

        public class CircleSavedState
            : BaseSavedState
        {
            public int CurrentPage { get; set; }

            public CircleSavedState(IParcelable superState)
                : base(superState) { }

            private CircleSavedState(Parcel parcel)
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
            public static CircleSavedStateCreator InitializeCreator()
            {
                return new CircleSavedStateCreator();
            }

            public class CircleSavedStateCreator : Java.Lang.Object, IParcelableCreator
            {
                public Java.Lang.Object CreateFromParcel(Parcel source)
                {
                    return new CircleSavedState(source);
                }

                public Java.Lang.Object[] NewArray(int size)
                {
                    return new CircleSavedState[size];
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
                _paintFill?.Dispose();
                _paintPageFill?.Dispose();
                _paintStroke?.Dispose();

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
