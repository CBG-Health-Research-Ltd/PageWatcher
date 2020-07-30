using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Timers;

namespace PageWatcher
{
    class Program
    {
        //The 'globals' below need to be updated for new showcard instructions. So does receiveShowcardLists() and GetShowcardPageList()
        static List<string[]> hls2020ShowcardList;
        static List<string[]> adultY10ShowcardList;
        static List<string[]> childY10ShowcardList;
        static System.Timers.Timer questionTimer;
        static FileSystemWatcher fileWatcher = new FileSystemWatcher();


        static void Main(string[] args)
        {
            receiveShowcardLists();          
            fileWatcher.Path = @"C:\nzhs\questioninformation\QuestionLogTemp\";
            fileWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                        | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fileWatcher.Filter = "*.txt";
            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.Changed += FileWatcher_Changed;

            while (true)
            {
                Thread.Sleep(10000);//0 cpu usage 
            }
            
        }

        private static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            
            string fileName = e.Name;
            string timeStamp = DateTime.Now.ToString();
            bool showcardExists = CheckShowcardExists(fileName);
            Console.WriteLine("file modified/created " + fileName + " " + timeStamp + " Showcard exists: " + showcardExists.ToString());

            //Log the txt file from QuestionLogTemp into QuestionLog which is where laptopshowcards reads from.
            //Because file changed events turned off as soon as SC observed, any proceeding questions within 500ms boundary won't be passed to questionlog.
            var myFile = File.Create(@"C:\nzhs\questioninformation\QuestionLog\" + fileName);
            myFile.Close();

            if (showcardExists == true)
            {

                
                //Turn off QuestionLogTemp reading events so any questions after the showcard within StartTimer() bounds won't be logged to QuestionLog
                fileWatcher.Created -= FileWatcher_Changed;
                fileWatcher.Changed -= FileWatcher_Changed;

                //start timer for 500 ms declared in StartTimer function
                StartTimer();

            }

            
        }

        private static void StartTimer()
        {
            // 500ms interval
            questionTimer = new System.Timers.Timer(500);

            // Hook up the Elapsed event for the timer. 
            questionTimer.Elapsed += OnTimedEvent;

            questionTimer.AutoReset = false;
            questionTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.Changed += FileWatcher_Changed;
            questionTimer.Enabled = false;
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

        #region Getting Corresponding Showcard

        static bool CheckShowcardExists(string inputTxt)
        {
            bool showcardExists = false;//false by default for non-existent showcards

            //Obtain the question number and then find the corresponding showcard from look up.
            if (string.Equals(inputTxt.Substring(0, 8), "question", StringComparison.CurrentCultureIgnoreCase)) //question<shortcut/ID> <survey_name> format expected
            {
                //String processing to split shortcut/ID from survey name
                char Qsplitter = ' ';
                string[] subStrings = inputTxt.Split(Qsplitter);
                string questionNum = subStrings[0].Substring(8);
                string surveyInfo = subStrings[1];
                int i = 0;
                
                string surveyType = surveyInfo.ToLower();
                List<string[]> showcardList = getShowcardList(surveyType);
                while (i < showcardList.Count)
                {
                    if ((showcardList[i])[0] == questionNum)//Page num/shortcut is first element i.e. 0 index of showcard list entries.
                    {

                        showcardExists = true;//the question shortcut/ID has been found in the showcard look-up/reference sheet.
                        break;
                    }
                    i++;
                }

            }

            return showcardExists;

        }


        //Two functions below must be changed to accomodate new surveys, also the global variable list instantiation
        //must be updated so that showcard reference lists are generated upon application launch.
        static List<string[]> getShowcardList(string survey)
        {
            survey = survey.ToLower();
            List<string[]> showcardList = new List<string[]>();
            switch (survey)
            {
                case ("hls20"): //THESE NEED TO BE UPDATED IN THE SAME FORMAT FOR HLS AND NZCVS!!!!
                    showcardList = hls2020ShowcardList;
                    break;
                case ("nha10"):
                    showcardList = adultY10ShowcardList;
                    break;
                case ("nhc10"):
                    showcardList = childY10ShowcardList;
                    break;

            }
            return showcardList;
        }

        #endregion

    }
}
