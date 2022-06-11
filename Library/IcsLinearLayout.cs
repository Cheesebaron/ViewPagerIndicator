using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

namespace DK.Ostebaronen.Droid.ViewPagerIndicator
{
    public class IcsLinearLayout
        : LinearLayout
    {
        private static readonly int[] Ll =
        {
            Android.Resource.Attribute.Divider,
            Android.Resource.Attribute.ShowDividers,
            Android.Resource.Attribute.DividerPadding
        };

        private const int LlDivider = 0;
        private const int LlShowDivider = 1;
        private const int LlDividerPadding = 2;

        private Drawable _divider;
        private int _dividerWidth;
        private int _dividerHeight;
        private readonly int _showDividers;
        private readonly int _dividerPadding;

        public IcsLinearLayout(Context context, int themeAttr)
            : base(context)
        {
            using (var a = context.ObtainStyledAttributes(null, Ll, themeAttr, 0))
            {
                DividerDrawable = a.GetDrawable(LlDivider);
                _dividerPadding = a.GetDimensionPixelSize(LlDividerPadding, 0);
                _showDividers = a.GetInteger(LlShowDivider, (int)ShowDividers.None);

                a.Recycle();
            }
        }

        public new Drawable DividerDrawable
        {
            get { return _divider; }
            set
            {
                if (value == _divider) return;

                _divider = value;
                if (_divider != null)
                {
                    _dividerWidth = _divider.IntrinsicWidth;
                    _dividerHeight = _divider.IntrinsicHeight;
                }
                else
                {
                    _dividerWidth = 0;
                    _dividerHeight = 0;
                }
                SetWillNotDraw(_divider == null);
                RequestLayout();
            }
        }

        protected override void MeasureChildWithMargins(View child, int parentWidthMeasureSpec, int widthUsed,
            int parentHeightMeasureSpec, int heightUsed)
        {
            var index = IndexOfChild(child);
            var lparams = (LayoutParams)child.LayoutParameters;

            if (HasDividerBeforeChildAt(index))
            {
                if (Orientation == Orientation.Vertical)
                    lparams.TopMargin = _dividerHeight;
                else
                    lparams.LeftMargin = _dividerWidth;
            }

            if (index == ChildCount - 1)
            {
                if (HasDividerBeforeChildAt(ChildCount))
                {
                    if (Orientation == Orientation.Vertical)
                        lparams.BottomMargin = _dividerHeight;
                    else
                        lparams.RightMargin = _dividerWidth;
                }
            }

            base.MeasureChildWithMargins(child, parentWidthMeasureSpec, widthUsed, parentHeightMeasureSpec, heightUsed);
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (_divider != null)
            {
                if (Orientation == Orientation.Vertical)
                    DrawDividersVertical(canvas);
                else
                    DrawDividersHorizontal(canvas);
            }

            base.OnDraw(canvas);
        }

        private void DrawDividersVertical(Canvas canvas)
        {
            for (var i = 0; i < ChildCount; i++)
            {
                var child = GetChildAt(i);

                if (child != null && child.Visibility != ViewStates.Gone)
                {
                    if (HasDividerBeforeChildAt(i))
                    {
                        var lp = (LayoutParams)child.LayoutParameters;
                        var top = child.Top - lp.TopMargin;
                        DrawHorizontalDivider(canvas, top);
                    }
                }
            }

            if (HasDividerBeforeChildAt(ChildCount))
            {
                var child = GetChildAt(ChildCount - 1);
                int bottom;
                if (child == null)
                    bottom = Height - PaddingBottom - _dividerHeight;
                else
                    bottom = child.Bottom;
                DrawHorizontalDivider(canvas, bottom);
            }
        }

        private void DrawDividersHorizontal(Canvas canvas)
        {
            for (var i = 0; i < ChildCount; i++)
            {
                var child = GetChildAt(i);

                if (child != null && child.Visibility != ViewStates.Gone)
                {
                    if (HasDividerBeforeChildAt(i))
                    {
                        var lp = (LayoutParams)child.LayoutParameters;
                        var left = child.Left - lp.LeftMargin;
                        DrawVerticalDivider(canvas, left);
                    }
                }
            }

            if (HasDividerBeforeChildAt(ChildCount))
            {
                var child = GetChildAt(ChildCount - 1);
                int right;
                if (child == null)
                    right = Width - PaddingRight - _dividerWidth;
                else
                    right = child.Right;
                DrawVerticalDivider(canvas, right);
            }
        }

        private void DrawHorizontalDivider(Canvas canvas, int top)
        {
            _divider.SetBounds(PaddingLeft + _dividerPadding, top, Width - PaddingRight - _dividerPadding,
                               top + _dividerHeight);
            _divider.Draw(canvas);
        }

        private void DrawVerticalDivider(Canvas canvas, int left)
        {
            _divider.SetBounds(left, PaddingTop + _dividerPadding,
                left + _dividerWidth, Height - PaddingBottom - _dividerPadding);
            _divider.Draw(canvas);
        }

        private bool HasDividerBeforeChildAt(int childIndex)
        {
            if (childIndex == 0 || childIndex == ChildCount)
                return false;

            if ((_showDividers & (int)ShowDividers.Middle) != 0)
            {
                var hasVisibleViewBefore = false;
                for (var i = childIndex; i >= 0; i--)
                    if (GetChildAt(i).Visibility != ViewStates.Gone)
                    {
                        hasVisibleViewBefore = true;
                        break;
                    }
                return hasVisibleViewBefore;
            }
            return false;
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _divider?.Dispose();
            }

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
}
