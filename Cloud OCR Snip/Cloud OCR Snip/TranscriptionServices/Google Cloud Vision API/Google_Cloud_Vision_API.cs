// 必要な外部パッケージ
//   ・Google.Cloud.Vision.V1
//     https://www.nuget.org/packages/Google.Cloud.Vision.V1/

using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    // 文字読み取りサービスの設定データがない場合
                    return null;
                }
                else
                {
                    if ((string)settings["api_endpoint"] != string.Empty)
                    {
                        // APIエンドポイントが設定されている場合
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
                    }
                    return settings;
                }
            }
            set
            {
                if (value != null)
                {
                    if ((string)value["api_endpoint"] != string.Empty)
                    {
                        // 設定データにAPIエンドポイントの設定データが含まれている場合
                        value["api_endpoint"] = Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes((string)value["api_endpoint"])));
                    }
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
            if (settings != null)
            {
                string[] language_hints = ((List<object>)settings["language_hints"]).OfType<string>().ToList().ToArray();
                if (language_hints != Array.Empty<string>())
                {
                    // 言語ヒントが設定されている場合
                    context = new Google.Cloud.Vision.V1.ImageContext();
                    context.LanguageHints.Add(language_hints);
                }
            }
            // 呼び出し設定を準備する
            const int timeout_seconds = 10;
            CallSettings call_settings = CallSettings.FromExpiration(Expiration.FromTimeout(new TimeSpan(0, 0, timeout_seconds)));
            // クライアントを準備する
            Google.Cloud.Vision.V1.ImageAnnotatorClientBuilder builder = new Google.Cloud.Vision.V1.ImageAnnotatorClientBuilder
            {
                JsonCredentials = service_credential
            };
            if ((string)settings["api_endpoint"] != string.Empty)
            {
                // APIエンドポイントが設定されている場合
                byte[] api_endpoint_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["api_endpoint"]));
                if (api_endpoint_bytes == null)
                {
                    // APIエンドポイントの設定データが破損している場合
                    builder.Endpoint = ".googleapis.com";
                }
                else
                {
                    builder.Endpoint = Encoding.UTF8.GetString(api_endpoint_bytes);
                }
            }
            Google.Cloud.Vision.V1.ImageAnnotatorClient client = builder.Build();
            // 画像内の文字を読み取る
            Google.Cloud.Vision.V1.TextAnnotation result = await client.DetectDocumentTextAsync(image_, context, call_settings);
            return result.Text;
        }
    }
}
