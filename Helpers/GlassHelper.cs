/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace RoliSoft.TVShowTracker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using Interop;

    internal class GlassHelper
    {
        // Test Notes:
        // Things to manually verify when making changes to this class.
        // * Do modified windows look correct in non-composited themes?
        // * Does changing the theme back and forth leave the window in a visually ugly state?
        //     * Does it matter which theme was used first?
        // * Does glass extension work properly in high-dpi?
        // * Which of SetWindowThemeAttribute and ExtendGlassFrame are set first shouldn't matter.
        //   The hooks injected by one should not block the hooks of the other.
        // * Do captions and icons always show up when composition is disabled?
        //
        // There are not automated unit tests for this class ( Boo!!! :( )
        // Be careful not to break things...

        private static readonly Dictionary<IntPtr, HwndSourceHook> _extendedWindows = new Dictionary<IntPtr, HwndSourceHook>();

        // TODO:
        // Verify that this really is sufficient.  There are DWMWINDOWATTRIBUTEs as well, so this may
        // be able to be turned off on a per-HWND basis, but I never see comments about that online...
        public static bool IsCompositionEnabled
        {
            get
            {
                if (Environment.OSVersion.Version.Major < 6)
                {
                    return false;
                }

                return NativeMethods.DwmIsCompositionEnabled();
            }
        }

        public static bool ExtendGlassFrameComplete(Window window)
        {
            return ExtendGlassFrame(window, new Thickness(-1));
        }

        /// <summary>
        /// Extends the glass frame of a window.  Only works on operating systems that support composition.
        /// </summary>
        /// <param name="window">The window to modify.</param>
        /// <param name="margin">The margins of the new frame.</param>
        /// <returns>Whether the frame was successfully extended.</returns>
        /// <remarks>
        /// This function adds hooks to the Window to respond to changes to whether composition is enabled.
        /// </remarks>
        public static bool ExtendGlassFrame(Window window, Thickness margin)
        {
            window.VerifyAccess();

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (_extendedWindows.ContainsKey(hwnd))
            {
                // The hook into the HWND's WndProc has the original margin cached.
                // Don't want to support dynamically adjusting that unless there's a need.
                throw new InvalidOperationException("Multiple calls to this function for the same Window are not supported.");
            }

            return _ExtendGlassFrameInternal(window, margin);
        }

        private static bool _ExtendGlassFrameInternal(Window window, Thickness margin)
        {
            // Expect that this might be called on OSes other than Vista.
            if (Environment.OSVersion.Version.Major < 6)
            {
                // Not an error.  Just not on Vista so we're not going to get glass.
                return false;
            }

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (IntPtr.Zero == hwnd)
            {
                throw new InvalidOperationException("Window must be shown before extending glass.");
            }

            HwndSource hwndSource = HwndSource.FromHwnd(hwnd);

            bool isGlassEnabled = NativeMethods.DwmIsCompositionEnabled();

            if (!isGlassEnabled)
            {
                window.Background = SystemColors.WindowBrush;
                hwndSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
            }
            else
            {
                // Apply the transparent background to both the Window and the HWND
                window.Background = Brushes.Transparent;
                hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;

                // Thickness is going to be DIPs, need to convert to system coordinates.
                Point deviceTopLeft = DpiHelper.LogicalPixelsToDevice(new Point(margin.Left, margin.Top));
                Point deviceBottomRight = DpiHelper.LogicalPixelsToDevice(new Point(margin.Right, margin.Bottom));

                var dwmMargin = new MARGINS
                {
                    // err on the side of pushing in glass an extra pixel.
                    cxLeftWidth = (int)Math.Ceiling(deviceTopLeft.X),
                    cxRightWidth = (int)Math.Ceiling(deviceBottomRight.X),
                    cyTopHeight = (int)Math.Ceiling(deviceTopLeft.Y),
                    cyBottomHeight = (int)Math.Ceiling(deviceBottomRight.Y),
                };

                NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref dwmMargin);
            }

            // Even if glass isn't currently enabled, add the hook so we can appropriately respond
            // if that changes.

            bool addHook = !_extendedWindows.ContainsKey(hwnd);

            if (addHook)
            {
                HwndSourceHook hook = delegate(IntPtr innerHwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    if (WM.DWMCOMPOSITIONCHANGED == (WM)msg)
                    {
                        _ExtendGlassFrameInternal(window, margin);
                        handled = false;
                    }
                    return IntPtr.Zero;
                };

                _extendedWindows.Add(hwnd, hook);
                hwndSource.AddHook(hook);
                window.Closing += _OnExtendedWindowClosing;
            }

            return isGlassEnabled;
        }

        /// <summary>
        /// Handler for the Closing event on a Window with an extended glass frame.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// When a Window with an extended glass frame closes, removes any local references to it.
        /// </remarks>
        // BUGBUG: Doesn't handle if the Closing gets canceled.
        static void _OnExtendedWindowClosing(object sender, CancelEventArgs e)
        {
            var window = sender as Window;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            // We use the Closing rather than the Closed event to ensure that we can get this value.

            HwndSource hwndSource = HwndSource.FromHwnd(hwnd);

            hwndSource.RemoveHook(_extendedWindows[hwnd]);
            _extendedWindows.Remove(hwnd);

            window.Closing -= _OnExtendedWindowClosing;
        }

        private static readonly Dictionary<IntPtr, HwndSourceHook> _attributedWindows = new Dictionary<IntPtr, HwndSourceHook>();

        public static bool SetWindowThemeAttribute(Window window, bool showCaption, bool showIcon)
        {
            window.VerifyAccess();

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (_attributedWindows.ContainsKey(hwnd))
            {
                // The hook into the HWND's WndProc has the original settings cached.
                // Don't want to support dynamically adjusting that unless there's a need.
                throw new InvalidOperationException("Multiple calls to this function for the same Window are not supported.");
            }

            return _SetWindowThemeAttribute(window, showCaption, showIcon);
        }

        private static bool _SetWindowThemeAttribute(Window window, bool showCaption, bool showIcon)
        {
            bool isGlassEnabled;

            // This only is expected to work if Aero glass is enabled.
            try
            {
                isGlassEnabled = NativeMethods.DwmIsCompositionEnabled();
            }
            catch (DllNotFoundException)
            {
                // Not an error.  Just not on Vista so we're not going to get glass.
                return false;
            }

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (IntPtr.Zero == hwnd)
            {
                throw new InvalidOperationException("Window must be shown before we can modify attributes.");
            }

            var options = new WTA_OPTIONS
            {
                dwMask = (WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON)
            };
            if (isGlassEnabled)
            {
                if (!showCaption)
                {
                    options.dwFlags |= WTNCA.NODRAWCAPTION;
                }
                if (!showIcon)
                {
                    options.dwFlags |= WTNCA.NODRAWICON;
                }
            }

            NativeMethods.SetWindowThemeAttribute(hwnd, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref options, WTA_OPTIONS.Size);

            bool addHook = !_attributedWindows.ContainsKey(hwnd);

            if (addHook)
            {
                HwndSourceHook hook = delegate(IntPtr unusedHwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    if (WM.DWMCOMPOSITIONCHANGED == (WM)msg)
                    {
                        _SetWindowThemeAttribute(window, showCaption, showIcon);
                        handled = false;
                    }
                    return IntPtr.Zero;
                };

                _attributedWindows.Add(hwnd, hook);
                HwndSource.FromHwnd(hwnd).AddHook(hook);
                window.Closing += _OnAttributedWindowClosing;
            }

            return isGlassEnabled;
        }

        static void _OnAttributedWindowClosing(object sender, CancelEventArgs e)
        {
            var window = sender as Window;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            // We use the Closing rather than the Closed event to ensure that we can get this value.

            HwndSource hwndSource = HwndSource.FromHwnd(hwnd);

            hwndSource.RemoveHook(_attributedWindows[hwnd]);
            _attributedWindows.Remove(hwnd);

            window.Closing -= _OnExtendedWindowClosing;
        }
    }

    internal static class DpiHelper
    {
        private static Matrix _transformToDevice;
        private static Matrix _transformToDip;

        static DpiHelper()
        {
            // Getting the Desktop, so we shouldn't have to release this DC.
            IntPtr desktop = NativeMethods.GetDC(IntPtr.Zero);
            if (IntPtr.Zero == desktop)
            {
                HRESULT hr = Win32Error.GetLastError();
                hr.ThrowIfFailed();
            }

            // Can get these in the static constructor.  They shouldn't vary window to window,
            // and changing the system DPI requires a restart.
            int pixelsPerInchX = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSX);
            int pixelsPerInchY = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSY);

            _transformToDip = Matrix.Identity;
            _transformToDip.Scale(96d / (double)pixelsPerInchX, 96d / (double)pixelsPerInchY);
            _transformToDevice = Matrix.Identity;
            _transformToDevice.Scale((double)pixelsPerInchX / 96d, (double)pixelsPerInchY / 96d);
        }

        /// <summary>
        /// Convert a point in device independent pixels (1/96") to a point in the system coordinates.
        /// </summary>
        /// <param name="logicalPoint">A point in the logical coordinate system.</param>
        /// <returns>Returns the parameter converted to the system's coordinates.</returns>
        public static Point LogicalPixelsToDevice(Point logicalPoint)
        {
            return _transformToDevice.Transform(logicalPoint);
        }

        /// <summary>
        /// Convert a point in system coordinates to a point in device independent pixels (1/96").
        /// </summary>
        /// <param name="logicalPoint">A point in the physical coordinate system.</param>
        /// <returns>Returns the parameter converted to the device independent coordinate system.</returns>
        public static Point DevicePixelsToLogical(Point devicePoint)
        {
            return _transformToDip.Transform(devicePoint);
        }

        public static Rect LogicalRectToDevice(Rect logicalRectangle)
        {
            Point topLeft = LogicalPixelsToDevice(new Point(logicalRectangle.Left, logicalRectangle.Top));
            Point bottomRight = LogicalPixelsToDevice(new Point(logicalRectangle.Right, logicalRectangle.Bottom));

            return new Rect(topLeft, bottomRight);
        }

        public static Rect DeviceRectToLogical(Rect deviceRectangle)
        {
            Point topLeft = DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top));
            Point bottomRight = DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom));

            return new Rect(topLeft, bottomRight);
        }
    }
}
