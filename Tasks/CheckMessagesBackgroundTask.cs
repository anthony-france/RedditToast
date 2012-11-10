// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

using System;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage;
using Windows.System.Threading;
using com.reddit.api;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using BoxKite.Notifications;
using BoxKite.Notifications.Templates;
using Windows.UI.Core;

//
// The namespace for the background tasks.
//
namespace Tasks
{
    //
    // A background task always implements the IBackgroundTask interface.
    //
    public sealed class CheckMessagesBackgroundTask : IBackgroundTask
    {
        volatile bool _cancelRequested = false;
        BackgroundTaskDeferral _deferral = null;
        //ThreadPoolTimer _periodicTimer = null;
        //uint _progress = 0;
        IBackgroundTaskInstance _taskInstance = null;
        DateTime lastMessage = DateTime.Now;

        //
        // The Run method is the entry point of a background task.
        //
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //
            // Associate a cancellation handler with the background task.
            //
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            //
            // Get the deferral object from the task instance, and take a reference to the taskInstance;
            //
            _deferral = taskInstance.GetDeferral();
            _taskInstance = taskInstance;

            //_periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(MessageCheckCallback), TimeSpan.FromSeconds(60));
            MessageCheck();
        }

        //
        // Handles background task cancellation.
        //
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
        }

        //private void MessageCheckCallback(ThreadPoolTimer timer)
        private void MessageCheck()
        {
            if (_cancelRequested == false)
            {
                _taskInstance.Progress = 25;
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("reddit_username") &&
                      Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("reddit_password"))
                {
                    
                    User ru = new User();
                    Session r = new Session();
                    try
                    {
                        r = User.Login(
                              (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_username"],
                              (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_password"]
                        );
                        ru = User.Get(r, (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["reddit_username"]);
                        _taskInstance.Progress = 40;
                        ru.Messages = User.GetMessages(r, true);
                        ru.Messages.Reverse();
                        _taskInstance.Progress = 50;
                    }
                    catch (Exception)
                    {
                        _taskInstance.Progress = 0;
                        _deferral.Complete();
                        return;
                    }

                    if (ru.Messages.Count > 0)
                    {
                        BadgeNumericNotificationContent badgeContent = new BadgeNumericNotificationContent((uint)ru.Messages.Count);
                        BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeContent.CreateNotification());
                        var tileUpdate = TileContentFactory.CreateTileSquareImage();
                        tileUpdate.Image.Src = "Assets/hasmail.png";
                        TileUpdateManager.CreateTileUpdaterForApplication().Update(tileUpdate.CreateNotification());
                    }
                    else
                    {
                        TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                        BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
                    }

                    var notifier = ToastNotificationManager.CreateToastNotifier();
                    
                    if (notifier.Setting == NotificationSetting.Enabled)
                    {
                        _taskInstance.Progress = 75;
                        foreach (Message message in ru.Messages)
                        {
                            if ((bool)Windows.Storage.ApplicationData.Current.LocalSettings.Values["persistant_toast"] == true)
                            {
                                lastMessage = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            }

                            if (lastMessage < message.Created)
                            {
                                lastMessage = message.Created;

                                var template = ToastContentFactory.CreateToastImageAndText02();

                                if (message.Context.Length > 0)
                                {
                                    template.Launch = "/message/unread";
                                }
                                else
                                {
                                    template.Launch = "/message/messages/" + message.ID;
                                }

                                if ((bool)Windows.Storage.ApplicationData.Current.LocalSettings.Values["toast_length"] == true)
                                {
                                    template.Duration = ToastDuration.Long;
                                }
                                else
                                {
                                    template.Duration = ToastDuration.Short;
                                }

                                template.TextHeading.Text = message.Subject + " - " + message.Author;
                                template.TextBodyWrap.Text = message.Body;
                                template.Image.Src = "Assets/hasmail.png";
                                ToastNotification notification = template.CreateNotification();
                                ToastNotificationManager.CreateToastNotifier().Show(template.CreateNotification());
                            }
                        }
                    }
                    _taskInstance.Progress = 100;
                    _deferral.Complete();
                }
            }
            else
            {
                //
                // Indicate that the background task has completed.
                //
                _deferral.Complete();
            }
        }
    }
}
