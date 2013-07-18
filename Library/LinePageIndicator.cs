using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Java.Interop;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    public class LinePageIndicator : View, IPageIndicator
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
            if(IsInEditMode) return;

            var res = Resources;

            //Load defaults from resources
            var defaultSelectedColor = res.GetColor(Resource.Color.default_line_indicator_selected_color);
            var defaultUnselectedColor = res.GetColor(Resource.Color.default_line_indicator_unselected_color);
            var defaultLineWidth = res.GetDimension(Resource.Dimension.default_line_indicator_line_width);
            var defaultGapWidth = res.GetDimension(Resource.Dimension.default_line_indicator_gap_width);
            var defaultStrokeWidth = res.GetDimension(Resource.Dimension.default_line_indicator_stroke_width);
            var defaultCentered = res.GetBoolean(Resource.Boolean.default_line_indicator_centered);

            //Retrive styles attributes
            var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.LinePageIndicator, defStyle, 0);
            _centered = a.GetBoolean(Resource.Styleable.LinePageIndicator_centered, defaultCentered);
            _lineWidth = a.GetDimension(Resource.Styleable.LinePageIndicator_lineWidth, defaultLineWidth);
            _gapWidth = a.GetDimension(Resource.Styleable.LinePageIndicator_gapWidth, defaultGapWidth);
            StrokeWidth = a.GetDimension(Resource.Styleable.LinePageIndicator_strokeWidth, defaultStrokeWidth);
            _paintUnSelected.Color = a.GetColor(Resource.Styleable.LinePageIndicator_unselectedColor,
                                                defaultUnselectedColor);
            _paintSelected.Color = a.GetColor(Resource.Styleable.LinePageIndicator_selectedColor, defaultSelectedColor);

            var background = a.GetDrawable(Resource.Styleable.LinePageIndicator_android_background);
            if(null != background)
                SetBackgroundDrawable(background);

            a.Recycle();

            var configuration = ViewConfiguration.Get(context);
            _touchSlop = ViewConfigurationCompat.GetScaledPagingTouchSlop(configuration);
        }

        public bool Centered
        {
            get { return _centered; }
            set
            {
                _centered = value;
                Invalidate();
            }
        }

        public Color UnselectedColor
        {
            get { return _paintUnSelected.Color; }
            set
            {
                _paintUnSelected.Color = value;
                Invalidate();
            }
        }

        public Color SelectedColor
        {
            get { return _paintSelected.Color; }
            set
            {
                _paintSelected.Color = value;
                Invalidate();
            }
        }

        public float LineWidth
        {
            get { return _lineWidth; } 
            set
            {
                _lineWidth = value;
                Invalidate();
            }
        }

        public float StrokeWidth
        {
            get { return _paintSelected.StrokeWidth; }
            set
            {
                _paintSelected.StrokeWidth = value;
                _paintUnSelected.StrokeWidth = value;
                Invalidate();
            }
        }

        public float GapWidth
        {
            get { return _gapWidth; }
            set
            {
                _gapWidth = value;
                Invalidate();
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if(null == _viewPager) return;

            var count = _viewPager.Adapter.Count;
            if(count == 0) return;

            if(_currentPage >= count)
            {
                CurrentItem = count - 1;
                return;
            }

            var lineWidthAndGap = LineWidth + GapWidth;
            var indicatorWidth = (count * lineWidthAndGap) - GapWidth;

            var verticalOffset = PaddingTop + ((Height - PaddingTop - PaddingBottom) / 2.0f);
            float horizontalOffset = PaddingLeft;
            if(_centered)
                horizontalOffset += ((Width - PaddingLeft - PaddingRight) / 2.0f) - (indicatorWidth / 2.0f);

            //Draw stroked circles
            for(var i = 0; i < count; i++)
            {
                var dx1 = horizontalOffset + (i * lineWidthAndGap);
                var dx2 = dx1 + LineWidth;
                canvas.DrawLine(dx1, verticalOffset, dx2, verticalOffset, (i == _currentPage) ? _paintSelected : _paintUnSelected);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if(base.OnTouchEvent(e))
                return true;

            if(null == _viewPager || _viewPager.Adapter.Count == 0)
                return false;

            var action = (int)e.Action & MotionEventCompat.ActionMask;
            switch (action)
            {
                case (int)MotionEventActions.Down:
                    _activePointerId = MotionEventCompat.GetPointerId(e, 0);
                    _lastMotionX = e.GetX();
                    break;
                case (int)MotionEventActions.Move:
                    var activePointerIndex = MotionEventCompat.FindPointerIndex(e, _activePointerId);
                    var x = MotionEventCompat.GetX(e, activePointerIndex);
                    var deltaX = x - _lastMotionX;

                    if (!_isDragging)
                        if (Math.Abs(deltaX) > _touchSlop)
                            _isDragging = true;

                    if (_isDragging)
                    {
                        _lastMotionX = x;
                        if (_viewPager.IsFakeDragging || _viewPager.BeginFakeDrag())
                            _viewPager.FakeDragBy(deltaX);
                    }

                    break;

                case (int)MotionEventActions.Cancel:
                case (int)MotionEventActions.Up:
                    if (!_isDragging)
                    {
                        var count = _viewPager.Adapter.Count;
                        var halfWidth = Width / 2f;
                        var sixthWidth = Width / 6f;

                        if ((_currentPage > 0) && (e.GetX() < halfWidth - sixthWidth))
                        {
                            if (action != (int)MotionEventActions.Cancel)
                                _viewPager.CurrentItem = _currentPage - 1;
                            return true;
                        }
                        if ((_currentPage < count - 1) && (e.GetX() > halfWidth + sixthWidth))
                        {
                            if (action != (int)MotionEventActions.Cancel)
                                _viewPager.CurrentItem = _currentPage + 1;
                            return true;
                        }
                    }

                    _isDragging = false;
                    _activePointerId = InvalidPointer;
                    if (_viewPager.IsFakeDragging) _viewPager.EndFakeDrag();
                    break;

                case (int)MotionEventActions.PointerDown:
                {
                    var pointerIndex = MotionEventCompat.GetActionIndex(e);
                    _lastMotionX = MotionEventCompat.GetX(e, pointerIndex);
                    _activePointerId = MotionEventCompat.GetPointerId(e, pointerIndex);
                    break;
                }

                case (int)MotionEventActions.PointerUp:
                {
                    var pointerIndex = MotionEventCompat.GetActionIndex(e);
                    var pointerId = MotionEventCompat.GetPointerId(e, pointerIndex);
                    if (pointerId == _activePointerId)
                    {
                        var newPointerIndex = pointerIndex == 0 ? 1 : 0;
                        _activePointerId = MotionEventCompat.GetPointerId(e, newPointerIndex);
                    }
                    _lastMotionX = MotionEventCompat.GetX(e, MotionEventCompat.FindPointerIndex(e, _activePointerId));
                    break;
                }
            }

            return true;
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
                if (null == _viewPager)
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

            if (null != PageScrollStateChanged)
                PageScrollStateChanged(this, new PageScrollStateChangedEventArgs { State = state });
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            if (null != _listener)
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
            _currentPage = position;
            Invalidate();

            if (null != _listener)
                _listener.OnPageSelected(position);

            if (null != PageSelected)
                PageSelected(this, new PageSelectedEventArgs { Position = position });
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

            if (specMode == MeasureSpecMode.Exactly || null == _viewPager)
                result = specSize;
            else
            {
                //Calculate the width according to the views count
                var count = _viewPager.Adapter.Count;
                result = (PaddingLeft + PaddingRight + (count * _lineWidth) + (count - 1) * _gapWidth);
                if (specMode == MeasureSpecMode.AtMost)
                    result = Math.Min(result, specSize);
            }
            return (int)FloatMath.Ceil(result);
        }

        private int MeasureHeight(int measureSpec)
        {
            float result;
            var specMode = MeasureSpec.GetMode(measureSpec);
            var specSize = MeasureSpec.GetSize(measureSpec);

            if (specMode == MeasureSpecMode.Exactly || null == _viewPager)
                result = specSize;
            else
            {
                result = _paintSelected.StrokeWidth + PaddingTop + PaddingBottom;
                if (specMode == MeasureSpecMode.AtMost)
                    result = Math.Min(result, specSize);
            }
            return (int)FloatMath.Ceil(result);
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            try
            {
                var savedState = (LineSavedState)state;
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
            var savedState = new LineSavedState(superState)
            {
                CurrentPage = _currentPage
            };
            return savedState;
        }

        public class LineSavedState : BaseSavedState
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
            static SavedStateCreator InitializeCreator()
            {
                return new SavedStateCreator();
            }

            class SavedStateCreator : Java.Lang.Object, IParcelableCreator
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
    }
}