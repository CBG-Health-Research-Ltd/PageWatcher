using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            closeFirstInstance();
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

        private static void closeFirstInstance()
        {
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1) System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        //This event firing looks up to see whether questions are showcard, recorded or both. It then applies delays thread.sleeps and double-checks. If a showcard and/or recording
        //question is observed, it stops all moniotring of the QuestionLogTemp folder which is where pageturner.exe sends info to from askia. This prevents askia
        //sending wrong information therefore causing wrong showcard display or recording. It runs a double-check to ensure the expected item is being logged in
        //QuestionLog which is where laptopShowcards reads from.
        private static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            
            string fileName = e.Name;
            string timeStamp = DateTime.Now.ToString();
            string questionType = CheckQuestionType(fileName);

            //Log the txt file from QuestionLogTemp into QuestionLog which is where laptopshowcards reads from.
            //Because file changed events turned off as soon as SC or audio Recording observed, any proceeding questions within 500ms boundary won't be passed to questionlog.


            if (questionType == "showcardANDrecord" || questionType == "showcardOnly" || questionType == "recordOnly")
            {
                Thread.Sleep(100);
                var myFile = File.Create(@"C:\nzhs\questioninformation\QuestionLog\" + fileName);
                myFile.Close();

                //Turn off QuestionLogTemp reading events so any questions after the showcard within StartTimer() bounds won't be logged to QuestionLog
                fileWatcher.Created -= FileWatcher_Changed;
                fileWatcher.Changed -= FileWatcher_Changed;

                //start timer for 500 ms declared in StartTimer function
                StartTimer();
            }
            else //cases for no SC AND/OR record, therefore a logo page question.
            {
                //check again to see if a showcard or record question has been logged after a certain delay by checking latest file in QuestionLogTemp
                //if not then finally log this question
                Thread.Sleep(100);
                string newerFileName = getLatest(@"C:\nzhs\questioninformation\QuestionLogTemp\") + ".txt";//returns the latest observed file after the delay
                questionType = CheckQuestionType(newerFileName);
                if (questionType == "showcardANDrecord" || questionType == "showcardOnly" || questionType == "recordOnly")
                {
                    //exact same as initial if statement. Could be wrapped in a function.. This is the double check
                    Thread.Sleep(100);
                    var myFile = File.Create(@"C:\nzhs\questioninformation\QuestionLog\" + newerFileName );
                    myFile.Close();

                    //Turn off QuestionLogTemp reading events so any questions after the showcard within StartTimer() bounds won't be logged to QuestionLog
                    fileWatcher.Created -= FileWatcher_Changed;
                    fileWatcher.Changed -= FileWatcher_Changed;

                    //start timer for 500 ms declared in StartTimer function
                    StartTimer();
                }
                else
                {
                    //Finally, if after two checks the question is not a record or showcard question, log it. It will be a logo page no record question
                    var myFile = File.Create(@"C:\nzhs\questioninformation\QuestionLog\" + newerFileName);
                    myFile.Close();
                }
            }

            
        }

        private static string getLatest(string directory)//Gets the name of the latest file created/updated in QuestionLogTemp directory.
        {
            string Username = Environment.UserName;
            DirectoryInfo questionDirectory = new DirectoryInfo(directory);
            string latestFile = Path.GetFileNameWithoutExtension(FindLatestFile(questionDirectory).Name);
            return latestFile;

        }

        private static FileInfo FindLatestFile(DirectoryInfo directoryInfo)//Gets file info of latest file updated/created in directory.
        {
            if (directoryInfo == null || !directoryInfo.Exists)
                return null;

            FileInfo[] files = directoryInfo.GetFiles();
            DateTime lastWrite = DateTime.MinValue;
            TimeSpan lastWriteMiliseconds = lastWrite.TimeOfDay;
            FileInfo lastWrittenFile = null;

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime.TimeOfDay > lastWriteMiliseconds)
                {
                    lastWriteMiliseconds = file.LastWriteTime.TimeOfDay;
                    lastWrittenFile = file;
                }
            }
            return lastWrittenFile;
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

        //Reset the events so pagewatcher continues to listen for new questions.
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.Changed += FileWatcher_Changed;
            questionTimer.Enabled = false;
        }

        #region Receiving Showcard lists from external instructions files

        //Sets the lists from instruction files on initialisation of app.
        static void receiveShowcardLists()
        {
            //This method could be cleaned up in the future
            hls2020ShowcardList = GetShowcardPageList("HLS2020");
            adultY10ShowcardList = GetShowcardPageList("ADULTY10");
            childY10ShowcardList = GetShowcardPageList("CHILDY10");
        }


        //Processes the instruction files recieved on initialisation and saves them to a manageable list within application.
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

        //Checks showcard/record status of question. This is how the file added/updated event knows how to log in QuestionLog and then enforce a delay
        //to prevent unexpected question shortcuts being fired by askia.
        static string CheckQuestionType(string inputTxt)
        {
            string questionType = "noShowcardOrRecord";//This is the default for questions not containing showcard or record keyword

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
                    if ((showcardList[i][0] == questionNum) && (showcardList[i][1] != "1") && (showcardList[i][2] == "record"))//Page num/shortcut is first element i.e. 0 index of showcard list entries.
                    {

                        questionType = "showcardANDrecord";//the question shortcut/ID has been found in the showcard look-up/reference sheet.
                        break;
                    }
                    else if ((showcardList[i][0] == questionNum) && (showcardList[i][1] != "1") && (showcardList[i][2] != "record"))//Page num/shortcut is first element i.e. 0 index of showcard list entries.
                    {

                        questionType = "showcardOnly";//the question shortcut/ID has been found in the showcard look-up/reference sheet.
                        break;
                    }
                    else if ((showcardList[i][0] == questionNum) && (showcardList[i][1] == "1") && (showcardList[i][2] == "record"))//Page num/shortcut is first element i.e. 0 index of showcard list entries.
                    {

                        questionType = "recordOnly";//the question shortcut/ID has been found in the showcard look-up/reference sheet.
                        break;
                    }
                    i++;
                }

            }

            return questionType;

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
