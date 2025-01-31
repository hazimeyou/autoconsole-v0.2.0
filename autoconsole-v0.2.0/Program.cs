using System;

class Program
{
    static void Main(string[] args)
    {
        // import変数でコンソールからの入力を受け取る
        Console.Write("Enter your input: ");
        string import = Console.ReadLine();

        // output変数にコンソールへの出力を格納
        string output = ProcessInput(import);

        // 結果を表示
        Console.WriteLine("Output: " + output);
    }

    static string ProcessInput(string input)
    {
        // 入力に対して何か処理を行う
        // ここでは例として入力文字列を逆順にする
        char[] charArray = input.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
}
