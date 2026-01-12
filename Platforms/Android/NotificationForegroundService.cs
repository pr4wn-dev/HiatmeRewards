using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace HiatmeApp
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
    public class NotificationForegroundService : Service
    {
        private const int NotificationId = 9999;
        private const string ChannelId = "hiatme_foreground_channel";

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            StartForeground(NotificationId, CreateNotification());
            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    "Hiatme Background Service",
                    NotificationImportance.Low)
                {
                    Description = "Keeps app running for notifications"
                };
                
                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            var builder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Hiatme")
                .SetContentText("Ready for notifications")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityLow);

            return builder.Build();
        }

        public static void Start(Context context)
        {
            var intent = new Intent(context, typeof(NotificationForegroundService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        public static void Stop(Context context)
        {
            var intent = new Intent(context, typeof(NotificationForegroundService));
            context.StopService(intent);
        }
    }
}

