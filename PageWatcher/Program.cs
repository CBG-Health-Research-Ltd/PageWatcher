using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace PageWatcher
{
    class Program
    {
        //The 'globals' below need to be updated for new showcard instructions. So does receiveShowcardLists() and GetShowcardPageList()
        static List<string[]> adultY13ShowcardList;
        static List<string[]> childY13ShowcardList;
        static List<string[]> nzcvsy7ShowcardList;
        static List<string[]> ppmy7ShowcardList;
        static List<string[]> adultY14ShowcardList;
        static List<string[]> childY14ShowcardList;
        static System.Timers.Timer questionTimer;
        static string currentQuestion = null;
        static FileSystemWatcher fileWatcher = new FileSystemWatcher();
        static List<string> askedQuestions = new List<string>()
                                                {
                                                    "EMPTYFORINIT"
                                                };


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

        //
        private static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            
            string fileName = e.Name;
            string timeStamp = DateTime.Now.ToString();
            string questionType = CheckQuestionType(fileName);
            askedQuestions.Add(fileName);

            List<string> askedQuestionView = askedQuestions; //so can view while debugging (something was going on with VS)

            //Log the txt file from QuestionLogTemp into QuestionLog which is where laptopshowcards reads from.
            //Because file changed events turned off as soon as SC or audio Recording observed, any proceeding questions within 500ms boundary won't be passed to questionlog.

            string previousQuestion = currentQuestion;
            currentQuestion = fileName;

            if (!(currentQuestion == "questionPE03 Y7PPM .txt" && previousQuestion == "questionPE03 Y7PPM .txt"))//Handle weird cases from PPM
            {

                if (questionType == "showcardANDrecord" || questionType == "showcardOnly" || questionType == "recordOnly")
                {


                    Thread.Sleep(100);


                    var myFile = File.Create(@"C:\nzhs\questioninformation\QuestionLog\" + fileName);
                    myFile.Close();


                    if (!(currentQuestion.Contains("PE01_Response") || currentQuestion.Contains("PE02_Response") || currentQuestion.Contains("PE03_Response")
                        || currentQuestion.Contains("PE03 ") || currentQuestion.Contains("PE04_Response")))//handling weird PPM behavior
                    {

                        //Turn off QuestionLogTemp reading events so any questions after the showcard within StartTimer() bounds won't be logged to QuestionLog
                        fileWatcher.Created -= FileWatcher_Changed;
                        fileWatcher.Changed -= FileWatcher_Changed;


                        //start timer for 500 ms declared in StartTimer function
                        StartTimer();
                    }



                }
                else //cases for no SC AND/OR record, therefore a logo page question. Clean unwante stuff (omit) here.
                {
                    if ((fileName.Contains("Y7PPM") && //Handles weird PPM behavior sending loop on answering Q sub Qs each time
                        (fileName.Contains("PE01 ") || fileName.Contains("PE02 ") || fileName.Contains("PE04 ") ||
                        fileName.Contains("PE05 ") || fileName.Contains("PE06 ") || fileName.Contains("PE07 ") || fileName.Contains("PE08 ") ||
                        fileName.Contains("PE09 ") || fileName.Contains("PE10 ") || fileName.Contains("PE11 ") || fileName.Contains("PE12 ") ||
                        fileName.Contains("PE13 ") || fileName.Contains("PE14 ") || fileName.Contains("PE15 ") || fileName.Contains("PE16 ") ||
                        fileName.Contains("PE17 ") || fileName.Contains("PE18 ") || fileName.Contains("PE19 ") || fileName.Contains("PE20 ")))
                        
                        ||


                        (fileName.Contains("MD 1.02 section")))


                    {
                        //Do nothing, i.e. do not log to QuestionLog
                    }
                    else
                    {
                        var myFile = File.Create(@"C:\nzhs\questioninformation\QuestionLog\" + fileName);
                        myFile.Close();
                    }

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
            questionTimer = new System.Timers.Timer(10);

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
            adultY13ShowcardList = GetShowcardPageList("ADULTY13");
            childY13ShowcardList = GetShowcardPageList("CHILDY13");
            nzcvsy7ShowcardList = GetShowcardPageList("NZCVSY7");
            ppmy7ShowcardList = GetShowcardPageList("PPMY7");
            adultY14ShowcardList = GetShowcardPageList("ADULTY14");
            childY14ShowcardList = GetShowcardPageList("CHILDY14");
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
                    case ("CHILDY13"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY13ChildInstructions.txt");
                        break;
                    case ("ADULTY13"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY13AdultInstructions.txt");
                        break;
                    case ("NZCVSY7"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZCVSY7Instructions.txt", Encoding.Default);
                        break;
                    case ("PPMY7"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\PPMY7Instructions.txt", Encoding.Default);
                        break;
                    case ("CHILDY14"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY14ChildInstructions.txt");
                        break;
                    case ("ADULTY14"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY14AdultInstructions.txt");
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
                case ("nha13"):
                    showcardList = adultY13ShowcardList;
                    break;
                case ("nhc13"):
                    showcardList = childY13ShowcardList;
                    break;
                case ("y7cvs"):
                    showcardList = nzcvsy7ShowcardList;
                    break;
                case ("y7ppm"):
                    showcardList = ppmy7ShowcardList;
                    break;
                case ("nha14"):
                    showcardList = adultY14ShowcardList;
                    break;
                case ("nhc14"):
                    showcardList = childY14ShowcardList;
                    break;

            }
            return showcardList;
        }

        #endregion

    }
}
