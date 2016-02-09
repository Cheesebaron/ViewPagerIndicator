using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Java.Interop;
using Java.Lang;
using Math = System.Math;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    [Register("dk.ostebaronen.droid.viewpagerindicator.UnderlinePageIndicator")]
    public class UnderlinePageIndicator 
        : View
        , IPageIndicator
    {
        private const int InvalidPointer = -1;
        private const int FadeFrameMs = 30;

        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias);
        private bool _fades;
        private int _fadeBy;
        private int _fadeLength;

        private ViewPager _viewPager;
        private ViewPager.IOnPageChangeListener _listener;
        private int _scrollState;
        private float _positionOffset;

        private readonly int _touchSlop;
        private float _lastMotionX = -1;
        private int _activePointerId = InvalidPointer;
        private bool _isDragging;
        private int _currentPage;

        private readonly Runnable _fadeRunnable;

        public event PageScrollStateChangedEventHandler PageScrollStateChanged;
        public event PageSelectedEventHandler PageSelected;
        public event PageScrolledEventHandler PageScrolled;

        public UnderlinePageIndicator(Context context)
            : this(context, null)
        {
        }

        public UnderlinePageIndicator(Context context, IAttributeSet attrs)
            : this(context, attrs, Resource.Attribute.vpiUnderlinePageIndicatorStyle)
        {
        }

        public UnderlinePageIndicator(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            if (IsInEditMode) return;

            var res = Resources;

            var defaultFades = res.GetBoolean(Resource.Boolean.default_underline_indicator_fades);
            var defaultFadeDelay = res.GetInteger(Resource.Integer.default_underline_indicator_fade_delay);
            var defaultFadeLength = res.GetInteger(Resource.Integer.default_underline_indicator_fade_length);
            var defaultSelectedColor = res.GetColor(Resource.Color.default_underline_indicator_selected_color);

            var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.UnderlinePageIndicator, defStyle, 0);

            Fades = a.GetBoolean(Resource.Styleable.UnderlinePageIndicator_fades, defaultFades);
            SelectedColor = a.GetColor(Resource.Styleable.UnderlinePageIndicator_selectedColor, defaultSelectedColor);
            FadeDelay = a.GetInteger(Resource.Styleable.UnderlinePageIndicator_fadeDelay, defaultFadeDelay);
            FadeLength = a.GetInteger(Resource.Styleable.UnderlinePageIndicator_fadeLength, defaultFadeLength);

            var background = a.GetDrawable(Resource.Styleable.UnderlinePageIndicator_android_background);
            if(null != background)
                Background = background;

            a.Recycle();

            var configuration = ViewConfiguration.Get(context);
            _touchSlop = ViewConfigurationCompat.GetScaledPagingTouchSlop(configuration);

            _fadeRunnable = new Runnable(() =>
            {
                if (!_fades) return;

                var alpha = Math.Max(_paint.Alpha - _fadeBy, 0);
                _paint.Alpha = alpha;
                Invalidate();
                if (alpha > 0)
                    PostDelayed(_fadeRunnable, FadeFrameMs);
            });
        }

        public bool Fades
        {
            get { return _fades; }
            set
            {
                if(value != _fades)
                {
                    _fades = value;
                    if(_fades)
                    {
                        Post(_fadeRunnable);
                    }
                    else
                    {
                        RemoveCallbacks(_fadeRunnable);
                        _paint.Alpha = 0xFF;
                        Invalidate();
                    }
                }
            }
        }

        public int FadeDelay { get; set; }

        public int FadeLength
        {
            get { return _fadeLength; }
            set
            {
                _fadeLength = value;
                _fadeBy = 0xFF / (_fadeLength / FadeFrameMs);
            }
        }

        public Color SelectedColor
        {
            get { return _paint.Color; }
            set
            {
                _paint.Color = value;
                Invalidate();
            }
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

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if(null == _viewPager) return;

            var count = _viewPager.Adapter.Count;
            if (count == 0) return;
            
            if(_currentPage >= count)
            {
                CurrentItem = count - 1;
                return;
            }

            var pageWidth = (Width - PaddingLeft - PaddingRight) / (1f * count);
            var left = PaddingLeft + pageWidth * (_currentPage + _positionOffset);
            var right = left + pageWidth;
            var top = PaddingTop;
            var bottom = Height - PaddingBottom;
            canvas.DrawRect(left, top, right, bottom, _paint);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (base.OnTouchEvent(e))
                return true;

            if (null == _viewPager || _viewPager.Adapter.Count == 0)
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

                        if ((_currentPage > 0) && (e.GetX() > halfWidth - sixthWidth))
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
				_viewPager.ClearOnPageChangeListeners();

            if (null == view.Adapter)
                throw new InvalidOperationException("ViewPager does not have an Adapter instance.");

            _viewPager = view;
			_viewPager.AddOnPageChangeListener(this);
            Invalidate();
            Post(_fadeRunnable);
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

            if (null != PageScrollStateChanged)
                PageScrollStateChanged(this, new PageScrollStateChangedEventArgs { State = state });
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            _currentPage = position;
            _positionOffset = positionOffset;
            if(_fades)
            {
                if (positionOffsetPixels > 0)
                {
                    RemoveCallbacks(_fadeRunnable);
                    _paint.Alpha = 0xFF;
                }
                else if(_scrollState != ViewPager.ScrollStateDragging)
                    PostDelayed(_fadeRunnable, FadeFrameMs);
            }
            Invalidate();

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
            if (_scrollState == ViewPager.ScrollStateIdle)
            {
                _currentPage = position;
                _positionOffset = 0;
                Invalidate();
                _fadeRunnable.Run();
            }

            if (null != _listener)
                _listener.OnPageSelected(position);

            if (null != PageSelected)
                PageSelected(this, new PageSelectedEventArgs { Position = position });
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            var savedState = state as UnderlineSavedState;
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
            var savedState = new UnderlineSavedState(superState)
            {
                CurrentPage = _currentPage
            };
            return savedState;
        }

        public class UnderlineSavedState 
            : BaseSavedState
        {
            public int CurrentPage { get; set; }

            public UnderlineSavedState(IParcelable superState)
                : base(superState) { }

            private UnderlineSavedState(Parcel parcel)
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
            public static UnderlineSavedStateCreator InitializeCreator()
            {
                return new UnderlineSavedStateCreator();
            }

            public class UnderlineSavedStateCreator : Java.Lang.Object, IParcelableCreator
            {
                public Java.Lang.Object CreateFromParcel(Parcel source)
                {
                    return new UnderlineSavedState(source);
                }

                public Java.Lang.Object[] NewArray(int size)
                {
                    return new UnderlineSavedState[size];
                }
            }
        }
    }
}