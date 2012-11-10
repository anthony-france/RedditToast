using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using com.reddit.api;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using BoxKite.Notifications;
using BoxKite.Notifications.Templates;
using Windows.UI.Core;
using Windows.ApplicationModel.Background;
using Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Reddit_Toast
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string _reportedStatus = string.Empty;
        bool _toastLengthToggle = false;
        bool _toastPersistant = false;
        string _user = string.Empty;
        string _pass = string.Empty;
        
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackgroundTaskRegistration task = App.RegisterBackgroundTask("Tasks.CheckMessagesBackgroundTask", "CheckMessagesBackgroundTask", new TimeTrigger(15, false), null);

            if (task.Name.Length > 0)
            {
                txtStatus.Text = task.Name + " is active.";
                task.Progress += new BackgroundTaskProgressEventHandler(task_Progress);
                task.Completed += new BackgroundTaskCompletedEventHandler(task_Completed);
            }

            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("reddit_username") &&
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("reddit_password"))
            {
                _user = (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_username"];
                _pass = (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_password"];
            }

            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("toast_length"))
            {
                _toastLengthToggle = (bool)Windows.Storage.ApplicationData.Current.LocalSettings.Values["toast_length"];
            }
            else
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["toast_length"] = false;
                _toastLengthToggle = false;
            }

            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("persistant_toast"))
            {
                _toastPersistant = (bool)Windows.Storage.ApplicationData.Current.LocalSettings.Values["persistant_toast"];
            }
            else
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["persistant_toast"] = false;
                _toastPersistant = false;
            }

            UpdateUI();
        }

        void task_Completed(IBackgroundTaskRegistration task, BackgroundTaskCompletedEventArgs args)
        {
            _reportedStatus = "Task run complete.";
            UpdateUI();
        }

        void task_Progress(IBackgroundTaskRegistration task, BackgroundTaskProgressEventArgs args)
        {
            var progress = args.Progress + "%";
            _reportedStatus = progress;
            UpdateUI();
        }

        private async void UpdateUI()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                txtStatus.Text = _reportedStatus;
                togToastLength.IsOn = _toastLengthToggle;
                togPersistant.IsOn = _toastPersistant;
                txtRedditPassword.Password = _pass;
                txtRedditUsername.Text = _user;
            });
        }

        private void togToastLength_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.togToastLength.IsOn == true)
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["toast_length"] = true;
                _toastLengthToggle = true;
                _reportedStatus = "Toggle toast length long.";
            }
            else
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["toast_length"] = false;
                _toastLengthToggle = false;
                _reportedStatus = "Toggle toast length short.";
            }
                        
            UpdateUI();
        }

        private void txtRedditUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_username"] = txtRedditUsername.Text;
            _reportedStatus = "Username saved.";
            _user = txtRedditUsername.Text;
            UpdateUI();
        }

        private void txtRedditPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_password"] = txtRedditPassword.Password;
            _reportedStatus = "Password saved.";
            _pass = txtRedditPassword.Password;
            UpdateUI();
        }

        private void togPersistant_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.togPersistant.IsOn == true)
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["persistant_toast"] = true;
                _toastPersistant = true;
                _reportedStatus = "Toggle persistant setting turned on.";
            }
            else
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["persistant_toast"] = false;
                _toastPersistant = false;
                _reportedStatus = "Toggle persistant setting turned off.";
            }

            UpdateUI();
        }

    }
}
