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
using DJIVideoParser;
using System.Threading.Tasks;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DJIWSDKDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DJIVideoParser.Parser videoParser;

        public MainPage()
        {
            this.InitializeComponent();
            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationEvent;
            //Replace app key with the real key registered. Make sure that the key is matched with your application's package id.
            DJISDKManager.Instance.RegisterApp("app key");
        }
        //Callback of SDKRegistrationEvent
        private async void Instance_SDKRegistrationEvent(SDKRegistrationState state, SDKError resultCode)
        {
            if (resultCode == SDKError.NO_ERROR)
            {
                System.Diagnostics.Debug.WriteLine("Register app successfully.");

                //Must in UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //Raw data and decoded data listener
                    if (videoParser == null)
                    {
                        videoParser = new DJIVideoParser.Parser();
                        videoParser.Initialize(delegate (byte[] data)
                        {
                            //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
                            return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
                        });
                        //Set the swapChainPanel to display and set the decoded data callback.
                        videoParser.SetSurfaceAndVideoCallback(0, 0, swapChainPanel, ReceiveDecodedData);
                        DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
                    }
                    //get the camera type and observe the CameraTypeChanged event.
                    DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += OnCameraTypeChanged;
                    var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
                    OnCameraTypeChanged(this, type.value);
                });

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SDK register failed, the error is: ");
                System.Diagnostics.Debug.WriteLine(resultCode.ToString());
            }
        }

        //raw data
        void OnVideoPush(VideoFeed sender, byte[] bytes)
        {
            videoParser.PushVideoData(0, 0, bytes, bytes.Length);
        }

        //Decode data. Do nothing here. This function would return a bytes array with image data in RGBA format.
        async void ReceiveDecodedData(byte[] data, int width, int height)
        {
        }

        //We need to set the camera type of the aircraft to the DJIVideoParser. After setting camera type, DJIVideoParser would correct the distortion of the video automatically.
        private void OnCameraTypeChanged(object sender, CameraTypeMsg? value)
        {
            if (value != null)
            {
                switch (value.Value.value)
                {
                    case CameraType.MAVIC_2_ZOOM:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Zoom);
                        break;
                    case CameraType.MAVIC_2_PRO:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Pro);
                        break;
                    default:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Others);
                        break;
                }

            }
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
