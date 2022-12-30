// 必要な外部パッケージ
//   ・Google.Cloud.Vision.V1
//     https://www.nuget.org/packages/Google.Cloud.Vision.V1/

using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TranscriptionService
{
    /// <summary>
    /// 文字読み取りサービス：Google Cloud Vision API
    /// </summary>
    internal class Google_Cloud_Vision_API : Service
    {
        /// <summary>
        /// 文字読み取りサービスの説明
        /// </summary>
        internal override string Explanation
        {
            get
            {
                return (string)Application.Current.FindResource("google_cloud_vision_api/explanation");
            }
        }

        /// <summary>
        /// 文字読み取りサービスの設定データ
        /// インポート/エクスポート時に暗号化されていないデータを入出力するために使用される。
        /// </summary>
        internal override Dictionary<string, object> Settings
        {
            get
            {
                Dictionary<string, object> settings = Functions.GetTranscriptionServiceSettings();
                if (settings == null)
                {
                    // エクスポートする設定データがnullの場合
                    return null;
                }
                else
                {
                    byte[] api_endpoint_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["api_endpoint"]));
                    if (api_endpoint_bytes == null)
                    {
                        // APIエンドポイントの設定データが破損している場合
                        settings["api_endpoint"] = ".googleapis.com";
                    }
                    else
                    {
                        settings["api_endpoint"] = Encoding.UTF8.GetString(api_endpoint_bytes);
                    }
                    return settings;
                }
            }
            set
            {
                if (value != null)
                {
                    // null以外の設定データがインポートされた場合
                    value["api_endpoint"] = Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes((string)value["api_endpoint"])));
                }
                Functions.SetTranscriptionServiceSettings(value);
            }
        }

        /// <summary>
        /// 初期設定ページを返すメソッド
        /// </summary>
        internal override Page GetInitialSettingPage()
        {
            return new Google_Cloud_Vision_API_InitialSetting_Page1(initial_setting: true);
        }

        /// <summary>
        /// 認証情報の設定ページを返すメソッド
        /// </summary>
        internal override Page GetCredentialSettingsPage(bool initial_setting)
        {
            return new Google_Cloud_Vision_API_InitialSetting_Page2(initial_setting: initial_setting);
        }

        /// <summary>
        /// オプション設定ページを返すメソッド
        /// </summary>
        internal override Page GetOptionSettingPage(bool initial_setting)
        {
            return new Google_Cloud_Vision_API_InitialSetting_Page4(initial_setting: initial_setting);
        }

        /// <summary>
        /// 指定された画像内の文字を読み取るメソッド
        /// </summary>
        internal override async Task<string> DetectText(System.Drawing.Bitmap image, string service_credential)
        {
            // BitmapをGoogle.Cloud.Vision.V1.Imageに変換する
            using Stream image_stream = Functions.BitmapToStream(image);
            Google.Cloud.Vision.V1.Image image_ = Google.Cloud.Vision.V1.Image.FromStream(image_stream);
            // 必要であればコンテキストを準備する
            Google.Cloud.Vision.V1.ImageContext context = null;
            Dictionary<string, object> settings = Functions.GetTranscriptionServiceSettings();
            string[] language_hints = ((List<object>)settings["language_hints"]).OfType<string>().ToList().ToArray();
            if (language_hints != Array.Empty<string>())
            {
                // 言語ヒントが設定されている場合
                context = new Google.Cloud.Vision.V1.ImageContext();
                context.LanguageHints.Add(language_hints);
            }
            // 呼び出し設定を準備する
            const int timeout_seconds = 10;
            CallSettings call_settings = CallSettings.FromExpiration(Expiration.FromTimeout(new TimeSpan(0, 0, timeout_seconds)));
            // クライアントを準備する
            Google.Cloud.Vision.V1.ImageAnnotatorClientBuilder builder = new Google.Cloud.Vision.V1.ImageAnnotatorClientBuilder
            {
                JsonCredentials = service_credential
            };
            byte[] api_endpoint_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["api_endpoint"]));
            if (api_endpoint_bytes == null)
            {
                // APIエンドポイントの設定データが破損している場合
                builder.Endpoint = ".googleapis.com";
            }
            else
            {
                string api_endpoint = Encoding.UTF8.GetString(api_endpoint_bytes);
                if (api_endpoint != string.Empty)
                {
                    // 接続するAPIエンドポイントがユーザーによって設定されている場合
                    builder.Endpoint = api_endpoint;
                }
            }
            Google.Cloud.Vision.V1.ImageAnnotatorClient client = builder.Build();
            // 画像内の文字を読み取る
            try
            {
                Google.Cloud.Vision.V1.TextAnnotation result = await client.DetectDocumentTextAsync(image_, context, call_settings);
                if (result == null)
                {
                    // 何も読み取られなかった場合
                    return string.Empty;
                }
                else
                {
                    // 何かが読み取られた場合
                    return result.Text;
                }
            }
            catch (RpcException ex)
            {
                // 読み取り処理に失敗した場合
                Assembly executing_assembly = Assembly.GetExecutingAssembly();
                if (ex.StatusCode == StatusCode.Unavailable)
                {
                    MessageBox.Show((string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_message_due_to_unavailable"), (string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_title_due_to_unavailable") + " - " + executing_assembly.GetName().Name);
                }
                else if (ex.StatusCode == StatusCode.Unauthenticated)
                {
                    MessageBox.Show((string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_message_due_to_unauthenticated"), (string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_title_due_to_unauthenticated") + " - " + executing_assembly.GetName().Name);
                }
                else if (ex.StatusCode == StatusCode.PermissionDenied)
                {
                    MessageBox.Show((string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_message_due_to_permissiondenied"), (string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_title_due_to_permissiondenied") + " - " + executing_assembly.GetName().Name);
                }
                else if (ex.StatusCode == StatusCode.Unimplemented)
                {
                    MessageBox.Show((string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_message_due_to_unimplemented"), (string)Application.Current.FindResource("google_cloud_vision_api/other/transcription_error_title_due_to_unimplemented") + " - " + executing_assembly.GetName().Name);
                }
                else
                {
                    throw ex;
                }
                return null;
            }
        }
    }
}
