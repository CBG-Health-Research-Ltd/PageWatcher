using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PageWatcher
{
    class Program
    {
        //The 'globals' below need to be updated for new showcard instructions. So does receiveShowcardLists() and GetShowcardPageList()
        static List<string[]> hls2020ShowcardList;
        static List<string[]> adultY10ShowcardList;
        static List<string[]> childY10ShowcardList;


        static void Main(string[] args)
        {
            receiveShowcardLists();
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

        #region Receiving Showcard lists from external instructions files
        static void receiveShowcardLists()
        {
            //This method could be cleaned up in the future
            hls2020ShowcardList = GetShowcardPageList("HLS2020");
            adultY10ShowcardList = GetShowcardPageList("ADULTY10");
            childY10ShowcardList = GetShowcardPageList("CHILDY10");
        }

        static string[] subStrings;
        static List<string[]> GetShowcardPageList(string survey)
        {
            string User = Environment.UserName;
            string[] ShowcardPageArray = new string[0];
            try
            {
                switch (survey)
                {                 
                    case ("HLS20"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\HLS2020Instructions.txt");
                        break;
                    case ("CHILDY10"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY10ChildInstructions.txt");
                        break;
                    case ("ADULTY10"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY10AdultInstructions.txt");
                        break;
                }
            }
            catch (Exception e)
            {
                //Missing some survey insructions files
                //Do nothing
            }
            finally
            {
                //Continue with app as expected. This allows code that was successfully executed to be stored
                //so that relevant show-card lists still loaded.
            }

            
            List<string[]> shoPageList = new List<string[]>();
            char splitter = ' ';

            for (int i = 0; i < ShowcardPageArray.Length; i++)
            {
                //NOTE: Uses TSSQuestionNum -> RequiredShowcard rleationship. "&QN&\t&SN&"
                ShowcardPageArray[i] = ShowcardPageArray[i].Replace("\t", " ");//Replacing tab with space for ease of processing.
                //Could get rid of the above line if we have space delimited .txt file.
                subStrings = ShowcardPageArray[i].Split(splitter);//Forming array into sub strings so it may be added to list
                shoPageList.Add(subStrings);//Generating the pageNum -> ShoCard list.
            }
            return shoPageList;
        }

        #endregion
    }
}
