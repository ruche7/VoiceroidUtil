using System;
using System.IO;
using ruche.voiceroid;

namespace voiceroidTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var factory = new ProcessFactory();
            foreach (var process in factory.Processes)
            {
                if (process.IsRunning)
                {
                    // 自身の名称をWAVEファイル保存してみる
                    process.SetTalkText(process.WindowTitle);
                    process.Save(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "voiceroidTest",
                            process.WindowTitle));
                }
            }
        }
    }
}
