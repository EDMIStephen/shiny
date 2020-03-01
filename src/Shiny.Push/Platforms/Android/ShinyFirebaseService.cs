﻿using System;
using Android.App;
using Firebase.Messaging;
using Shiny.Logging;
using Shiny.Notifications;
using Shiny.Settings;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class ShinyFirebaseService : FirebaseMessagingService
    {
        readonly Lazy<ISettings> settings = ShinyHost.LazyResolve<ISettings>();
        readonly Lazy<INotificationManager> notifications = ShinyHost.LazyResolve<INotificationManager>();
        readonly Lazy<IPushDelegate> pushDelegate = ShinyHost.LazyResolve<IPushDelegate>();
        readonly Lazy<IMessageBus> msgBus = ShinyHost.LazyResolve<IMessageBus>();


        public override async void OnMessageReceived(RemoteMessage message) => await Log.SafeExecute(async () =>
        {
            await this.pushDelegate.Value.OnReceived(message.Data);

            var native = message.GetNotification();
            await this.notifications.Value.Send(new Notification
            {
                Title = native.Title,
                Message = native.Body
            });
            this.msgBus.Value.Publish(
                nameof(ShinyFirebaseService),
                message.Data
            );
        });


        public override async void OnNewToken(string token) => await Log.SafeExecute(async () =>
        {
            this.settings.Value.SetRegToken(token);
            this.settings.Value.SetRegDate(DateTime.UtcNow);
            await this.pushDelegate.Value.OnTokenChanged(token);
        });
    }
}