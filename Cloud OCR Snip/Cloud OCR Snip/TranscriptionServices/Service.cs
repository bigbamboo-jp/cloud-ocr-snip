using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TranscriptionService
{
    /// <summary>
    /// それぞれの文字読み取りサービスの基底クラス
    /// </summary>
    public class Service
    {
        /// <summary>
        /// 文字読み取りサービスの説明
        /// </summary>
        internal virtual string Explanation { get; }

        /// <summary>
        /// 文字読み取りサービスの設定データ
        /// インポート/エクスポート時に暗号化されていないデータを入出力するために使用される。
        /// ※設定データを暗号化しない場合はオーバーライド不要
        /// </summary>
        internal virtual Dictionary<string, object> Settings
        {
            get
            {
                return Functions.GetTranscriptionServiceSettings();
            }
            set
            {
                Functions.SetTranscriptionServiceSettings(value);
            }
        }

        /// <summary>
        /// 初期設定ページを返すメソッド
        /// </summary>
        internal virtual Page GetInitialSettingPage()
        {
            return null;
        }

        /// <summary>
        /// 認証情報の設定ページを返すメソッド
        /// </summary>
        internal virtual Page GetCredentialSettingsPage(bool initial_setting)
        {
            return null;
        }

        /// <summary>
        /// オプション設定ページを返すメソッド
        /// ※該当するページがない場合はオーバーライド不要
        /// </summary>
        internal virtual Page GetOptionSettingPage(bool initial_setting)
        {
            return null;
        }

        /// <summary>
        /// 指定された画像内の文字を読み取るメソッド
        /// </summary>
        internal virtual async Task<string> DetectText(System.Drawing.Bitmap image, string service_credential)
        {
            return await Task.Run(() => { return (string)null; });
        }
    }
}
