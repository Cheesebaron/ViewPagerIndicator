using System;
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
    [Register("dk.ostebaronen.droid.viewpagerindicator.LinePageIndicator")]
    public class LinePageIndicator
        : View
        , IPageIndicator
    {
        private const int InvalidPointer = -1;

        private readonly Paint _paintUnSelected = new Paint(PaintFlags.AntiAlias);
        private readonly Paint _paintSelected = new Paint(PaintFlags.AntiAlias);
        private ViewPager _viewPager;
        private ViewPager.IOnPageChangeListener _listener;
        private int _currentPage;
        private bool _centered;
        private float _lineWidth;
        private float _gapWidth;

        private readonly int _touchSlop;
        private float _lastMotionX = -1;
        private int _activePointerId = InvalidPointer;
        private bool _isDragging;

        public event PageScrollStateChangedEventHandler PageScrollStateChanged;
        public event PageSelectedEventHandler PageSelected;
        public event PageScrolledEventHandler PageScrolled;

        public LinePageIndicator(Context context)
            : this(context, null)
        {
        }

        public LinePageIndicator(Context context, IAttributeSet attrs)
            : this(context, attrs, Resource.Attribute.vpiLinePageIndicatorStyle)
        {
        }

        public LinePageIndicator(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            if (IsInEditMode) return;

            var res = Resources;

            //Load defaults from resources
            var defaultSelectedColor = ContextCompat.GetColor(context, Resource.Color.default_line_indicator_selected_color);
            var defaultUnselectedColor = ContextCompat.GetColor(context, Resource.Color.default_line_indicator_unselected_color);
            var defaultLineWidth = res.GetDimension(Resource.Dimension.default_line_indicator_line_width);
            var defaultGapWidth = res.GetDimension(Resource.Dimension.default_line_indicator_gap_width);
            var defaultStrokeWidth = res.GetDimension(Resource.Dimension.default_line_indicator_stroke_width);
            var defaultCentered = res.GetBoolean(Resource.Boolean.default_line_indicator_centered);

            //Retrive styles attributes
            using (var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.LinePageIndicator, defStyle, 0))
            {
                _centered = a.GetBoolean(Resource.Styleable.LinePageIndicator_centered, defaultCentered);
                _lineWidth = a.GetDimension(Resource.Styleable.LinePageIndicator_lineWidth, defaultLineWidth);
                _gapWidth = a.GetDimension(Resource.Styleable.LinePageIndicator_gapWidth, defaultGapWidth);
                StrokeWidth = a.GetDimension(Resource.Styleable.LinePageIndicator_strokeWidth, defaultStrokeWidth);
                _paintUnSelected.Color = a.GetColor(Resource.Styleable.LinePageIndicator_unselectedColor,
                                                    defaultUnselectedColor);
                _paintSelected.Color = a.GetColor(Resource.Styleable.LinePageIndicator_selectedColor, defaultSelectedColor);

                var background = a.GetDrawable(Resource.Styleable.LinePageIndicator_android_background);
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

        public Color UnselectedColor
        {
            get => _paintUnSelected.Color;
            set
            {
                _paintUnSelected.Color = value;
                Invalidate();
            }
        }

        public Color SelectedColor
        {
            get => _paintSelected.Color;
            set
            {
                _paintSelected.Color = value;
                Invalidate();
            }
        }

        public float LineWidth
        {
            get => _lineWidth;
            set
            {
                _lineWidth = value;
                Invalidate();
            }
        }

        public float StrokeWidth
        {
            get => _paintSelected.StrokeWidth;
            set
            {
                _paintSelected.StrokeWidth = value;
                _paintUnSelected.StrokeWidth = value;
                Invalidate();
            }
        }

        public float GapWidth
        {
            get => _gapWidth;
            set
            {
                _gapWidth = value;
                Invalidate();
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            var count = ViewPager?.Adapter?.Count ?? 0;
            if (count == 0) return;

            if (_currentPage >= count)
            {
                CurrentItem = count - 1;
                return;
            }

            var lineWidthAndGap = LineWidth + GapWidth;
            var indicatorWidth = (count * lineWidthAndGap) - GapWidth;

            var verticalOffset = PaddingTop + ((Height - PaddingTop - PaddingBottom) / 2.0f);
            float horizontalOffset = PaddingLeft;
            if (_centered)
                horizontalOffset += ((Width - PaddingLeft - PaddingRight) / 2.0f) - (indicatorWidth / 2.0f);

            //Draw stroked circles
            for (var i = 0; i < count; i++)
            {
                var dx1 = horizontalOffset + (i * lineWidthAndGap);
                var dx2 = dx1 + LineWidth;
                canvas.DrawLine(dx1, verticalOffset, dx2, verticalOffset, (i == _currentPage) ? _paintSelected : _paintUnSelected);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (base.OnTouchEvent(e))
                return true;

            if (ViewPager?.Adapter?.Count == 0)
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
                        var count = _viewPager.Adapter?.Count ?? 0;
                        var halfWidth = Width / 2f;
                        var sixthWidth = Width / 6f;

                        if ((_currentPage > 0) && (e.GetX() < halfWidth - sixthWidth))
                        {
                            if (action != MotionEventActions.Cancel)
                                _viewPager.CurrentItem = _currentPage - 1;
                            return true;
                        }
                        if ((_currentPage < count - 1) && (e.GetX() > halfWidth + sixthWidth))
                        {
                            if (action != MotionEventActions.Cancel)
                                _viewPager.CurrentItem = _currentPage + 1;
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

        public int CurrentItem
        {
            get { return _currentPage; }
            set
            {
                if (null == ViewPager)
                    throw new InvalidOperationException("ViewPager has not been bound.");

                _viewPager.CurrentItem = value;
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
            if (null != _listener)
                _listener.OnPageScrollStateChanged(state);

            PageScrollStateChanged?.Invoke(this, new PageScrollStateChangedEventArgs { State = state });
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
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
            _currentPage = position;
            Invalidate();

            if (null != _listener)
                _listener.OnPageSelected(position);

            PageSelected?.Invoke(this, new PageSelectedEventArgs { Position = position });
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            SetMeasuredDimension(MeasureWidth(widthMeasureSpec), MeasureHeight(heightMeasureSpec));
        }

        private int MeasureWidth(int measureSpec)
        {
            float result;
            var specMode = MeasureSpec.GetMode(measureSpec);
            var specSize = MeasureSpec.GetSize(measureSpec);

            if (specMode == MeasureSpecMode.Exactly || null == ViewPager)
                result = specSize;
            else
            {
                //Calculate the width according to the views count
                var count = _viewPager.Adapter?.Count ?? 0;
                result = (PaddingLeft + PaddingRight + (count * _lineWidth) + (count - 1) * _gapWidth);
                if (specMode == MeasureSpecMode.AtMost)
                    result = Math.Min(result, specSize);
            }
            return (int)Math.Ceiling(result);
        }

        private int MeasureHeight(int measureSpec)
        {
            float result;
            var specMode = MeasureSpec.GetMode(measureSpec);
            var specSize = MeasureSpec.GetSize(measureSpec);

            if (specMode == MeasureSpecMode.Exactly || null == ViewPager)
                result = specSize;
            else
            {
                result = _paintSelected.StrokeWidth + PaddingTop + PaddingBottom;
                if (specMode == MeasureSpecMode.AtMost)
                    result = Math.Min(result, specSize);
            }

            return (int)Math.Ceiling(result);
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            var savedState = state as LineSavedState;
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
            var savedState = new LineSavedState(superState)
            {
                CurrentPage = _currentPage
            };
            return savedState;
        }

        public class LineSavedState
            : BaseSavedState
        {
            public int CurrentPage { get; set; }

            public LineSavedState(IParcelable superState)
                : base(superState)
            {
            }

            private LineSavedState(Parcel parcel)
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
            public static LineSavedStateCreator InitializeCreator()
            {
                return new LineSavedStateCreator();
            }

            public class LineSavedStateCreator : Java.Lang.Object, IParcelableCreator
            {
                public Java.Lang.Object CreateFromParcel(Parcel source)
                {
                    return new LineSavedState(source);
                }

                public Java.Lang.Object[] NewArray(int size)
                {
                    return new LineSavedState[size];
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
                _paintUnSelected?.Dispose();
                _paintSelected?.Dispose();

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
