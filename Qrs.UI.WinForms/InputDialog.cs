using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Qrs.UI.WinForms
{
    public static class InputDialog
    {
        /// <summary>
        /// Shows a popup dialog for the user to input a text string.
        /// </summary>
        /// <param name="prompt">The text prompt to display above the input field.</param>
        /// <param name="title">The title of the dialog window.</param>
        /// <returns>The text entered by the user if submitted, or null if canceled.</returns>
        public static string Show(string prompt, string title)
        {
            // Create a new form to act as the dialog
            Form dialog = new Form();
            dialog.Text = title;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ClientSize = new System.Drawing.Size(400, 150);
            dialog.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Create the prompt label
            Label label = new Label();
            label.Text = prompt;
            label.AutoSize = true;
            label.Location = new System.Drawing.Point(15, 15);
            label.MaximumSize = new System.Drawing.Size(370, 0);

            // Create the input textbox
            TextBox textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(15, 50);
            textBox.Size = new System.Drawing.Size(370, 25);

            // Create the OK button
            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new System.Drawing.Point(215, 90);

            // Create the Cancel button
            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(310, 90);

            // Add controls to the form
            dialog.Controls.Add(label);
            dialog.Controls.Add(textBox);
            dialog.Controls.Add(okButton);
            dialog.Controls.Add(cancelButton);

            // Set the dialog's Accept and Cancel buttons
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            dialog.TopMost = true;

            // Show the dialog and get the result
            DialogResult result = dialog.ShowDialog();

            // Return the input if OK was pressed; otherwise, return null
            return result == DialogResult.OK ? textBox.Text : null;
        }

    }
}
