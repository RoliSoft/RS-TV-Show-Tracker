namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Remote;

    /// <summary>
    /// Interaction logic for SendFeedbackWindow.xaml
    /// </summary>
    public partial class SendFeedbackWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendFeedbackWindow"/> class.
        /// </summary>
        public SendFeedbackWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (AeroGlassCompositionEnabled)
            {
                SetAeroGlassTransparency();
            }

            ideaRadioButton.IsChecked = true;

            var name  = Settings.Get("User Name");
            var email = Settings.Get("User Email");

            nameTextBox.Text  = string.IsNullOrWhiteSpace(name)
                                ? Environment.UserName
                                : name;
            emailTextBox.Text = email;
        }

        /// <summary>
        /// Handles the Click event of the sendButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(messageTextBox.Text)) return;

            if (!string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                Settings.Set("User Name", nameTextBox.Text);
            }

            if (!string.IsNullOrWhiteSpace(emailTextBox.Text))
            {
                Settings.Set("User Email", emailTextBox.Text);
            }

            var name    = nameTextBox.Text;
            var email   = emailTextBox.Text;
            var message = messageTextBox.Text;
            var type    = ideaRadioButton.IsChecked.HasValue && ideaRadioButton.IsChecked.Value
                          ? "idea"
                          : bugRadioButton.IsChecked.HasValue && bugRadioButton.IsChecked.Value
                            ? "bug"
                            : "other";

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Environment.UserName;
            }

            ideaRadioButton.IsEnabled = bugRadioButton.IsEnabled = otherRadioButton.IsEnabled = nameTextBox.IsEnabled = emailTextBox.IsEnabled = messageTextBox.IsEnabled = submitButton.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);

            new Task(() => { try
                {
                    API.SendFeedback(type, name, email, message);

                    Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                    Dispatcher.Invoke((Action)(() =>
                    {
                        progressBar.Visibility = Visibility.Collapsed;

                        new TaskDialog
                            {
                                Icon            = TaskDialogStandardIcon.Information,
                                Caption         = "Feedback sent",
                                InstructionText = "Feedback sent",
                                Text            = "Thank you for your feedback!" + Environment.NewLine + Environment.NewLine + "If you specified an email address I'll answer shortly.",
                                Cancelable      = true
                            }.Show();

                        Close();
                    }));
                } catch { } }).Start();
        }
    }
}
