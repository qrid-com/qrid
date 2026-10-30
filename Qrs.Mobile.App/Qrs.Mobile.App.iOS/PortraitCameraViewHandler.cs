using AVFoundation;
using BarcodeScanning;
using Foundation;
using Microsoft.Maui.ApplicationModel;

namespace Qrs.Mobile.App.iOS;

/// <summary>
/// CameraView handler that always keeps the camera preview in portrait (90 degrees).
/// BarcodeScanning.Native.Maui (iOS 17+) applies AVCaptureDeviceRotationCoordinator.VideoRotationAngleForHorizonLevelPreview,
/// which reports 0 (landscape) until the preview layer is attached to a laid-out window. When the page hosting the
/// CameraView becomes the new root (Window.Page / Application.MainPage swap), the preview is first shown in landscape
/// and only later rotated to portrait. The app is portrait-only, so the angle is pinned to 90 degrees and any
/// other value written by the library is reverted immediately (KVO) before it is rendered.
/// </summary>
public class PortraitCameraViewHandler : CameraViewHandler
{
    private static readonly System.Runtime.InteropServices.NFloat PortraitAngle = (System.Runtime.InteropServices.NFloat)90;

    private AVCaptureVideoPreviewLayer? _previewLayer;
    private AVCaptureConnection? _observedConnection;
    private IDisposable? _connectionObserver;
    private NSObject? _sessionStartedObserver;
    private bool _connected;

    protected override void ConnectHandler(BarcodeView platformView)
    {
        base.ConnectHandler(platformView);
        _connected = true;

        _previewLayer = platformView.Layer.Sublayers?.OfType<AVCaptureVideoPreviewLayer>().FirstOrDefault();
        if (_previewLayer?.Session is AVCaptureSession session)
        {
            _sessionStartedObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                AVCaptureSession.DidStartRunningNotification,
                OnSessionStartedRunning,
                session);
        }

        // the preview connection is created on the library's session queue shortly after the handler is connected;
        // lock it as soon as it exists, so the very first frames are already rendered in portrait
        _ = WaitForConnectionAsync();
    }

    protected override void DisconnectHandler(BarcodeView platformView)
    {
        _connected = false;

        if (_sessionStartedObserver is not null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_sessionStartedObserver);
            _sessionStartedObserver.Dispose();
            _sessionStartedObserver = null;
        }
        _connectionObserver?.Dispose();
        _connectionObserver = null;
        _observedConnection = null;
        _previewLayer = null;

        base.DisconnectHandler(platformView);
    }

    private async Task WaitForConnectionAsync()
    {
        for (int i = 0; i < 100 && _connected; i++)
        {
            if (LockPortrait())
                return;
            await Task.Delay(20);
        }
    }

    private void OnSessionStartedRunning(NSNotification notification)
    {
        // posted on the library's session queue - apply right away, then (re)attach the observer on the UI thread
        var connection = _previewLayer?.Connection;
        if (connection is not null)
            ApplyPortrait(connection);

        MainThread.BeginInvokeOnMainThread(() => LockPortrait());
    }

    /// <summary>
    /// Sets the preview connection to portrait and observes it, so any later change made by the library is reverted.
    /// Must run on the UI thread. Returns true once a preview connection exists.
    /// </summary>
    private bool LockPortrait()
    {
        if (!_connected)
            return false;

        var connection = _previewLayer?.Connection;
        if (connection is null)
            return false;

        ApplyPortrait(connection);

        if (!ReferenceEquals(connection, _observedConnection))
        {
            _connectionObserver?.Dispose();
            _observedConnection = connection;
            var key = OperatingSystem.IsIOSVersionAtLeast(17) ? "videoRotationAngle" : "videoOrientation";
            _connectionObserver = connection.AddObserver(key, NSKeyValueObservingOptions.New, _ => ApplyPortrait(connection));
        }
        return true;
    }

    private static void ApplyPortrait(AVCaptureConnection connection)
    {
        if (OperatingSystem.IsIOSVersionAtLeast(17))
        {
            if (connection.VideoRotationAngle != PortraitAngle && connection.IsVideoRotationAngleSupported(PortraitAngle))
                connection.VideoRotationAngle = PortraitAngle;
        }
        else if (connection.SupportsVideoOrientation && connection.VideoOrientation != AVCaptureVideoOrientation.Portrait)
        {
            connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
        }
    }
}
