﻿using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Bit.Core;
using System.Linq;
using Bit.App.Abstractions;
using Bit.Core.Utilities;
using Bit.Core.Abstractions;
using System.IO;
using System;
using Android.Content;
using Bit.Droid.Utilities;

namespace Bit.Droid
{
    [Activity(
        Label = "Bitwarden",
        Icon = "@mipmap/ic_launcher",
        Theme = "@style/MainTheme",
        Exported = false,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [Register("com.x8bit.bitwarden.MainActivity")]
    public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private IDeviceActionService _deviceActionService;
        private IMessagingService _messagingService;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            _deviceActionService = ServiceContainer.Resolve<IDeviceActionService>("deviceActionService");
            _messagingService = ServiceContainer.Resolve<IMessagingService>("messagingService");

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App.App());
        }

        public async override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == Constants.SelectFilePermissionRequestCode)
            {
                if(grantResults.Any(r => r != Permission.Granted))
                {
                    _messagingService.Send("selectFileCameraPermissionDenied");
                }
                await _deviceActionService.SelectFileAsync();
            }
            else
            {
                Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if(requestCode == Constants.SelectFileRequestCode && resultCode == Result.Ok)
            {
                Android.Net.Uri uri = null;
                string fileName = null;
                if(data != null && data.Data != null)
                {
                    uri = data.Data;
                    fileName = AndroidHelpers.GetFileName(ApplicationContext, uri);
                }
                else
                {
                    // camera
                    var root = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory, "bitwarden");
                    var file = new Java.IO.File(root, "temp_camera_photo.jpg");
                    uri = Android.Net.Uri.FromFile(file);
                    fileName = $"photo_{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.jpg";
                }

                if(uri == null)
                {
                    return;
                }
                try
                {
                    using(var stream = ContentResolver.OpenInputStream(uri))
                    using(var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        _messagingService.Send("selectFileResult",
                            new Tuple<byte[], string>(memoryStream.ToArray(), fileName ?? "unknown_file_name"));
                    }
                }
                catch(Java.IO.FileNotFoundException)
                {
                    return;
                }
            }
        }
    }
}
