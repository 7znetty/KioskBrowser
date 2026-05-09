using System;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace KioskBrowser
{
    /// <summary>
    /// アプリケーション設定を保持するクラス
    /// </summary>
    public class AppConfig
    {
        /// <summary>起動時に表示するURL</summary>
        public string StartUrl { get; set; }

        /// <summary>アプリ終了用の暗証番号（4桁の数字文字列）</summary>
        public string ExitPin { get; set; }

        public AppConfig()
        {
            StartUrl = "https://example.com";
            ExitPin = "1234";
        }
    }

    /// <summary>
    /// 実行ファイルと同じフォルダの config.json を読み書きするクラス
    /// </summary>
    public static class ConfigManager
    {
        // 実行ファイルの場所を基準にした設定ファイルパス
        private static readonly string ConfigPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "config.json");

        /// <summary>
        /// 設定ファイルを読み込む。ファイルが存在しない場合はデフォルト設定で新規作成する。
        /// </summary>
        public static AppConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                // 初回起動時：デフォルト設定を書き出す
                var defaultConfig = new AppConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<AppConfig>(json);
                return config ?? new AppConfig();
            }
            catch
            {
                // 読み込み・パース失敗時はデフォルト値を返す
                return new AppConfig();
            }
        }

        /// <summary>
        /// 設定をファイルに書き込む（インデント付きJSON形式）
        /// </summary>
        private static void Save(AppConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch
            {
                // 書き込み失敗は無視（読み取り専用フォルダ等）
            }
        }
    }
}
