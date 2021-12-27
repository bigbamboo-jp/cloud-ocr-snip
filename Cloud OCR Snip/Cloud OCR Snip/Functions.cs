using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// アプリケーション全体で使用する機能を集約したクラス
    /// </summary>
    public class Functions
    {
        // DLLのインポート
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int modKey, int vKey);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public static bool displaying_shoot; // Shootを表示しているかのフラグ

        /// <summary>
        /// クリップボードから画像を取得してその画像で文字読み取りをするメソッド
        /// </summary>
        public static async Task<bool> DetectScreenshotText()
        {
            bool live_capture_mode = (bool)GetUserSettings()["live_capture_mode"];

            // アクティブなウィンドウを取得する
            IntPtr foreground_window = GetForegroundWindow();
            // アクティブなウィンドウにTopmostが設定されているか確認する
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TOPMOST = 0x0008;
            IntPtr exStyle = GetWindowLong(foreground_window, GWL_EXSTYLE);
            bool foreground_window_topmost_setting = ((int)exStyle & WS_EX_TOPMOST) == WS_EX_TOPMOST;
            // アクティブなウィンドウにTopmostが設定されていた場合（ライブ撮影方式の場合）
            Window window = null;
            if (foreground_window_topmost_setting == true && live_capture_mode == true)
            {
                // 新しくウィンドウを表示してこのアプリケーションにフォーカスを当てる
                window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    Height = 0,
                    Width = 0,
                    ShowInTaskbar = false
                };
                window.Show();
                window.Activate();
            }

            // 切り取るスクリーンショットを準備する（切り取り方式の場合）
            BitmapSource background_image = null;
            if (live_capture_mode == false)
            {
                // スクリーンショットを撮影する
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromControl(new System.Windows.Forms.Form());
                System.Drawing.Bitmap image = new System.Drawing.Bitmap(screen.Bounds.Width, screen.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                // BitmapSourceに変換する
                using System.Drawing.Graphics graph = System.Drawing.Graphics.FromImage(image);
                graph.CopyFromScreen(new System.Drawing.Point(), new System.Drawing.Point(), image.Size);
                IntPtr hbitmap = image.GetHbitmap();
                background_image = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(hbitmap);
            }
            // 画面を撮影して（ライブ撮影方式の場合）、使用する範囲を切り取る
            Shoot screen_shooting_window = new Shoot(background_image);
            displaying_shoot = true;
            bool shot = (bool)screen_shooting_window.ShowDialog();
            displaying_shoot = false;

            // フォーカス確保用のウィンドウを閉じる（表示した場合）
            if (window != null)
            {
                window.Close();
            }

            if (shot == true)
            {
                await Result_Show(screen_shooting_window.image, data_source: 1);
            }
            return shot == false;
        }

        public static readonly string[] TRANSCRIPTION_SERVICES = TranscriptionService.Functions.TRANSCRIPTION_SERVICES; // 文字読み取りサービスのリスト

        /// <summary>
        /// サービス名から文字読み取りサービスを検索して、そのインスタンスを返すメソッド
        /// </summary>
        public static TranscriptionService.Service GetTranscriptionService(string service_name)
        {
            return TranscriptionService.Functions.GetTranscriptionService(service_name);
        }

        /// <summary>
        /// 指定されたサービスで画像内の文字を読み取って、その結果を返すメソッド
        /// </summary>
        public static async Task<string> DetectText(TranscriptionService.Service service, System.Drawing.Bitmap image)
        {
            string transcription_service_credential = GetTranscriptionServiceCredential();
            if (transcription_service_credential == string.Empty)
            {
                // 文字読み取りサービスの初期設定が済んでいない場合
                Settings_Show(show_transcription_settings: true);
                return null;
            }
            // 画像内の文字を読み取る
            string result = null;
            try
            {
                result = await service.DetectText(image, transcription_service_credential);
            }
            catch (Exception)
            {
                MessageBox.Show((string)Application.Current.FindResource("other/transcription_error_message"), (string)Application.Current.FindResource("other/transcription_error_title"));
            }
            return result;
        }

        // 設定ファイルのパス
        public static string config_file_path
        {
            get
            {
                if (Directory.Exists(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "AppData")) == true)
                {
                    // プログラムフォルダ内に「AppData」フォルダがある場合
                    return Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "AppData", "config.json");
                }
                else
                {
                    // プログラムフォルダ内に「AppData」フォルダがない場合
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name, "config.json");
                }
            }
        }
        public const string CONFIG_FILE_FILE_TYPE = "Cloud OCR Snip Configuration File (Format Version 1)"; // 設定ファイルのファイルタイプ [製品名を含む]
        public const string USER_SETTING_SECTION_KEY = "data:user_settings"; // 設定データ内でユーザー設定セクションの場所を表すキー

        // 初期状態の設定データ
        public static readonly Dictionary<string, object> INITIAL_CONFIGURATION_DATA = new Dictionary<string, object>
        {
            {
                "file_type", CONFIG_FILE_FILE_TYPE
            },
            {
                "data", new Dictionary<string, object>
                {{"user_settings", new Dictionary<string, object>
                    {
                        { "hotkey1_mainkey", 62 },
                        { "hotkey1_modifierkey", 5 },
                        { "hotkey2_mainkey", 47 },
                        { "hotkey2_modifierkey", 12 },
                        { "language", "" },
                        { "live_capture_mode", false },
                        { "result_data_use_default_setting", true },
                        { "search_service_url", "" },
                        { "transcription_service", "" },
                        { "transcription_service_credential", "" },
                        { "transcription_service_settings", null }
                    }
                }}
            }
        };

        /// <summary>
        /// アプリケーション設定を読み込むメソッド
        /// </summary>
        public static Dictionary<string, object> GetAppSettings(string file_path, string section_key = "")
        {
            // 設定ファイルを読み込む
            string config_file_data = File.ReadAllText(file_path);
            object app_settings = JsonConvert_DeserializeObject(JToken.Parse(config_file_data));
            if (app_settings.GetType().IsGenericType == true)
            {
                // デシリアライズしたデータがジェネリックタイプの場合
                if (app_settings.GetType().GetGenericTypeDefinition() != typeof(Dictionary<,>))
                {
                    // デシリアライズしたデータがディクショナリ以外の場合
                    return null;
                }
            }
            else
            {
                // デシリアライズしたデータがジェネリックタイプ以外の場合
                return null;
            }
            if (section_key != string.Empty)
            {
                // 返す設定セクションの指定がある場合
                List<string> keys = section_key.Split(":").ToList();
                foreach (string key in keys)
                {
                    // 指定された設定セクションを取り出す
                    app_settings = ((Dictionary<string, object>)app_settings)[key];
                }
            }
            return (Dictionary<string, object>)app_settings;
        }

        /// <summary>
        /// JSON形式のデータを再帰的にデシリアライズするメソッド
        /// </summary>
        public static object JsonConvert_DeserializeObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    // 最深層まで再帰処理でデータを取り出す
                    return token.Children<JProperty>()
                                .ToDictionary(prop => prop.Name,
                                              prop => JsonConvert_DeserializeObject(prop.Value));

                case JTokenType.Array:
                    return token.Select(JsonConvert_DeserializeObject).ToList();

                case JTokenType.Integer:
                    // 数値は可能な限りInt32型で返す
                    try
                    {
                        return Convert.ToInt32(((JValue)token).Value); // int
                    }
                    catch (OverflowException)
                    {
                        // データがInt32の最大値を超える場合
                        return ((JValue)token).Value; // long
                    }

                default:
                    return ((JValue)token).Value;
            }
        }

        /// <summary>
        /// アプリケーション設定を保存するメソッド
        /// </summary>
        public static void SetAppSettings(object setting_value, string file_path, string section_key = "")
        {
            Dictionary<string, object> app_settings;
            if (section_key == string.Empty)
            {
                // 更新する設定セクションが指定されていない場合
                app_settings = (Dictionary<string, object>)setting_value;
            }
            else
            {
                // 更新する設定セクションが指定されている場合
                app_settings = GetAppSettings(file_path);
                Dictionary<string, object> current_section = app_settings;
                List<string> keys = section_key.Split(":").ToList();
                foreach (string key in keys.GetRange(0, keys.Count - 1))
                {
                    // 指定された設定セクションの親セクションを取り出す
                    current_section = (Dictionary<string, object>)current_section[key];
                }
                // 指定されたセクションにデータを入れ込む
                current_section[keys[keys.Count - 1]] = setting_value;
            }
            // 設定データをシリアライズして保存する
            string config_file_data = JsonConvert.SerializeObject(app_settings, Formatting.Indented);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file_path));
                File.WriteAllText(file_path, config_file_data);
            }
            catch (UnauthorizedAccessException)
            {
                // 書き込み権限がない場合
                if (MessageBox.Show((string)Application.Current.FindResource("other/file_write_permission_error_message"), (string)Application.Current.FindResource("other/file_write_permission_error_title"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // ユーザーが指示した場合に管理者権限で再起動する
                    RestartApplication(run_as: true);
                }
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// ユーザー設定を読み込むメソッド
        /// </summary>
        public static Dictionary<string, object> GetUserSettings(string sub_section_key = "")
        {
            if (sub_section_key != string.Empty)
            {
                // 設定セクションが指定されている場合
                sub_section_key = ":" + sub_section_key;
            }
            if (File.Exists(config_file_path) == false)
            {
                // 設定ファイルが存在しない場合は新しく作成する
                Dictionary<string, object> configuration_data = new Dictionary<string, object>(INITIAL_CONFIGURATION_DATA);
                ((Dictionary<string, object>)((Dictionary<string, object>)configuration_data["data"])["user_settings"])["search_service_url"] = Convert.ToBase64String(Protect(Encoding.UTF8.GetBytes(DEFAULT_SEARCH_SERVICE_URL)));
                SetAppSettings(configuration_data, config_file_path);
            }
            return GetAppSettings(config_file_path, USER_SETTING_SECTION_KEY + sub_section_key);
        }

        /// <summary>
        /// ユーザー設定を保存するメソッド
        /// </summary>
        public static void SetUserSettings(object setting_value, string sub_section_key = "")
        {
            if (sub_section_key != string.Empty)
            {
                // 設定セクションが指定されている場合
                sub_section_key = ":" + sub_section_key;
            }
            if (File.Exists(config_file_path) == false)
            {
                // 設定ファイルが存在しない場合は新しく作成する
                Dictionary<string, object> configuration_data = new Dictionary<string, object>(INITIAL_CONFIGURATION_DATA);
                ((Dictionary<string, object>)((Dictionary<string, object>)configuration_data["data"])["user_settings"])["search_service_url"] = Convert.ToBase64String(Protect(Encoding.UTF8.GetBytes(DEFAULT_SEARCH_SERVICE_URL)));
                SetAppSettings(configuration_data, config_file_path);
            }
            SetAppSettings(setting_value, config_file_path, USER_SETTING_SECTION_KEY + sub_section_key);
        }

        public const string DEFAULT_SEARCH_SERVICE_URL = "https://www.google.com/search?q={0}"; // ウェブ検索サービスのURLのデフォルト設定

        /// <summary>
        /// 文字読み取りサービスの設定を消去するメソッド
        /// </summary>
        public static void ClearTranscriptionServiceSettings()
        {
            ClearTranscriptionServiceCredential();
            SetUserSettings(null, "transcription_service_settings");
            // ※使用する文字読み取りサービスの設定は消去しない
        }

        /// <summary>
        /// 文字読み取りサービスの認証情報を返すメソッド
        /// </summary>
        public static string GetTranscriptionServiceCredential()
        {
            byte[] protected_credential_data = Convert.FromBase64String((string)GetUserSettings()["transcription_service_credential"]);
            if (protected_credential_data.Length == 0)
            {
                // 文字読み取りサービスの認証情報が登録されていない場合
                return string.Empty;
            }
            else
            {
                // 文字読み取りサービスの認証情報が登録されている場合
                byte[] transcription_service_credential_bytes = Unprotect(protected_credential_data);
                if (transcription_service_credential_bytes == null)
                {
                    // データが破損している場合
                    return string.Empty;
                }
                // ヘッダー（エクスポートの可否設定）を削除したデータを返す
                return Encoding.UTF8.GetString(transcription_service_credential_bytes).Substring("#OO#".Length);
            }
        }

        /// <summary>
        /// 文字読み取りサービスの認証情報を格納するメソッド
        /// </summary>
        public static void SetTranscriptionServiceCredential(string credential_data, bool allow_export)
        {
            string prefix;
            if (allow_export == true)
            {
                // エクスポートが許可される場合
                prefix = "#OK#";
            }
            else
            {
                // エクスポートが許可されない場合
                prefix = "#NG#";
            }
            byte[] protected_credential_data = Protect(Encoding.UTF8.GetBytes(prefix + credential_data));
            SetUserSettings(Convert.ToBase64String(protected_credential_data), "transcription_service_credential");
        }

        /// <summary>
        /// 文字読み取りサービスの認証情報をエクスポートするメソッド
        /// </summary>
        public static string ExportTranscriptionServiceCredential()
        {
            byte[] protected_credential_data = Convert.FromBase64String((string)GetUserSettings()["transcription_service_credential"]);
            if (protected_credential_data.Length == 0)
            {
                // 文字読み取りサービスの認証情報が登録されていない場合
                return string.Empty;
            }
            byte[] transcription_service_credential_bytes = Unprotect(protected_credential_data);
            if (transcription_service_credential_bytes == null)
            {
                // データが破損している場合
                return string.Empty;
            }
            string modified_credential_data = Encoding.UTF8.GetString(transcription_service_credential_bytes);
            if (modified_credential_data.Substring(0, "#OO#".Length) == "#OK#")
            {
                // エクスポートが許可される場合
                return modified_credential_data.Substring("#OO#".Length);
            }
            else
            {
                // エクスポートが許可されない場合
                return null;
            }
        }

        /// <summary>
        /// 文字読み取りサービスの認証情報を消去するメソッド
        /// </summary>
        public static void ClearTranscriptionServiceCredential()
        {
            SetUserSettings(Convert.ToBase64String(Array.Empty<byte>()), "transcription_service_credential");
        }

        /// <summary>
        /// データ保護API（DPAPI）でデータを暗号化するメソッド
        /// ソース：https://docs.microsoft.com/dotnet/api/system.security.cryptography.protecteddata.protect
        /// </summary>
        public static byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, GetAdditionalData(), DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        /// <summary>
        /// データ保護API（DPAPI）でデータを復号するメソッド
        /// ソース：https://docs.microsoft.com/dotnet/api/system.security.cryptography.protecteddata.unprotect
        /// </summary>
        public static byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, GetAdditionalData(), DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public static string additional_data_file_path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "additional_data.json"); // 付加データファイルのパス

        /// <summary>
        /// データを暗号化する際に付加するデータを返すメソッド
        /// </summary>
        public static byte[] GetAdditionalData()
        {
            if (File.Exists(additional_data_file_path) == false)
            {
                // 付加データファイルが存在しない場合は新しく作成する
                Dictionary<string, object> additional_data_dictionary = new Dictionary<string, object>
                {
                    { "data", Convert.ToBase64String(Encryption.AESThenHMAC.NewKey()) }
                };
                SetAppSettings(additional_data_dictionary, additional_data_file_path);
            }
            // ファイルから付加データを読み込んで返す
            return Convert.FromBase64String((string)GetAppSettings(additional_data_file_path)["data"]);
        }

        public const int WM_HOTKEY = 0x0312; // ショートカットキーに関するメッセージのID
        public static readonly Dictionary<int, int> HOTKEY_ID = new Dictionary<int, int>() // 各ショートカットキー固有のID（範囲：0x0000～0xbfff）
        {
            {00, 0x0001}, {01, 0x0002}, {10, 0x0010}
        };

        /// <summary>
        /// ショートカットキーを登録するメソッド
        /// </summary>
        public static bool SetUpHotkey(IntPtr window_handle, int hotkey_id, ModifierKeys modifier_key, Key main_key)
        {
            bool result = RegisterHotKey(window_handle, hotkey_id, (int)modifier_key, KeyInterop.VirtualKeyFromKey(main_key));
            return result == false;
        }

        /// <summary>
        /// ショートカットキーの登録を解除するメソッド
        /// </summary>
        public static bool DisableHotkey(IntPtr window_handle, int hotkey_id)
        {
            bool result = UnregisterHotKey(window_handle, hotkey_id);
            return result == false;
        }

        public static Window product_information_window; // 製品情報ウィンドウのインスタンス

        /// <summary>
        /// 製品情報ウィンドウを表示するメソッド
        /// </summary>
        public static void ProductInformation_Show()
        {
            if (product_information_window != null)
            {
                // 既に表示している場合は新しく表示せず、既存のウィンドウを最前面に移動する
                if (product_information_window.IsLoaded == true)
                {
                    product_information_window.Activate();
                    return;
                }
            }
            product_information_window = new ProductInformation();
            product_information_window.Show();
        }

        public static Window settings_window; // 設定ウィンドウのインスタンス

        /// <summary>
        /// 設定ウィンドウを表示するメソッド
        /// </summary>
        public static void Settings_Show(bool show_transcription_settings = false)
        {
            if (settings_window != null)
            {
                // 既に表示している場合は新しく表示せず、既存のウィンドウを最前面に移動する
                if (settings_window.IsLoaded == true)
                {
                    if (show_transcription_settings == true)
                    {
                        // オプションが有効である場合に、表示するタブを文字読み取りタブに変更する
                        Settings.static_transcription_settings_tabitem.IsSelected = true;
                    }
                    settings_window.Activate();
                    return;
                }
            }
            settings_window = new Settings();
            if (show_transcription_settings == true)
            {
                // オプションが有効である場合に、表示するタブを文字読み取りタブに変更する
                Settings.static_transcription_settings_tabitem.IsSelected = true;
            }
            settings_window.Show();
        }

        /// <summary>
        /// 画像から読み取った文字を結果ウィンドウで表示するメソッド
        /// </summary>
        public static async Task Result_Show(System.Drawing.Bitmap image, int data_source = 0)
        {
            object transcription_service_bytes = Unprotect(Convert.FromBase64String((string)GetUserSettings()["transcription_service"]));
            if (transcription_service_bytes != null)
            {
                transcription_service_bytes = Encoding.UTF8.GetString((byte[])transcription_service_bytes);
            }
            TranscriptionService.Service transcription_service = GetTranscriptionService((string)transcription_service_bytes);
            if (image != null)
            {
                new Result(await DetectText(transcription_service, image), data_source).Show();
            }
        }

        public const string LANGUAGE_INFORMATION_FILE_PATH = "Languages/LanguageInformation.xaml"; // 言語情報ファイルのパス
        public const string LANGUAGE_DATA_FILE_PATH = "Languages/{0}.xaml"; // 言語データファイルのパス
        public const string DEFAULT_LANGUAGE_SETTINGS = "en-US"; // デフォルト言語のリージョンタグ

        /// <summary>
        /// 設定できる言語のリストを返すメソッド
        /// </summary>
        public static Dictionary<string, string> GetAvailableLanguages()
        {
            System.Collections.SortedList available_languages = (System.Collections.SortedList)Application.Current.FindResource("general/available_languages");
            // SortedListをDictionaryに変換して返す
            return available_languages.Keys.Cast<string>().ToDictionary(x => x, x => (string)available_languages[x]);
        }

        /// <summary>
        /// ユーザーが設定した言語の言語データを読み込むメソッド
        /// </summary>
        public static void LoadLanguageData()
        {
            ResourceDictionary language_information_resource_dictionary = new ResourceDictionary
            {
                Source = new Uri(LANGUAGE_INFORMATION_FILE_PATH, UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(language_information_resource_dictionary);
            const string DEFAULT_LANGUAGE = "en-US"; // ユーザーが設定した言語と共に読み込む言語（ユーザーが設定した言語のデータに不足があった場合に表示される）
            ResourceDictionary default_language_resource_dictionary = new ResourceDictionary
            {
                Source = new Uri(string.Format(LANGUAGE_DATA_FILE_PATH, DEFAULT_LANGUAGE), UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(default_language_resource_dictionary);
            string application_language_setting = (string)GetUserSettings()["language"];
            if (application_language_setting == string.Empty)
            {
                // アプリケーションで使用する言語が設定されていない場合
                CultureInfo system_language = CultureInfo.CurrentCulture;
                Dictionary<string, string> available_languages = GetAvailableLanguages();
                if (available_languages.ContainsKey(system_language.Name))
                {
                    // システムの言語がアプリケーションの言語として選択できる場合
                    application_language_setting = system_language.Name;
                }
                else
                {
                    // システムの言語がアプリケーションの言語として選択できない場合
                    application_language_setting = DEFAULT_LANGUAGE_SETTINGS;
                }
                SetUserSettings(application_language_setting, "language");
            }
            if (application_language_setting != DEFAULT_LANGUAGE)
            {
                // ユーザーが設定した言語とデフォルトの言語が異なる場合
                ResourceDictionary language_resource_dictionary = new ResourceDictionary
                {
                    Source = new Uri(string.Format(LANGUAGE_DATA_FILE_PATH, application_language_setting), UriKind.Relative)
                };
                // デフォルト言語のデータをユーザーが設定した言語のデータで置き換える（双方に存在するリソースのみ）
                Application.Current.Resources.MergedDictionaries.Add(language_resource_dictionary);
            }
        }

        /// <summary>
        /// アプリケーションを再起動するメソッド
        /// </summary>
        public static void RestartApplication(bool run_as = false)
        {
            // 移行先のアプリケーションを起動する
            string args = string.Join("\" \"", Environment.GetCommandLineArgs().Skip(1));
            if (args != string.Empty)
            {
                args = "\"" + args + "\"";
            }
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                Arguments = args,
                UseShellExecute = true
            };
            if (run_as == true)
            {
                // 指定された場合に管理者権限で起動する
                psi.Verb = "RunAs";
            }
            try
            {
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception) { }
            // 移行元のアプリケーションを終了する（移行先を起動できなかった場合でも行う）
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 既定のウェブブラウザでURLを開くメソッド
        /// </summary>
        public static void AccessWebsite(string url)
        {
            // URLの末尾に半角スペースを追加する（一部のURLにおいてそうしないと動かない）
            url += " ";

            // OSコマンドインジェクション（CWE 78）対策
            // 参考：https://www.veracode.com/security/dotnet/cwe-78
            Regex valDesc = new Regex(@"[a-zA-Z0-9\x20]+$");
            if (!valDesc.IsMatch(url))
            {
                return;
            }

            // 既定のウェブブラウザでURLを開く
            Process.Start(new ProcessStartInfo("cmd", string.Format("/c start {0}", url)) { CreateNoWindow = true });
        }

        public const string SETTING_TAKEOVER_FILE_FILE_TYPE = "Cloud OCR Snip Setting Takeover File (Format Version 1)"; // 設定引き継ぎファイルのファイルタイプ [製品名を含む]
        public const string SETTING_TAKEOVER_FILE_EXTENSION = ".costf"; // 設定引き継ぎファイルの拡張子
    }
}
