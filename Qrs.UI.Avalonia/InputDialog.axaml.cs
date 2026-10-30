using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Qrs.UI.Avalonia
{
    public partial class InputDialog : Window
    {
        public InputDialog()
        {
            InitializeComponent();
        }

        public InputDialog(string prompt, string title) : this()
        {
            Title = title;
            InputLabel.Text = prompt;
        }

        /// <summary>
        /// Shows a dialog for the user to input a text string.
        /// </summary>
        /// <param name="prompt">The text prompt to display above the input field.</param>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="owner">The owner window for modal display.</param>
        /// <returns>The text entered by the user if submitted, or null if canceled.</returns>
        public static async Task<string?> Show(string prompt, string title, Window owner)
        {
            var dialog = new InputDialog(prompt, title);
            var result = await dialog.ShowDialog<string?>(owner);
            return result;
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            Close(InputBox.Text);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            InputBox.Focus();
        }
    }
}
