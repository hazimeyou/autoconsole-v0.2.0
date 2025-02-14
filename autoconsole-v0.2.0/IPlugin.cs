public interface IPlugin
{
    string Name { get; }   // プラグイン名
    string Trigger { get; } // 実行トリガー（例: "エラー" など）
    string Type { get; }    // "send"（入力） or "output"（出力監視）

    void Execute(string input, Action<string> sendCommand);
}
