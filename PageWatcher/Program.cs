using System;
using System.IO;
using System.Threading;

namespace PageWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            FileSystemWatcher fileWatcher = new FileSystemWatcher();
            fileWatcher.Path = @"C:\nzhs\questioninformation\QuestionLogTemp\";
            fileWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                        | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fileWatcher.Filter = "*.txt";
            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.Changed += FileWatcher_Changed;

            while (true)
            {
                Thread.Sleep(10000);
            }
            
        }

        private static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            
            string fileName = e.Name;
            string timeStamp = DateTime.Now.ToString();
            Console.WriteLine("file modified/created " + fileName + " " + timeStamp);
        }


    }
}
