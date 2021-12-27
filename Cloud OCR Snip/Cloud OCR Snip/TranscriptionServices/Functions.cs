using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TranscriptionService
{
    public class Functions
    {
        public static readonly string[] TRANSCRIPTION_SERVICES = { "Google Cloud Vision API" }; // 利用できる文字読み取りサービスのリスト

        /// <summary>
        /// サービス名から文字読み取りサービスを検索して、そのインスタンスを返すメソッド
        /// </summary>
        public static Service GetTranscriptionService(string service_name)
        {
            switch (service_name)
            {
                case "Google Cloud Vision API":
                    return new Google_Cloud_Vision_API();
                default:
                    return new Service();
            }
        }

        /// <summary>
        /// 文字読み取りサービスの認証情報を格納するメソッド
        /// </summary>
        public static void SetTranscriptionServiceCredential(string credential_data, bool allow_export)
        {
            Cloud_OCR_Snip.Functions.SetTranscriptionServiceCredential(credential_data, allow_export);
        }

        public const string TRANSCRIPTION_SERVICE_SETTING_SECTION_KEY = "transcription_service_settings"; // 設定データ内で文字読み取りサービス設定セクションの場所を表すキー

        /// <summary>
        /// 文字読み取りサービスの設定を読み込むメソッド
        /// </summary>
        public static Dictionary<string, object> GetTranscriptionServiceSettings(string sub_section_key = "")
        {
            if (sub_section_key != string.Empty)
            {
                // 設定セクションが指定されている場合
                sub_section_key = ":" + sub_section_key;
            }
            return Cloud_OCR_Snip.Functions.GetUserSettings(TRANSCRIPTION_SERVICE_SETTING_SECTION_KEY + sub_section_key);
        }

        /// <summary>
        /// 文字読み取りサービスの設定を保存するメソッド
        /// </summary>
        public static void SetTranscriptionServiceSettings(object setting_value, string sub_section_key = "")
        {
            if (sub_section_key != string.Empty)
            {
                // 設定セクションが指定されている場合
                sub_section_key = ":" + sub_section_key;
            }
            Cloud_OCR_Snip.Functions.SetUserSettings(setting_value, TRANSCRIPTION_SERVICE_SETTING_SECTION_KEY + sub_section_key);
        }

        /// <summary>
        /// データ保護API（DPAPI）でデータを暗号化するメソッド
        /// </summary>
        public static byte[] Protect(byte[] data)
        {
            return Cloud_OCR_Snip.Functions.Protect(data);
        }

        /// <summary>
        /// データ保護API（DPAPI）でデータを復号するメソッド
        /// </summary>
        public static byte[] Unprotect(byte[] data)
        {
            return Cloud_OCR_Snip.Functions.Unprotect(data);
        }

        /// <summary>
        /// BitmapをStreamに変換するメソッド
        /// </summary>
        public static Stream BitmapToStream(Bitmap bitmap)
        {
            // 画像データをストリームに保存して、そのストリームを返す
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <summary>
        /// 既定のウェブブラウザでURLを開くメソッド
        /// </summary>
        public static void AccessWebsite(string url)
        {
            Cloud_OCR_Snip.Functions.AccessWebsite(url);
        }
    }
}
