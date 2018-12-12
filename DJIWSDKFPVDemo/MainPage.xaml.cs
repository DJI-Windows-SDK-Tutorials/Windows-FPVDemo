using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DJI.WindowsSDK;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DJIWSDKDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DJIVideoParser.Parser videoParser;
        public WriteableBitmap VideoSource;
        private byte[] decodedDataBuf;
        private object bufLock = new object();

        public MainPage()
        {
            this.InitializeComponent();
            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationEvent;
            //Replace app key with the real key registered. Make sure that the key is matched with your application's package id.
            DJISDKManager.Instance.RegisterApp("app key");
        }

        private async void Instance_SDKRegistrationEvent(SDKRegistrationState state, SDKError resultCode)
        {
            if (resultCode == SDKError.NO_ERROR)
            {
                System.Diagnostics.Debug.WriteLine("Register app successfully.");
                DJISDKManager.Instance.ComponentManager.GetProductHandler(0).ProductTypeChanged += async delegate (object sender, ProductTypeMsg? value)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (value != null && value?.value != ProductType.NONE)
                        {
                            System.Diagnostics.Debug.WriteLine("Aircraft is connected now.");
                            //You can load/display your pages relative to aircraft operations here.
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Aircraft is disconnected now.");
                            //You can hide your pages relative to aircraft operations, or provide users with some aircraft connection tips here.
                        }
                    });
                };
                //You need to get the product's connection state after activating, if you have already connected the aircraft before activate Windows SDK.
                var productType = (await DJISDKManager.Instance.ComponentManager.GetProductHandler(0).GetProductTypeAsync()).value;
                if (productType != null && productType?.value != ProductType.NONE)
                {
                    System.Diagnostics.Debug.WriteLine("Aircraft is connected now.");
                    //You can load/display your pages relative to aircraft operations here.
                }

                //listen video receive data
                if (videoParser == null)
                {
                    videoParser = new DJIVideoParser.Parser();
                    videoParser.Initialize();
                    videoParser.SetVideoDataCallack(0, 0, ReceiveDecodedData);
                    DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
                }

                //listen camera work mode
                DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraWorkModeChanged += async delegate (object sender, CameraWorkModeMsg? value)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (value != null)
                        {
                            ModeTB.Text = value.Value.value.ToString();
                        }
                    });
                };

                //listen video record time
                DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).RecordingTimeChanged += async delegate (object sender, IntMsg? value)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (value != null)
                        {
                            RecordTimeTB.Text = value.Value.value.ToString();
                        }
                    });
                };

            } else
            {
                System.Diagnostics.Debug.WriteLine("SDK register failed, the error is: ");
                System.Diagnostics.Debug.WriteLine(resultCode.ToString());
            }
        }

        //raw data
        void OnVideoPush(VideoFeed sender, [ReadOnlyArray] ref byte[] bytes)
        {
            videoParser.PushVideoData(0, 0, bytes, bytes.Length);
        }

        //decode data
        async void ReceiveDecodedData(byte[] data, int width, int height)
        {
            lock (bufLock)
            {
                if (decodedDataBuf == null)
                {
                    decodedDataBuf = data;
                }
                else
                {
                    if (data.Length != decodedDataBuf.Length)
                    {
                        Array.Resize(ref decodedDataBuf, data.Length);
                    }
                    data.CopyTo(decodedDataBuf.AsBuffer());
                }
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (VideoSource == null || VideoSource.PixelWidth != width || VideoSource.PixelHeight != height)
                {
                    VideoSource = new WriteableBitmap((int)width, (int)height);
                    FPVImage.Source = VideoSource;
                }

                lock (bufLock)
                {
                    decodedDataBuf.AsBuffer().CopyTo(VideoSource.PixelBuffer);
                }
                VideoSource.Invalidate();
            });
        }

        private async void StartShootPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StartShootPhotoAsync();
                if (retCode != SDKError.NO_ERROR)
                {
                    OutputTB.Text = "Failed to shoot photo, result code is " + retCode.ToString();
                } else
                {
                    OutputTB.Text = "Shoot photo successfully";
                }
            }
            else
            {
                OutputTB.Text = "SDK hasn't been activated yet.";
            }
        }

        private async void StartRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StartRecordAsync();
                if (retCode != SDKError.NO_ERROR)
                {
                    OutputTB.Text = "Failed to record video, result code is " + retCode.ToString();
                }
                else
                {
                    OutputTB.Text = "Record video successfully";
                }
            }
            else
            {
                OutputTB.Text = "SDK hasn't been activated yet.";
            }
        }

        private async void StopRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StopRecordAsync();
                if (retCode != SDKError.NO_ERROR)
                {
                    OutputTB.Text = "Failed to stop record video, result code is " + retCode.ToString();
                }
                else
                {
                    OutputTB.Text = "Stop record video successfully";
                }
            }
            else
            {
                OutputTB.Text = "SDK hasn't been activated yet.";
            }
        }

        private async void SetCameraWorkModeToShootPhoto_Click(object sender, RoutedEventArgs e)
        {
            SetCameraWorkMode(CameraWorkMode.SHOOT_PHOTO);
        }

        private void SetCameraModeToRecord_Click(object sender, RoutedEventArgs e)
        {
            SetCameraWorkMode(CameraWorkMode.RECORD_VIDEO);
        }

        private async void SetCameraWorkMode(CameraWorkMode mode)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                CameraWorkModeMsg workMode = new CameraWorkModeMsg
                {
                    value = mode,
                };
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).SetCameraWorkModeAsync(workMode);
                if (retCode != SDKError.NO_ERROR)
                {
                    OutputTB.Text = "Set camera work mode to " + mode.ToString() + "failed, result code is " + retCode.ToString();
                }
            }
            else
            {
                OutputTB.Text = "SDK hasn't been activated yet.";
            }
        }
    }
}
