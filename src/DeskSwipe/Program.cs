using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DeskSwipe;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);

        if (!Options.TryParse(args, out var options, out var parseError))
        {
            MessageBox.Show(
                parseError,
                "DeskSwipe",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return 64;
        }

        var parsedOptions = options!;

        try
        {
            ApplicationConfiguration.Initialize();

            using var desktop =
                VirtualDesktopAccessor.Load(parsedOptions.DllPath);

            if (parsedOptions.Resident)
            {
                using var host = new ResidentHost(
                    desktop,
                    parsedOptions.DurationMs,
                    parsedOptions.CaptureDelayMs);

                Application.Run(host);

                return 0;
            }

            if (parsedOptions.Direction is null)
                return 64;

            var runner = new TransitionRunner(
                desktop,
                parsedOptions.DurationMs,
                parsedOptions.CaptureDelayMs);

            runner.Run(parsedOptions.Direction.Value);

            return 0;
        }
        catch (Exception ex)
        {
            if (!parsedOptions.Silent)
            {
                MessageBox.Show(
                    ex.Message,
                    "DeskSwipe",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return 1;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(
        IntPtr dpiContext);

    private static readonly IntPtr
        DpiAwarenessContextPerMonitorAwareV2 = new(-4);
}

internal enum SwipeDirection
{
    Left,
    Right
}

internal sealed class ResidentHost : Form
{
    public const int TransitionMessage = 0x802A;

    private readonly ContinuousTransitionEngine _engine;
    private readonly EdgeBounce _edgeBounce;

    public ResidentHost(
        VirtualDesktopAccessor desktop,
        int durationMs,
        int captureDelayMs)
    {
        Text = "DeskSwipeIPC";

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        Location = new Point(-32000, -32000);
        Size = new Size(1, 1);

        Opacity = 0;

        _engine = new ContinuousTransitionEngine(
            this,
            desktop,
            captureDelayMs);

        _edgeBounce = new EdgeBounce();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var cp = base.CreateParams;

            cp.ExStyle |=
                WS_EX_TOOLWINDOW |
                WS_EX_NOACTIVATE;

            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == TransitionMessage)
        {
            switch (m.WParam.ToInt32())
            {
                case 1:
                    _engine.Command(SwipeDirection.Left);
                    break;

                case 2:
                    _engine.Command(SwipeDirection.Right);
                    break;

                case 3:
                    _edgeBounce.Play(SwipeDirection.Left);
                    break;

                case 4:
                    _edgeBounce.Play(SwipeDirection.Right);
                    break;
            }

            return;
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _engine.Dispose();

        base.Dispose(disposing);
    }
}

internal sealed class EdgeBounce
{
    private bool _running;

    public void Play(SwipeDirection direction)
    {
        if (_running)
            return;

        _running = true;

        try
        {
            var bounds = SystemInformation.VirtualScreen;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            using var screenshot =
                ScreenCapture.Capture(bounds);

            using var form =
                new EdgeBounceForm(
                    bounds,
                    screenshot,
                    direction);

            form.Show();
            form.ForceTopMost();
            form.Animate();
        }
        finally
        {
            _running = false;
        }
    }
}

internal sealed class EdgeBounceForm : Form
{
    private readonly Rectangle _bounds;
    private readonly Bitmap _frame;
    private readonly SwipeDirection _direction;

    private double _offset;

    public EdgeBounceForm(
        Rectangle bounds,
        Bitmap frame,
        SwipeDirection direction)
    {
        _bounds = bounds;
        _frame = (Bitmap)frame.Clone();
        _direction = direction;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        Bounds = bounds;
        TopMost = true;
        BackColor = Color.Black;
        DoubleBuffered = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_TOPMOST = 0x00000008;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var cp = base.CreateParams;

            cp.ExStyle |=
                WS_EX_TOOLWINDOW |
                WS_EX_TOPMOST |
                WS_EX_NOACTIVATE;

            return cp;
        }
    }

    public void ForceTopMost()
    {
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            _bounds.Left,
            _bounds.Top,
            _bounds.Width,
            _bounds.Height,
            NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW);
    }

    public void Animate()
    {
        var clock = Stopwatch.StartNew();

        const double outwardMs = 90.0;
        const double returnMs = 235.0;

        var maxDistance =
            _bounds.Width * 0.072;

        var directionSign =
            _direction == SwipeDirection.Left
                ? -1.0
                : 1.0;

        while (true)
        {
            var elapsed =
                clock.Elapsed.TotalMilliseconds;

            if (elapsed <= outwardMs)
            {
                var t =
                    elapsed / outwardMs;

                var eased =
                    1.0 -
                    Math.Pow(1.0 - t, 3);

                _offset =
                    directionSign *
                    maxDistance *
                    eased;
            }
            else
            {
                var t =
                    Math.Min(
                        1.0,
                        (elapsed - outwardMs) /
                        returnMs);

                // EaseOutBack gives a subtle rubber-band spring
                // around the resting position.
                const double c1 = 1.05;
                const double c3 = c1 + 1.0;

                var eased =
                    1.0 +
                    c3 * Math.Pow(t - 1.0, 3) +
                    c1 * Math.Pow(t - 1.0, 2);

                _offset =
                    directionSign *
                    maxDistance *
                    (1.0 - eased);

                if (t >= 1.0)
                    break;
            }

            Invalidate();
            Update();

            Thread.Sleep(5);
            Application.DoEvents();
        }

        _offset = 0;

        Invalidate();
        Update();

        Close();
    }

    protected override void OnPaint(
        PaintEventArgs e)
    {
        e.Graphics.CompositingMode =
            CompositingMode.SourceCopy;

        e.Graphics.CompositingQuality =
            CompositingQuality.HighSpeed;

        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.Half;

        e.Graphics.Clear(Color.Black);

        e.Graphics.DrawImageUnscaled(
            _frame,
            (int)Math.Round(_offset),
            0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _frame.Dispose();

        base.Dispose(disposing);
    }
}
internal sealed class ContinuousTransitionEngine : IDisposable
{
    private const double SpringStrength = 420.0;
    private const double SpringDamping = 41.0;
    private const double GestureImpulse = 4.5;

    private readonly Form _owner;
    private readonly VirtualDesktopAccessor _desktop;
    private readonly int _captureDelayMs;

    private readonly Dictionary<int, Bitmap> _frames = new();

    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = new();

    private ContinuousOverlayForm? _overlay;

    private Rectangle _bounds;

    private int _desktopCount;
    private int _baseDesktop;

    private double _position;
    private double _target;
    private double _velocity;

    private double _lastTime;

    private bool _active;
    private bool _pinned;

    private long _captureGeneration;

    public ContinuousTransitionEngine(
        Form owner,
        VirtualDesktopAccessor desktop,
        int captureDelayMs)
    {
        _owner = owner;
        _desktop = desktop;
        _captureDelayMs = captureDelayMs;

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 7
        };

        _timer.Tick += (_, _) => Tick();
    }

    public void Command(SwipeDirection direction)
    {
        if (!_active)
        {
            if (!BeginSession())
                return;
        }

        var delta =
            direction == SwipeDirection.Left
                ? 1.0
                : -1.0;

        _target += delta;

        // Gives the transition an immediate kick instead of
        // waiting for the spring to build velocity.
        _velocity += delta * GestureImpulse;

        var targetStep =
            (int)Math.Round(_target);

        var targetDesktop =
            DesktopForStep(targetStep);

        // Switch Windows underneath the pinned overlay immediately.
        _desktop.GoToDesktopNumber(targetDesktop);

        ScheduleFocusRepair(targetDesktop);
        ScheduleCapture(targetDesktop);

        if (!_timer.Enabled)
        {
            _clock.Restart();
            _lastTime = 0;
            _timer.Start();
        }
    }

    private bool BeginSession()
    {
        _desktopCount =
            _desktop.GetDesktopCount();

        _baseDesktop =
            _desktop.GetCurrentDesktopNumber();

        if (_desktopCount <= 1 ||
            _baseDesktop < 0)
        {
            return false;
        }

        _bounds =
            SystemInformation.VirtualScreen;

        if (_bounds.Width <= 0 ||
            _bounds.Height <= 0)
        {
            return false;
        }

        ClearFrames();

        ReplaceFrame(
            _baseDesktop,
            ScreenCapture.Capture(_bounds));

        _position = 0;
        _target = 0;
        _velocity = 0;

        if (_overlay is null ||
            _overlay.IsDisposed)
        {
            _overlay =
                new ContinuousOverlayForm(
                    _bounds,
                    GetFrame);
        }

        _overlay.SetBounds(
            _bounds.Left,
            _bounds.Top,
            _bounds.Width,
            _bounds.Height);

        _overlay.SetScene(
            _position,
            _baseDesktop,
            _desktopCount);

        _overlay.ShowWithoutStealingFocus();
        _overlay.ForceTopMost();

        var hwnd = _overlay.Handle;

        NativeMethods.SetWindowDisplayAffinity(
            hwnd,
            NativeMethods.WDA_EXCLUDEFROMCAPTURE);

        if (!_pinned)
        {
            var result =
                _desktop.PinWindow(hwnd);

            if (result < 0)
            {
                _overlay.Hide();
                return false;
            }

            _pinned = true;
        }

        _active = true;

        return true;
    }

    private void Tick()
    {
        if (!_active ||
            _overlay is null ||
            _overlay.IsDisposed)
        {
            _timer.Stop();
            return;
        }

        var now =
            _clock.Elapsed.TotalSeconds;

        var dt =
            now - _lastTime;

        _lastTime = now;

        // Avoid giant physics steps after system stalls.
        dt = Math.Clamp(dt, 0.001, 0.030);

        var displacement =
            _target - _position;

        var acceleration =
            SpringStrength * displacement -
            SpringDamping * _velocity;

        _velocity +=
            acceleration * dt;

        _position +=
            _velocity * dt;

        _overlay.SetScene(
            _position,
            _baseDesktop,
            _desktopCount);

        _overlay.Invalidate();

        if (Math.Abs(_target - _position) < 0.002 &&
            Math.Abs(_velocity) < 0.03)
        {
            _position = _target;
            _velocity = 0;

            _overlay.SetScene(
                _position,
                _baseDesktop,
                _desktopCount);

            _overlay.Invalidate();
            _overlay.Update();

            EndSession();
        }
    }

    private async void ScheduleFocusRepair(
        int desktopNumber)
    {
        await Task.Delay(20);

        if (_owner.IsDisposed ||
            !_owner.IsHandleCreated)
        {
            return;
        }

        try
        {
            _owner.BeginInvoke((Action)(() =>
            {
                if (_desktop.GetCurrentDesktopNumber() !=
                    desktopNumber)
                {
                    return;
                }

                FocusRepair.ActivateWindowOnDesktop(
                    _desktop,
                    desktopNumber);
            }));
        }
        catch
        {
            // The resident host may be shutting down.
        }
    }
    private async void ScheduleCapture(
        int desktopNumber)
    {
        var generation =
            ++_captureGeneration;

        if (_captureDelayMs > 0)
            await Task.Delay(_captureDelayMs);

        if (_owner.IsDisposed ||
            !_owner.IsHandleCreated)
        {
            return;
        }

        try
        {
            _owner.BeginInvoke((Action)(() =>
            {
                if (!_active ||
                    generation != _captureGeneration)
                {
                    return;
                }

                // If another gesture already moved Windows again,
                // don't capture the wrong workspace into this slot.
                if (_desktop.GetCurrentDesktopNumber() !=
                    desktopNumber)
                {
                    return;
                }

                var bitmap =
                    ScreenCapture.Capture(_bounds);

                ReplaceFrame(
                    desktopNumber,
                    bitmap);

                _overlay?.Invalidate();
            }));
        }
        catch
        {
            // Host may be shutting down.
        }
    }

    private int DesktopForStep(int step)
    {
        var value =
            (_baseDesktop + step) %
            _desktopCount;

        if (value < 0)
            value += _desktopCount;

        return value;
    }

    private Bitmap? GetFrame(
        int desktopNumber)
    {
        if (_frames.TryGetValue(
            desktopNumber,
            out var exact))
        {
            return exact;
        }

        // Until the new desktop capture arrives, use an existing
        // frame rather than showing black or flashing Windows.
        foreach (var frame in _frames.Values)
            return frame;

        return null;
    }

    private void ReplaceFrame(
        int desktopNumber,
        Bitmap bitmap)
    {
        if (_frames.TryGetValue(
            desktopNumber,
            out var old))
        {
            old.Dispose();
        }

        _frames[desktopNumber] =
            bitmap;
    }

    private void EndSession()
    {
        _timer.Stop();

        if (_overlay is not null &&
            !_overlay.IsDisposed)
        {
            _overlay.Hide();

            if (_pinned)
            {
                _desktop.UnPinWindow(
                    _overlay.Handle);

                _pinned = false;
            }
        }

        _active = false;

        ClearFrames();
    }

    private void ClearFrames()
    {
        foreach (var frame in _frames.Values)
            frame.Dispose();

        _frames.Clear();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();

        if (_overlay is not null &&
            !_overlay.IsDisposed)
        {
            if (_pinned)
            {
                _desktop.UnPinWindow(
                    _overlay.Handle);
            }

            _overlay.Dispose();
        }

        ClearFrames();
    }
}

internal sealed class ContinuousOverlayForm : Form
{
    private readonly Func<int, Bitmap?> _frameProvider;

    private Rectangle _virtualBounds;

    private double _position;

    private int _baseDesktop;
    private int _desktopCount;

    public ContinuousOverlayForm(
        Rectangle bounds,
        Func<int, Bitmap?> frameProvider)
    {
        _virtualBounds = bounds;
        _frameProvider = frameProvider;

        FormBorderStyle =
            FormBorderStyle.None;

        ShowInTaskbar = false;

        StartPosition =
            FormStartPosition.Manual;

        Bounds = bounds;

        TopMost = true;
        BackColor = Color.Black;

        DoubleBuffered = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    protected override bool
        ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW =
                0x00000080;

            const int WS_EX_TOPMOST =
                0x00000008;

            const int WS_EX_NOACTIVATE =
                0x08000000;

            var cp = base.CreateParams;

            cp.ExStyle |=
                WS_EX_TOOLWINDOW |
                WS_EX_TOPMOST |
                WS_EX_NOACTIVATE;

            return cp;
        }
    }

    public void SetScene(
        double position,
        int baseDesktop,
        int desktopCount)
    {
        _position = position;
        _baseDesktop = baseDesktop;
        _desktopCount = desktopCount;
    }

    public void ShowWithoutStealingFocus()
    {
        NativeMethods.ShowWindow(
            Handle,
            NativeMethods.SW_SHOWNOACTIVATE);
    }

    public void ForceTopMost()
    {
        _virtualBounds = Bounds;

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            Bounds.Left,
            Bounds.Top,
            Bounds.Width,
            Bounds.Height,
            NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW);
    }

    protected override void OnPaint(
        PaintEventArgs e)
    {
        if (_desktopCount <= 0)
        {
            e.Graphics.Clear(Color.Black);
            return;
        }

        e.Graphics.CompositingMode =
            CompositingMode.SourceCopy;

        e.Graphics.CompositingQuality =
            CompositingQuality.HighSpeed;

        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.Half;

        var width =
            ClientSize.Width;

        var leftStep =
            (int)Math.Floor(_position);

        var fraction =
            _position - Math.Floor(_position);

        var leftDesktop =
            DesktopForStep(leftStep);

        var rightDesktop =
            DesktopForStep(leftStep + 1);

        var leftFrame =
            _frameProvider(leftDesktop);

        var rightFrame =
            _frameProvider(rightDesktop);

        var leftX =
            (int)Math.Round(
                -fraction * width);

        var rightX =
            leftX + width;

        if (leftFrame is not null)
        {
            e.Graphics.DrawImageUnscaled(
                leftFrame,
                leftX,
                0);
        }

        if (rightFrame is not null)
        {
            e.Graphics.DrawImageUnscaled(
                rightFrame,
                rightX,
                0);
        }
    }

    private int DesktopForStep(int step)
    {
        var value =
            (_baseDesktop + step) %
            _desktopCount;

        if (value < 0)
            value += _desktopCount;

        return value;
    }
}
internal sealed class TransitionRunner
{
    private readonly VirtualDesktopAccessor _desktop;
    private readonly int _durationMs;
    private readonly int _captureDelayMs;

    private bool _running;

    public TransitionRunner(
        VirtualDesktopAccessor desktop,
        int durationMs,
        int captureDelayMs)
    {
        _desktop = desktop;
        _durationMs = durationMs;
        _captureDelayMs = captureDelayMs;
    }

    public void Run(SwipeDirection direction)
    {
        if (_running)
            return;

        _running = true;

        try
        {
            var count = _desktop.GetDesktopCount();
            var current = _desktop.GetCurrentDesktopNumber();

            if (count <= 1 || current < 0)
                return;

            var target =
                direction == SwipeDirection.Left
                    ? (current + 1) % count
                    : (current - 1 + count) % count;

            RunTransition(direction, target);
        }
        finally
        {
            _running = false;
        }
    }

    private void RunTransition(
        SwipeDirection direction,
        int targetDesktop)
    {
        var bounds = SystemInformation.VirtualScreen;

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        using var outgoing =
            ScreenCapture.Capture(bounds);

        using var form =
            new TransitionForm(
                bounds,
                outgoing,
                direction,
                _durationMs);

        form.Show();
        form.ForceTopMost();
        Application.DoEvents();

        var overlayHandle = form.Handle;

        NativeMethods.SetWindowDisplayAffinity(
            overlayHandle,
            NativeMethods.WDA_EXCLUDEFROMCAPTURE);

        var pinResult =
            _desktop.PinWindow(overlayHandle);

        if (pinResult < 0)
        {
            form.Close();

            throw new InvalidOperationException(
                "VirtualDesktopAccessor could not pin the overlay.");
        }

        try
        {
            _desktop.GoToDesktopNumber(targetDesktop);

            form.AnimateWithIncomingCapture(
                () =>
                {
                    if (_captureDelayMs > 0)
                        Thread.Sleep(_captureDelayMs);

                    return ScreenCapture.Capture(bounds);
                });
        }
        finally
        {
            _desktop.UnPinWindow(overlayHandle);
        }
    }
}

internal sealed class Options
{
    public SwipeDirection? Direction { get; init; }

    public required string DllPath { get; init; }

    public bool Resident { get; init; }

    public int DurationMs { get; init; } = 220;

    // Previously 55ms.
    // Keeping this very low makes the swipe react much faster.
    public int CaptureDelayMs { get; init; } = 8;

    public bool Silent { get; init; } = true;

    public static bool TryParse(
        string[] args,
        out Options? options,
        out string error)
    {
        options = null;
        error = "";

        SwipeDirection? direction = null;

        string? dllPath = null;

        var resident = false;
        var duration = 220;
        var captureDelay = 8;
        var silent = true;

        for (var i = 0; i < args.Length; i++)
        {
            var value = args[i];

            if (TryParseDirection(value, out var parsedDirection))
            {
                direction = parsedDirection;
                continue;
            }

            switch (value.ToLowerInvariant())
            {
                case "--resident":
                    resident = true;
                    break;

                case "--dll":
                    if (++i >= args.Length)
                    {
                        error = "--dll requires a path.";
                        return false;
                    }

                    dllPath = args[i];
                    break;

                case "--duration":
                    if (++i >= args.Length ||
                        !int.TryParse(args[i], out duration) ||
                        duration < 80 ||
                        duration > 1000)
                    {
                        error =
                            "--duration must be between 80 and 1000.";
                        return false;
                    }

                    break;

                case "--capture-delay":
                    if (++i >= args.Length ||
                        !int.TryParse(args[i], out captureDelay) ||
                        captureDelay < 0 ||
                        captureDelay > 500)
                    {
                        error =
                            "--capture-delay must be between 0 and 500.";
                        return false;
                    }

                    break;

                case "--show-errors":
                    silent = false;
                    break;

                case "-h":
                case "--help":
                case "/?":
                    error = """
                    Usage:

                    Resident mode:
                      DeskSwipe.exe --resident

                    Direct test:
                      DeskSwipe.exe left
                      DeskSwipe.exe right
                    """;

                    return false;

                default:
                    error = $"Unknown option: {value}";
                    return false;
            }
        }

        if (!resident && direction is null)
        {
            error =
                "Specify left/right or use --resident.";

            return false;
        }

        dllPath ??=
            Path.Combine(
                AppContext.BaseDirectory,
                "VirtualDesktopAccessor.dll");

        dllPath =
            Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(
                    dllPath));

        if (!File.Exists(dllPath))
        {
            error =
                $"VirtualDesktopAccessor.dll was not found at:\n{dllPath}";

            return false;
        }

        options = new Options
        {
            Direction = direction,
            DllPath = dllPath,
            Resident = resident,
            DurationMs = duration,
            CaptureDelayMs = captureDelay,
            Silent = silent
        };

        return true;
    }

    private static bool TryParseDirection(
        string value,
        out SwipeDirection direction)
    {
        switch (value.ToLowerInvariant())
        {
            case "left":
            case "next":
                direction = SwipeDirection.Left;
                return true;

            case "right":
            case "previous":
            case "prev":
                direction = SwipeDirection.Right;
                return true;

            default:
                direction = SwipeDirection.Left;
                return false;
        }
    }
}

internal static class ScreenCapture
{
    public static Bitmap Capture(Rectangle bounds)
    {
        var bitmap =
            new Bitmap(
                bounds.Width,
                bounds.Height,
                PixelFormat.Format32bppPArgb);

        using var graphics =
            Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(
            bounds.Left,
            bounds.Top,
            0,
            0,
            bounds.Size,
            CopyPixelOperation.SourceCopy);

        return bitmap;
    }
}

internal sealed class TransitionForm : Form
{
    private readonly Rectangle _virtualBounds;
    private readonly Bitmap _outgoing;
    private readonly SwipeDirection _direction;
    private readonly int _durationMs;

    private readonly Stopwatch _clock = new();

    private Bitmap? _incoming;
    private double _progress;

    public TransitionForm(
        Rectangle virtualBounds,
        Bitmap outgoing,
        SwipeDirection direction,
        int durationMs)
    {
        _virtualBounds = virtualBounds;
        _outgoing = outgoing;
        _direction = direction;
        _durationMs = durationMs;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        Bounds = virtualBounds;

        TopMost = true;
        BackColor = Color.Black;

        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_TOPMOST = 0x00000008;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var cp = base.CreateParams;

            cp.ExStyle |=
                WS_EX_TOOLWINDOW |
                WS_EX_TOPMOST |
                WS_EX_NOACTIVATE;

            return cp;
        }
    }

    public void SetIncoming(Bitmap incoming)
    {
        _incoming = (Bitmap)incoming.Clone();
    }

    public void HideForCapture()
    {
        Hide();
        Application.DoEvents();

        // Old build waited 16ms here.
        Thread.Sleep(1);
    }

    public void ShowWithoutStealingFocus()
    {
        NativeMethods.ShowWindow(
            Handle,
            NativeMethods.SW_SHOWNOACTIVATE);

        Application.DoEvents();
    }

    public void ForceTopMost()
    {
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            _virtualBounds.Left,
            _virtualBounds.Top,
            _virtualBounds.Width,
            _virtualBounds.Height,
            NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW);
    }

    public void AnimateWithIncomingCapture(
        Func<Bitmap> captureIncoming)
    {
        Bitmap? captured = null;
        Exception? captureError = null;

        var captureTask = Task.Run(() =>
        {
            try
            {
                captured = captureIncoming();
            }
            catch (Exception ex)
            {
                captureError = ex;
            }
        });

        _clock.Restart();

        while (true)
        {
            if (_incoming is null &&
                captureTask.IsCompleted)
            {
                if (captureError is not null)
                    throw captureError;

                if (captured is not null)
                {
                    SetIncoming(captured);
                    captured.Dispose();
                    captured = null;
                }
            }

            var elapsed =
                _clock.Elapsed.TotalMilliseconds;

            _progress =
                Math.Min(
                    1.0,
                    elapsed / _durationMs);

            Invalidate();
            Update();

            if (_progress >= 1.0)
                break;

            Thread.Sleep(5);
            Application.DoEvents();
        }

        if (_incoming is null)
        {
            captureTask.Wait();

            if (captureError is not null)
                throw captureError;

            if (captured is not null)
            {
                SetIncoming(captured);
                captured.Dispose();
            }
        }

        Close();
    }
    public void Animate()
    {
        _clock.Restart();

        while (true)
        {
            var elapsed =
                _clock.Elapsed.TotalMilliseconds;

            _progress =
                Math.Min(
                    1.0,
                    elapsed / _durationMs);

            Invalidate();
            Update();

            if (_progress >= 1.0)
                break;

            Thread.Sleep(5);
            Application.DoEvents();
        }

        Close();
    }

    protected override void OnPaint(
        PaintEventArgs e)
    {
        e.Graphics.CompositingMode =
            CompositingMode.SourceCopy;

        e.Graphics.CompositingQuality =
            CompositingQuality.HighSpeed;

        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.Half;

        var eased =
            EaseOutCubic(_progress);

        var travel =
            (float)(_virtualBounds.Width * eased);

        float outgoingX;
        float incomingX;

        if (_direction == SwipeDirection.Left)
        {
            outgoingX = -travel;
            incomingX =
                _virtualBounds.Width - travel;
        }
        else
        {
            outgoingX = travel;
            incomingX =
                -_virtualBounds.Width + travel;
        }

        if (_incoming is not null)
        {
            e.Graphics.DrawImageUnscaled(
                _incoming,
                (int)Math.Round(incomingX),
                0);
        }
        else
        {
            e.Graphics.DrawImageUnscaled(
                _outgoing,
                (int)Math.Round(incomingX),
                0);
        }

        e.Graphics.DrawImageUnscaled(
            _outgoing,
            (int)Math.Round(outgoingX),
            0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _incoming?.Dispose();

        base.Dispose(disposing);
    }

    private static double EaseOutCubic(double t)
    {
        var inverse = 1.0 - t;

        return 1.0 -
            inverse *
            inverse *
            inverse;
    }
}

internal sealed class VirtualDesktopAccessor : IDisposable
{
    private readonly IntPtr _module;

    private readonly GetNumberDelegate
        _getCurrentDesktopNumber;

    private readonly GetNumberDelegate
        _getDesktopCount;

    private readonly GoToDesktopNumberDelegate
        _goToDesktopNumber;

    private readonly GetWindowDesktopNumberDelegate
        _getWindowDesktopNumber;

    private readonly HwndDelegate
        _pinWindow;

    private readonly HwndDelegate
        _unPinWindow;

    private VirtualDesktopAccessor(
        IntPtr module,
        GetNumberDelegate getCurrentDesktopNumber,
        GetNumberDelegate getDesktopCount,
        GoToDesktopNumberDelegate goToDesktopNumber,
        GetWindowDesktopNumberDelegate getWindowDesktopNumber,
        HwndDelegate pinWindow,
        HwndDelegate unPinWindow)
    {
        _module = module;
        _getCurrentDesktopNumber =
            getCurrentDesktopNumber;

        _getDesktopCount =
            getDesktopCount;

        _goToDesktopNumber =
            goToDesktopNumber;

        _getWindowDesktopNumber =
            getWindowDesktopNumber;

        _pinWindow = pinWindow;
        _unPinWindow = unPinWindow;
    }

    public static VirtualDesktopAccessor Load(
        string path)
    {
        var module =
            NativeMethods.LoadLibrary(path);

        if (module == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Windows could not load VirtualDesktopAccessor.dll from:\n{path}");
        }

        try
        {
            return new VirtualDesktopAccessor(
                module,
                GetRequiredDelegate<GetNumberDelegate>(
                    module,
                    "GetCurrentDesktopNumber"),

                GetRequiredDelegate<GetNumberDelegate>(
                    module,
                    "GetDesktopCount"),

                GetRequiredDelegate<GoToDesktopNumberDelegate>(
                    module,
                    "GoToDesktopNumber"),

                GetRequiredDelegate<GetWindowDesktopNumberDelegate>(
                    module,
                    "GetWindowDesktopNumber"),

                GetRequiredDelegate<HwndDelegate>(
                    module,
                    "PinWindow"),

                GetRequiredDelegate<HwndDelegate>(
                    module,
                    "UnPinWindow"));
        }
        catch
        {
            NativeMethods.FreeLibrary(module);
            throw;
        }
    }

    public int GetCurrentDesktopNumber() =>
        _getCurrentDesktopNumber();

    public int GetDesktopCount() =>
        _getDesktopCount();

    public int GoToDesktopNumber(int number) =>
        _goToDesktopNumber(number);

    public int GetWindowDesktopNumber(IntPtr hwnd) =>
        _getWindowDesktopNumber(hwnd);

    public int PinWindow(IntPtr hwnd) =>
        _pinWindow(hwnd);

    public int UnPinWindow(IntPtr hwnd) =>
        _unPinWindow(hwnd);

    public void Dispose()
    {
        if (_module != IntPtr.Zero)
            NativeMethods.FreeLibrary(_module);
    }

    private static T GetRequiredDelegate<T>(
        IntPtr module,
        string name)
        where T : Delegate
    {
        var address =
            NativeMethods.GetProcAddress(
                module,
                name);

        if (address == IntPtr.Zero)
        {
            throw new MissingMethodException(
                $"VirtualDesktopAccessor.dll does not export {name}.");
        }

        return Marshal.GetDelegateForFunctionPointer<T>(
            address);
    }

    [UnmanagedFunctionPointer(
        CallingConvention.Cdecl)]
    private delegate int GetNumberDelegate();

    [UnmanagedFunctionPointer(
        CallingConvention.Cdecl)]
    private delegate int GoToDesktopNumberDelegate(
        int desktopNumber);

    [UnmanagedFunctionPointer(
        CallingConvention.Cdecl)]
    private delegate int GetWindowDesktopNumberDelegate(
        IntPtr hwnd);

    [UnmanagedFunctionPointer(
        CallingConvention.Cdecl)]
    private delegate int HwndDelegate(
        IntPtr hwnd);
}

internal static class FocusRepair
{
    public static void ActivateWindowOnDesktop(
        VirtualDesktopAccessor desktop,
        int desktopNumber)
    {
        IntPtr candidate = IntPtr.Zero;

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd))
                return true;

            if (NativeMethods.GetWindow(
                hwnd,
                NativeMethods.GW_OWNER) != IntPtr.Zero)
            {
                return true;
            }

            if (NativeMethods.GetWindowTextLength(hwnd) == 0)
                return true;

            var windowDesktop =
                desktop.GetWindowDesktopNumber(hwnd);

            if (windowDesktop != desktopNumber)
                return true;

            candidate = hwnd;
            return false;
        }, IntPtr.Zero);

        if (candidate == IntPtr.Zero)
            return;

        NativeMethods.SetForegroundWindow(candidate);

        var info = new NativeMethods.FLASHWINFO
        {
            cbSize =
                (uint)Marshal.SizeOf<NativeMethods.FLASHWINFO>(),

            hwnd = candidate,

            dwFlags =
                NativeMethods.FLASHW_STOP,

            uCount = 0,
            dwTimeout = 0
        };

        NativeMethods.FlashWindowEx(ref info);
    }
}
internal static class NativeMethods
{
    public const int SW_SHOWNOACTIVATE = 4;

    public const uint GW_OWNER = 4;
    public const uint FLASHW_STOP = 0;

    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public static readonly IntPtr
        HWND_TOPMOST = new(-1);

    public delegate bool EnumWindowsProc(
        IntPtr hWnd,
        IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(
        EnumWindowsProc lpEnumFunc,
        IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(
        IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(
        IntPtr hWnd,
        uint uCmd);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(
        IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(
        IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool FlashWindowEx(
        ref FLASHWINFO pwfi);
[DllImport(
        "kernel32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    public static extern IntPtr LoadLibrary(
        string lpFileName);
[DllImport(
        "kernel32.dll",
        CharSet = CharSet.Ansi,
        SetLastError = true)]
    public static extern IntPtr GetProcAddress(
        IntPtr hModule,
        string lpProcName);
[DllImport(
        "kernel32.dll",
        SetLastError = true)]
    public static extern bool FreeLibrary(
        IntPtr hModule);

    [DllImport("user32.dll")]
    public static extern bool SetWindowDisplayAffinity(
        IntPtr hWnd,
        uint dwAffinity);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(
        IntPtr hWnd,
        int nCmdShow);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);
}




















