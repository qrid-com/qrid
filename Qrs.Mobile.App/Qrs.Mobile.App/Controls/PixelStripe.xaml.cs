using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Layouts;
using System.Runtime.CompilerServices;
using AbsoluteLayout = Microsoft.Maui.Controls.AbsoluteLayout;

namespace Qrs.Mobile.App.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PixelStripe : ContentView
    {
        public int PixelSize = 16;
        public Microsoft.Maui.Graphics.Color primaryColor;
        public PixelStripe()
        {
            InitializeComponent();

            primaryColor = (Microsoft.Maui.Graphics.Color)Application.Current.Resources["Primary"];

            MakePixelRow(0, "01110110110111101111011011101101");
            MakePixelRow(1, "01001001011101000101110110010010");
        }

        public void MakePixelRow(int rowNumber, string pattern)
        {
            var chars = pattern.ToCharArray();
            for(int i=0; i<chars.Length; i++)
            {
                // Create the index BoxView and add to layout.

                BoxView boxView = new BoxView
                {
                    BackgroundColor = chars[i] == '1' ? primaryColor : Colors.Transparent
                };

                AbsoluteLayout.SetLayoutBounds(boxView, new Rect(i * PixelSize, rowNumber * PixelSize, PixelSize, PixelSize));
                AbsoluteLayout.SetLayoutFlags(boxView, AbsoluteLayoutFlags.None);

                // Add the BoxView to the AbsoluteLayout
                absoluteLayout.Children.Add(boxView);
            }
        }


    }
}