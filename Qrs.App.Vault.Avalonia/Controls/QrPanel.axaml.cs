using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QRCoder;
using System.IO;
using Avalonia.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace Qrs.App.Vault.Avalonia.Controls;

public partial class QrPanel : QrsViewBase
{

    private string QrCodeContent;

    public QrPanel()
	{
		InitializeComponent();
	}

    public void SetQrCodeContent(string qrCodeContent)
    {
        QrCodeContent = qrCodeContent;

        var qrGen = new QRCodeGenerator();
        var data = qrGen.CreateQrCode(qrCodeContent, QRCodeGenerator.ECCLevel.L);

        var qrCode = new PngByteQRCode(data);
        byte[] pngBytes = qrCode.GetGraphic(3);
        using var pngStream = new MemoryStream(pngBytes);
        var pngBitmap = new Bitmap(pngStream);

        QrImage.Source = pngBitmap;
        QrImage.IsVisible = true;
    }

    public void OnCopyQrCodeClicked(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await Window.Clipboard.SetTextAsync(QrCodeContent);
        });
    }

    private void Button_Cancel_Clicked(object? sender, RoutedEventArgs e)
    {
        //Header.Title = DateTime.Now.ToString();

        App.DoCancelQrCodePolling();


        //App.DoHome(); << this does not work b/c it gets overthrown by other code that processes results of QR Code
    }

    public void SetTitle(string title, string subtitle)
    {
        Header.Title = title;
        Header.Subtitle = subtitle;
    }
}