using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

// nach logging bzw statistics aufteilen


public class DataLogger : MonoBehaviour
{
    // INSPECTOR 
    [Header("Debug Settings")]
    [Tooltip("true: prints 'start'/ 'stop' of manual logging")]
    public bool DEBUG_LoggingStatus = false;        
    public const bool DEBUG_Statistics = true;             // true: prints statistic calculations (numSaccades, numSaccadesDetected, numCorrectDetected, numFalseDetected, numDoubleDetected, falseRate, avgDelay) 

    [Header("File Settings")]
    [SerializeField]
    [Tooltip("directory path where data should be stored")]
    string DirectoryPath;
    [SerializeField]
    [Tooltip("true: created file with Index 1 will be overwritten by next game run, false: each run creates a new file")]
    bool OverwriteFiles = false;

    [Header("Required fields")]
    public TestScenarioBasic testScenario;
    public SaccadeDetection saccadeDetection;
    public CubeManager cubeManager;

    // FILE PATHS/ NAMES
    string fileType = ".csv";                           // standard file type of benchmark/ logging files: .csv
    
    string benchmark = "benchmark";
    string logging = "logging";
    string result = "results";
    const string loggingFileName = "logFile";           // fix name for loggingfile 
    string benchmarkFileName = "benchmarkFile";         // fix name for benchmarkFile
   // string resultsFileName = "";                        // empty name for resultsFile since depends on level and variable
    string configName = "";                             // empty name for resultsFile since depends on individual settings
    string loggingDirectoryPath;                        // path for loggingDirectory: consists of DirectoryPath."logging"
    string benchmarkDirectoryPath;                      // path for benchmarkDirectory: consists of DirectoryPath."benchmark"
    string resultsDirectoryPath;                        // path for resultsDirectory: consists of DirectoryPath."results"
    
    int fileIndex = 1;                                  // used to determine fileIndex if overwriteFiles false
    string sceneName;
    int logFileNumber;

    string correctSaccadesString = "";


    // FILE DATA

    // (timestamp, speed, acceleration, eyeOpeness, eyeDirectionLocal, eyeOriginLocal, eyeDirectionGlobal, eyeOriginGlobal, eyeDirectionLeftLocal, eyeOriginLeftLocal, eyeDirectionRightLocal, eyeOriginRightLocal, eyeDirectionLeftGlobal, eyeDirectionRightGlobal)
    // received from SaccadeDetection for each EyeData
    List<LogFile> loggingData = new List<LogFile>();
    LogFile[] loggingDataArray;
    List<(int, bool, bool, float, float)> benchmarkData = new List<(int, bool, bool, float, float)>();               // (timestamp, saccadeGroundTruth, saccadeDetected, distanceBetweenCubes, timeSinceOnset)
    (int, bool, bool, float, float)[] benchmarkDataArray;
    String loggingDataString = "";                      // the loggingDataString receives the loggingDataValues & some basic information after the experiment. The String is then written to the loggingFile
    String benchmarkDataString = "";                    // the benchmarkDataString receives the benchmarkDataValues & some basic information (settings) after the experiment. The String is then written to the benchmarkFile
    StringBuilder loggingBuilder = new StringBuilder();
    StringBuilder benchmarkBuilder = new StringBuilder();
    
    const char DELIMITER = ',';                       // x;y --> x in  different tile than y in excel
    const char NEXT_ROW = '\n';

    bool loggingMode = false;        // true when logging is manually activated by pressing 'l'
    bool benchmarkMode = false;             // true during benchmarkMode

    // ANALYSIS
    int numSaccades = 0;                // number of saccades that should have occured (Ground Truth). Depends on number of cubes
    int numSaccadesDetected = 0;        // number of saccades that the algorithm detected during the benchmark
    int numCorrectDetected = 0;         // number of saccades that the algorithm detected which are confirmed by the Ground Truth
    float falseRate;                    // falseDetected / (falseDetected + correctDetected)
    float correctRate_detected;         // correctDetected / detected
    float correctRate_truth;            // correctDetected / numSaccades
    float avgDuration;                  // average duration of detected saccades
    float minDuration = 6000;           // minimal duration of detected saccades (bigger than zero)
    float maxDuration = 0;              // maximal duration of detected saccades
    float avgDelay = 0;                 // average delay of correct detections from saccade onset (Ground Truth)
    float minDelay = 6000;              // minimal delay of correct detections from saccade onset (Ground Truth)
    float maxDelay = 0;                 // maximal delay of correct detections from saccade onset (Ground Truth)

    // HELP FOR ANALYSIS
    bool experimentRunning = false;     // true when start condition met for the first time, false after last cube has been focused. Avoids wrong measurements before/ after benchmark
    int numFalseDetected = 0;           // number of saccades that the algorithm detected but could not be confirmed by the Ground Truth --> false alarm
    int numMissedSaccades = 0;          // number of saccades which should have been detected but have not
    bool saccadeAlreadyStarted = false; // true when the first 'TRUE' value in Ground Truth was found, false when the first 'FALSE' is read again
    float tolerance = 0.0f;
    List<int> saccadeDetectionTimestamps = new List<int>();                 // includes all detections (correct, double, false) delivered from SaccadeDetection. used to categorize detections
    List<int> saccadeStartTimestamps = new List<int>();                     // Ground Truth: official saccade start
    List<int> delayList = new List<int>();
    // list of all delay values (delay: saccadeDetectionTimestamp - saccadeOnsetTimestamp (Ground Truth)
    Dictionary<int, (bool, float)> timestampSaccadeTruthSizeMatches = new Dictionary<int, (bool, float)>();
    int index;
    bool DEBUG_Evaluation = false;
    List<(int, int)> indexNumberOfMissedSaccades = new List<(int, int)>();
    string missedSaccadesString;
    string falseSaccadesString;
    int saccadeStartIndex;
    
    DateTime startTime;


    // SIULATION
    bool simulateInput = false;

    SaccadeDetection.Settings currentSettings;

    // DATA
    int timestamp;
    float speed;
    float acceleration;
    bool groundTruth;
    float eyeOpenessLeft;
    float eyeOpenessRight;
    Vector3 eyeDirectionLocal;
    Vector3 eyeOriginLocal;
    Vector3 eyeDirectionGlobal;
    Vector3 eyeOriginGlobal;
    Vector3 eyeDirectionLeftLocal;
    Vector3 eyeOriginLeftLocal;
    Vector3 eyeDirectionRightLocal;
    Vector3 eyeOriginRightLocal;
    Vector3 eyeDirectionLeftGlobal;
    Vector3 eyeDirectionRightGlobal;
    Vector3 eyeOriginLeftGlobal;
    Vector3 eyeOriginRightGlobal;

    // RESULT FILE
    string[][] table;
    Result[] results;
    string initialization = "";
    insertionType insertion;

    // Start is called before the first frame update
    private void Start()
    {
        // communicate with SaccadeDetection Script
        saccadeDetection.SaccadeDetectionDataAvailable.AddListener(getSaccadeDetectionData);
        saccadeDetection.SaccadeOccured.AddListener(IncreaseSaccadeDetectionCounter);
        benchmarkBuilder.Append(saccadeDetection.SendSettings(NEXT_ROW.ToString(), DELIMITER.ToString()));        // umschreiben? Wieder hierher nehmen?
        benchmarkBuilder.AppendLine(testScenario.AllowedRange.ToString());
        benchmarkMode = saccadeDetection.TestMode;
        currentSettings = saccadeDetection.GetSettings();

        sceneName = SceneManager.GetActiveScene().name;
        sceneName = sceneName.Replace("SaccadeDetectionTestScene", "");
        if (saccadeDetection.SimulateInput)
        {
            Debug.LogWarning("Logging functionality deactivated during Input Simulation");
            extractFileIndex();
            simulateInput = true;
        }
        else if (benchmarkMode)
        {
            Debug.Log("Data is automatically logged in TestMode");
        }
        else
        {
            // standard mode
            Debug.Log("Press 'l' to start/stop logging your data");
        }
    }

    private void Update()
    {
        receiveLoggingStatus();
    }

    private void OnApplicationQuit()
    {
        startTime = DateTime.Now;
        checkDirectoryAndIndex();
        manageLogging();
        manageBenchmark();
        if (simulateInput)
        {
            manageResults();
        }
        Debug.Log("All done in: " + getCurrentTimeDiff() + " ms");
    }

    #region Logging Status
    /// <summary>
    /// checks whether logging button was pressed for de/activation
    /// </summary>
    private void receiveLoggingStatus()
    {
        if (!benchmarkMode && !simulateInput)
        {
            if (Input.GetKeyDown("l") && !loggingMode)
            {
                if (DEBUG_LoggingStatus)
                {
                    Debug.Log("Logging started");
                }
                loggingMode = true;
            }
            else if (Input.GetKeyDown("l") && loggingMode)
            {
                if (DEBUG_LoggingStatus)
                {
                    Debug.Log("Logging ended");
                }
                loggingMode = false;
            }
        }
    }
    #endregion

    #region Logging, Benchmark


    void manageLogging()
    {
        if (!simulateInput)
        {
            if (loggingData.Count > 0)
            {
                writeLoggingDataToString();
                writeStringToFile(loggingDirectoryPath, loggingFileName + fileIndex, loggingDataString);
            }
            else
            {
                Debug.LogWarning("LoggingData is empty");
            }
        }
    }

    void manageBenchmark()
    {
        if ((benchmarkMode || simulateInput) && (benchmarkData.Count > 0))
        {
            checkBenchmarkSize();

            // as above but for benchmark
            calculateStatistics();
            writeBenchmarkDataToString();
            writeStringToFile(benchmarkDirectoryPath, benchmarkFileName + fileIndex, benchmarkDataString);
        }
        else if (benchmarkMode)
        {
            Debug.LogWarning("BenchmarkData is empty");
        }
    }

    void manageResults()
    {
        defineResultsKonfigCode();
        manuallyGetResults();

        // only necessary with 1 file since all equal structure
        // find column
        readResultFile(results[0]);
        int column = manageInsertionType();

        // read & fill each file
        foreach (Result currResultType in results)
        {
            if (!currResultType.name.Equals(results[0].name))
            {
                // first file has already been read
                readResultFile(currResultType);
            }
            insert(currResultType, column);
            writeTableToFile(currResultType.name);
        }
    }
    #endregion

    #region Receive Data
    /// <summary>
    /// externally called from saccadeDetection to send settings information
    /// </summary>
    /// <param name="separateEye"></param>
    /// <param name="speedThreshold"></param>
    /// <param name="sampleThreshold"></param>
    /// <param name="breakThreshold"></param>
    /// <param name="closedEyeThreshold"></param>
    public void SaveSettingsToFile(bool separateEye, int speedThreshold, int accelerationThreshold, int sampleThreshold, float breakThreshold, float closedEyeThreshold, bool simulateInputSetting)
    {
        simulateInput = simulateInputSetting;
        benchmarkBuilder.AppendLine("SETTINGS:" + NEXT_ROW +
        "Saccade Detection Mode:" + DELIMITER + "Separate Eye" + DELIMITER + separateEye + NEXT_ROW +
        "Saccade Detection Thresholds:" + DELIMITER + "Speed" + DELIMITER + speedThreshold + DELIMITER + "Acceleration" + DELIMITER + accelerationThreshold + DELIMITER + "Sample" + DELIMITER + sampleThreshold + DELIMITER + "Break" + DELIMITER + breakThreshold + DELIMITER + "Closed Eye" + DELIMITER + closedEyeThreshold + NEXT_ROW
        + "Simulation: " + DELIMITER + simulateInput + NEXT_ROW
        + "AllowedRange: " + DELIMITER + testScenario.AllowedRange);
    }

    
    void getSaccadeDetectionData()
    {
        // logging data
        if (!simulateInput && (loggingMode || (benchmarkMode && experimentRunning)))
        {
            saccadeDetection.SendLoggingData(out timestamp, out speed, out acceleration, out eyeOpenessLeft, out eyeOpenessRight, out eyeDirectionLocal, out eyeOriginLocal, out eyeDirectionLeftLocal, out eyeOriginLeftLocal, out eyeDirectionRightLocal, out eyeOriginRightLocal);

            cubeManager.SendGlobalLoggingData(out eyeDirectionGlobal, out eyeOriginGlobal, out eyeDirectionLeftGlobal, out eyeDirectionRightGlobal, out eyeOriginLeftGlobal, out eyeOriginRightGlobal);
            // only stored if logging activated or in TestMode
            loggingData.Add(new LogFile(timestamp, speed, acceleration, eyeOpenessLeft, eyeOpenessRight, eyeDirectionLocal, eyeOriginLocal, eyeDirectionGlobal, eyeOriginGlobal, eyeDirectionLeftLocal, eyeOriginLeftLocal, eyeDirectionRightLocal, eyeOriginRightLocal, eyeDirectionLeftGlobal, eyeOriginLeftGlobal, eyeDirectionRightGlobal, eyeOriginRightGlobal));
        }
    }

    // 'normal' logging / individual 'answers' from currently used algorithm
    public void SaveDataToFile(int timestamp, float speed, float acceleration, float eyeOpenessLeft, float eyeOpenessRight, Vector3 eyeDirectionLocal, Vector3 eyeOriginLocal, Vector3 eyeDirectionGlobal, Vector3 eyeOriginGlobal, Vector3 eyeDirectionLeftLocal, Vector3 eyeOriginLeftLocal, Vector3 eyeDirectionRightLocal, Vector3 eyeOriginRightLocal, Vector3 eyeDirectionLeftGlobal, Vector3 eyeDirectionRightGlobal, Vector3 eyeOriginLeftGlobal, Vector3 eyeOriginRightGlobal)
    {
        if (!simulateInput && (loggingMode || (benchmarkMode && experimentRunning)))
        {
            // only stored if logging activated or in TestMode
            loggingData.Add(new LogFile(timestamp, speed, acceleration, eyeOpenessLeft, eyeOpenessRight, eyeDirectionLocal, eyeOriginLocal, eyeDirectionGlobal, eyeOriginGlobal, eyeDirectionLeftLocal, eyeOriginLeftLocal, eyeDirectionRightLocal, eyeOriginRightLocal, eyeDirectionLeftGlobal, eyeOriginLeftGlobal, eyeDirectionRightGlobal, eyeOriginRightGlobal));
        }
    }

    // GroundTruth values (benchmark)
    public void SaveDataToFile(int timestamp, bool saccadeTruth, bool saccadeDet, float saccadeSize, float timeSinceOnset)
    {
        if (!saccadeTruth)
        {
            timeSinceOnset = -1.0f;     // uninteresting value
        }
        if (experimentRunning)
        {
            // only store if experiment started - indicated by focusing start point

            if (!simulateInput)
            {
                benchmarkData.Add((timestamp, saccadeTruth, saccadeDet, saccadeSize, timeSinceOnset));
            }
            else
            {
                // take groundTruth from logFile
                benchmarkData.Add((timestamp, groundTruth, saccadeDet, saccadeSize, timeSinceOnset));
            }
        }
        // has to be done permanent otherwise the last value is missing
        timestampSaccadeTruthSizeMatches.Add(timestamp, (saccadeTruth, saccadeSize));
    }




    #endregion

    #region Manage FileName
    /// <summary>
    /// gets Index from InputFile f.e. .../logFile3.csv --> number = 3
    /// manages benchmarkFileName --> benchmarkFile3_1, benchmarkFile3_2, ..
    /// </summary>
    void extractFileIndex()
    {
        string number = "";
        foreach (char currentChar in testScenario.InputPath)
        {
            if (char.IsDigit(currentChar))
            {
                number += currentChar;
            }
        }
        logFileNumber = Int16.Parse(number);

        if (testScenario.InputPath.Contains("Copy"))
        {
            // change benchmarkName
            benchmarkFileName += logFileNumber + "_modified_";
        }
    }


    void checkFileIndex()
    {
        string completeFilePath; 
        if (!OverwriteFiles)
        {
            if (!simulateInput)
            {
                // check logFile Index
                completeFilePath = loggingDirectoryPath + loggingFileName + fileIndex + fileType;
                while (File.Exists(completeFilePath))
                {
                    fileIndex++;
                    completeFilePath = loggingDirectoryPath + loggingFileName + fileIndex + fileType;
                }
            }
           
            // check benchmarkFile Index
            completeFilePath = benchmarkDirectoryPath + benchmarkFileName + fileIndex + fileType;
            while (File.Exists(completeFilePath))
            {
                fileIndex++;
                completeFilePath = benchmarkDirectoryPath + benchmarkFileName + fileIndex + fileType;
            }
        }
    }

    #endregion

    #region Data To String

    void writeLoggingDataToString()
    {
        loggingBuilder.AppendLine("timestamp [ms]" + DELIMITER + "saccade (truth)" + DELIMITER + "speed [deg/s]" + DELIMITER + "acceleration [deg/s^2]" + DELIMITER + "saccadeSize [deg]" + DELIMITER + "openessLeft" + DELIMITER + "openessRight" + DELIMITER + 
            "eyeDirectionLocal" + DELIMITER + DELIMITER + DELIMITER + "eyeOriginLocal" + DELIMITER + DELIMITER + DELIMITER + "eyeDirectionGlobal" + DELIMITER + DELIMITER + DELIMITER + "eyeOriginGlobal" + DELIMITER + DELIMITER + DELIMITER +
            "eyeDirectionLeftLocal" + DELIMITER + DELIMITER + DELIMITER + "eyeOriginLeftLocal" + DELIMITER + DELIMITER + DELIMITER + "eyeDirectionRightLocal" + DELIMITER + DELIMITER + DELIMITER + "eyeOriginRightLocal" + DELIMITER + DELIMITER + DELIMITER +
            "eyeDirectionLeftGlobal" + DELIMITER + DELIMITER + DELIMITER + "eyeOriginLeftGlobal" + DELIMITER + DELIMITER + DELIMITER + "eyeDirectionRightGlobal" + DELIMITER + DELIMITER + DELIMITER + "eyeOriginRightGlobal");
        
        loggingDataArray = loggingData.ToArray();

        foreach (var values in loggingDataArray)
        {
            loggingBuilder.Append(values.timestamp.ToString());                 // timestamp
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(getSaccadeTruth(values.timestamp).ToString());       // saccadeTruth        gesammelt und nachträglich eingetragen
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.speed.ToString());                     // speed
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.acceleration.ToString());              // acceleration
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(getSaccadeSize(values.timestamp).ToString()); // saccadeSize
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOpenessLeft.ToString());            // eyeOpenessLeft
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOpenessRight.ToString());           // eyeOpenessRight
            loggingBuilder.Append(DELIMITER);

            loggingBuilder.Append(values.eyeDirectionLocal.x.ToString());       // eyeDirectionLocal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLocal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLocal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLocal.x.ToString());          // eyeOrigin
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLocal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLocal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionGlobal.x.ToString());      // eyeDirectionGlobal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionGlobal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionGlobal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginGlobal.x.ToString());         // eyeOriginGlobal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginGlobal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginGlobal.z.ToString());
            loggingBuilder.Append(DELIMITER);

            loggingBuilder.Append(values.eyeDirectionLeftLocal.x.ToString());       // eyeDirectionLeftLocal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLeftLocal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLeftLocal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLeftLocal.x.ToString());          // eyeOriginLeftLocal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLeftLocal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLeftLocal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionRightLocal.x.ToString());      // eyeDirectionRightLocal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionRightLocal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionRightLocal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginRightLocal.x.ToString());        // eyeOriginRightLocal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginRightLocal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginRightLocal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLeftGlobal.x.ToString());     // eyeDirectionLeftGlobal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLeftGlobal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionLeftGlobal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLeftGlobal.x.ToString());         // eyeOriginLeftGlobal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLeftGlobal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginLeftGlobal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionRightGlobal.x.ToString());     // eyeDirectionRightGlobal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionRightGlobal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeDirectionRightGlobal.z.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginRightGlobal.x.ToString());        // eyeOriginRightGlobal
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.Append(values.eyeOriginRightGlobal.y.ToString());
            loggingBuilder.Append(DELIMITER);
            loggingBuilder.AppendLine(values.eyeOriginRightGlobal.z.ToString());
        }
        loggingDataString = loggingBuilder.ToString();
    }

    bool getSaccadeTruth(int saccadeTimestamp)
    {
        (bool, float) saccadeTruthSizeTuple;
        if (timestampSaccadeTruthSizeMatches.TryGetValue(saccadeTimestamp, out saccadeTruthSizeTuple))
        {
            return saccadeTruthSizeTuple.Item1;
        }
        else
        {
            Debug.LogWarning("Given timestamp not in dictionary:  " + saccadeTimestamp);
            return false;
        }
    }

    float getSaccadeSize(int saccadeTimestamp)
    {
        (bool, float) saccadeTruthSizeTuple;
        if (timestampSaccadeTruthSizeMatches.TryGetValue(saccadeTimestamp, out saccadeTruthSizeTuple))
        {
            return saccadeTruthSizeTuple.Item2;
        }
        else
        {
            Debug.LogWarning("Given timestamp not in dictionary:  " + saccadeTimestamp);
            return 0.0f;
        }
    }

    void writeBenchmarkDataToString()
    {

        benchmarkBuilder.AppendLine( "timestamp [ms]" + DELIMITER + "saccade (truth)" + DELIMITER + "saccadeDetected" + DELIMITER + "saccadeSize [deg]" + DELIMITER + "timeSinceOnset [ms]");
        
        foreach (var values in benchmarkDataArray)
        {
            benchmarkBuilder.Append(values.Item1.ToString());
            benchmarkBuilder.Append(DELIMITER);
            benchmarkBuilder.Append(values.Item2.ToString());
            benchmarkBuilder.Append(DELIMITER);
            benchmarkBuilder.Append(values.Item3.ToString());
            benchmarkBuilder.Append(DELIMITER);
            benchmarkBuilder.Append(values.Item4.ToString());
            benchmarkBuilder.Append(DELIMITER);
            benchmarkBuilder.AppendLine(values.Item5.ToString());
        }

        benchmarkBuilder.Append(
            "Saccades" + DELIMITER + numSaccades + NEXT_ROW +
            "Saccades Detected" + DELIMITER + numSaccadesDetected + NEXT_ROW +
            "Correct Detected" + DELIMITER + numCorrectDetected + DELIMITER + "(" + correctSaccadesString + ")" + DELIMITER + "False Detected" + DELIMITER + numFalseDetected + DELIMITER + "(" + falseSaccadesString + ")" + DELIMITER + "Missed Saccades" + DELIMITER + numMissedSaccades + DELIMITER + missedSaccadesString + NEXT_ROW +
            "False Rate" + DELIMITER + falseRate + NEXT_ROW +
            "correct Rate truth" + DELIMITER + correctRate_truth + NEXT_ROW +
            "correct Rate detected" + DELIMITER + correctRate_detected + NEXT_ROW +
            "Delay [ms]" + NEXT_ROW +
            "Avg/ Min/ Max " + DELIMITER + avgDelay + DELIMITER + minDelay + DELIMITER + maxDelay + NEXT_ROW +
            "Saccade Duration [ms]" + NEXT_ROW +
            "Avg/ Min/ Max " + DELIMITER + avgDuration + DELIMITER + minDuration + DELIMITER + maxDuration + NEXT_ROW +
            NEXT_ROW +
            "Note: Delay values only consider correctly detected saccades");

        benchmarkDataString = benchmarkBuilder.ToString();
    }

    #endregion

    #region String To File

    /// <summary>
    /// manages and creates directories and finds the smallest free Index that fits for logging AND benchmark
    /// </summary>
    void checkDirectoryAndIndex()
    {
        if (Directory.Exists(DirectoryPath))
        {
            
            // create scene folder
            DirectoryPath += "\\TestScene" + sceneName + "\\";
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }

            // create logging folder
            loggingDirectoryPath = DirectoryPath + logging + "\\";
            if (!Directory.Exists(loggingDirectoryPath))
            {
                Directory.CreateDirectory(loggingDirectoryPath);
            }

            // create benchmark folder
            benchmarkDirectoryPath = DirectoryPath + benchmark + "\\";
            if (!Directory.Exists(benchmarkDirectoryPath))
            {
                Directory.CreateDirectory(benchmarkDirectoryPath);
            }

            // create results folder
            if (testScenario.InputPath.Contains("Copy"))
            {
                result += "_modified";
                // change directory
            }
            resultsDirectoryPath = DirectoryPath + result + "\\";
            if (!Directory.Exists(resultsDirectoryPath))
            {
                Directory.CreateDirectory(resultsDirectoryPath);
            }

            // create allowedRange folder
            resultsDirectoryPath += "range_" + testScenario.AllowedRange + "\\";
            if (!Directory.Exists(resultsDirectoryPath))
            {
                Directory.CreateDirectory(resultsDirectoryPath);
            }

            checkFileIndex();
        }
        else
        {
            Debug.LogError("Directory Path does not exist: " + DirectoryPath);
        }
    }

    void writeStringToFile(string path, string fileName, string data)
    {
        // create and write file using System.IO.StreamWriter
        using (StreamWriter logFile = new StreamWriter(path + fileName + fileType))
            logFile.Write(data);
    }

    #endregion

    #region Statistics

    void calculateStatistics()
    {
        // go through Ground Truth list
        string detectionValues;
        detectionValues = String.Join(": ", saccadeDetectionTimestamps);
        if (DEBUG_Evaluation)
        {
            Debug.Log("detections: " + detectionValues);
        }
        checkBenchmarkValues();
        calculateFalseDetectionsAndFalseRate();
        calculateAverageDuration();
        calculateDelay();

        if (DEBUG_Statistics)
        {
            missedSaccadesString = String.Join(", ", indexNumberOfMissedSaccades);
            falseSaccadesString = String.Join(", ", saccadeDetectionTimestamps);
            Debug.Log("Saccades: " + numSaccades + "   SaccadesDetected: " + numSaccadesDetected + "  CorrectDetected: " + numCorrectDetected + "   FalseDetected: " + numFalseDetected + " (" + falseSaccadesString + ")" +
                 "   MissedSaccades: " + numMissedSaccades + " (" + missedSaccadesString + ")");
            Debug.Log("AvgDelay: " + avgDelay + "[ms]   AvgSaccadeDuration: " + avgDuration + "[ms]");
        }
    }

    void checkBenchmarkValues()
    {
        benchmarkDataArray = benchmarkData.ToArray();
       
        for (int i = 0; i < benchmarkDataArray.Length; i++)
        {
            if (benchmarkDataArray[i].Item2)
            {
                if (!saccadeAlreadyStarted)
                {
                    numSaccades++;
                    saccadeAlreadyStarted = true;
                    saccadeStartIndex = i;
                    saccadeStartTimestamps.Add(benchmarkDataArray[i].Item1);
                    checkRangeForDetections(i);
                }
            }
            else
            {
                if (saccadeAlreadyStarted)
                {
                    // turned from TRUE to FALSE --> duration
                    calculateDuration(i);
                }
                saccadeAlreadyStarted = false;
            }
        }
    }


    // checks whether a saccade has been detected correctly in the given range
    void checkRangeForDetections(int i)
    {
        // start with smallest possible index
        index = i - testScenario.AllowedRange;
        while (index < i + testScenario.AllowedRange)
        {
            // if saccade still ongoing or "earlier than onset"
            if (benchmarkDataArray.Length > index && benchmarkDataArray[index].Item2 || index < i)
            {
                // check range for detections
                if ((0 <= index) && (index < benchmarkDataArray.Length))
                {
                    if (saccadeDetectionTimestamps.Contains(benchmarkDataArray[index].Item1))
                    {
                        numCorrectDetected++;
                        correctSaccadesString += benchmarkDataArray[index].Item1 + ", ";

                        // manage lists
                        if (index <= i)
                        {
                            // add 0 delay to list (since earlier/ immediately detected)
                            delayList.Add(0);
                        }
                        else
                        {
                            // add delay
                            delayList.Add(benchmarkDataArray[index].Item1 - benchmarkDataArray[i].Item1);
                        }
                        saccadeDetectionTimestamps.Remove(benchmarkDataArray[index].Item1);

                        if (DEBUG_Evaluation)
                        {
                            Debug.Log("delay added: " + (benchmarkDataArray[index].Item1 - benchmarkDataArray[i].Item1));
                            Debug.Log("matching timestamp found, i: " + i);
                        }
                        return;
                    }
                }
            }
            index++;
        }
        numMissedSaccades++;
        indexNumberOfMissedSaccades.Add((numSaccades, benchmarkDataArray[i].Item1));
    }


    void calculateFalseDetectionsAndFalseRate()
    {
        // #falseDetected --> compared to detected at all
        numFalseDetected = numSaccadesDetected - numCorrectDetected;


        // false Rate
        if (numSaccadesDetected == 0)
        {
            falseRate = -1;
            correctRate_detected = 0;
            correctRate_truth = 0;
            if (DEBUG_Statistics)
            {
                Debug.Log("Denominator was zero, no saccade has been detected");
            }
        }
        else
        {
            // falseRate
            // correctRate_detected
            if (numSaccadesDetected == 0)
            {
                falseRate = 0;
                correctRate_detected = 0;
            }
            else
            {
                falseRate = (float)numFalseDetected / (float)(numSaccadesDetected);
                correctRate_detected = (float)numCorrectDetected / (float)(numSaccadesDetected);
            }
           
            // correctRate_truth
            if (numSaccades > 0)
            {
                // standard
                correctRate_truth = (float)numCorrectDetected / (float)(numSaccades);
            }
            else if (numSaccades == 0)
            {
                // there shoould have been no saccade detected
                if (numSaccadesDetected == 0)
                {
                    correctRate_truth = 1;
                }
                else
                {
                    correctRate_truth = 0;
                }
            }
            else
            {
                Debug.LogWarning("numSaccades have a negative value. This should not happen!");
            }

            if (DEBUG_Statistics)
            {
                Debug.Log("falseRate: " + falseRate);
                Debug.Log("correctRate_detected: " + correctRate_detected);
                Debug.Log("correctRate_truth: " + correctRate_truth);
                tolerance = Mathf.Abs(falseRate - ((float)numFalseDetected / (float)(numCorrectDetected + numFalseDetected)));
                if (tolerance > 0.001)
                {
                    Debug.LogWarning("Other falseRate measurement lead to a different result: " + ((numFalseDetected / (float)(numCorrectDetected + numFalseDetected))));
                }
            }
        }
    }


    void calculateDelay()
    {
        foreach (int delay in delayList)
        {
            avgDelay += delay;
            if (delay > maxDelay)
            {
                maxDelay = delay;
            }
            if (minDelay > delay)
            {
                minDelay = delay;
            }
        }
        if (delayList.Count > 0)
        {
            avgDelay /= delayList.Count;
        }
        else
        {
            avgDelay = -1;
        }
    }


    void calculateDuration(int index)
    {
        // now FALSE, once before TRUE --> index-1
        float duration = benchmarkDataArray[index - 1].Item1 - benchmarkDataArray[saccadeStartIndex].Item1;

        // saccade lasted only 1 frame
        if (duration == 0)
        {
            duration = 8;           // regular duration of frame
        }
        if (DEBUG_Evaluation)
        {
            Debug.Log("duration: " + duration);
        }
        avgDuration += duration;
        if (duration > maxDuration)
        {
            maxDuration = duration;
        }
        if (minDuration > duration)
        {
            minDuration = duration;
        }
    }


    void calculateAverageDuration()
    {
        if (numSaccades > 0)
        {
            avgDuration /= numSaccades;
        }
        else
        {
            avgDuration = -1;
        }
    }

    #endregion

    /// <summary>
    /// checks whether first/ last element of benchmark is "too much" and if so gets removed
    /// </summary>
    void checkBenchmarkSize()
    {
        if (simulateInput) return;
        // check if first element has to be removed
        if (benchmarkData[0].Item1 < loggingDataArray[0].timestamp)
        {
            benchmarkData.RemoveAt(0);
        }

        // check if last element has to be removed
        if (benchmarkData[benchmarkData.Count-1].Item1 > loggingDataArray[loggingDataArray.Length-1].timestamp)
        {
            benchmarkData.RemoveAt(benchmarkData.Count-1);
        }
    }

    /// <summary>
    /// returns currentTimeDiff (currentTime - startTime) in ms
    /// </summary>
    public int getCurrentTimeDiff()
    {
        // currentTime and startTime as number for time difference calculation
        // 60 * 1000 * Minute, 1000 * Second, Millisecond
        int currentTimeNumber = 60 * 1000 * DateTime.Now.Minute + 1000 * DateTime.Now.Second + DateTime.Now.Millisecond;
        int startTimeNumber = 60 * 1000 * startTime.Minute + 1000 * startTime.Second + startTime.Millisecond;
        return currentTimeNumber - startTimeNumber;
    }



    #region public methods

    public void SetBenchmarkLogging()
    {
        benchmarkMode = true;
    }

    public void IsExperimentRunning(bool trueFalse)
    {
        experimentRunning = trueFalse;
    }

    /// <summary>
    /// Called by SaccadeDetection when a saccade is detected and stores the current timestamp
    /// </summary>
    /// <param name="timestamp"></param>
    public void IncreaseSaccadeDetectionCounter()
    {
        if (experimentRunning)
        {
            numSaccadesDetected++;
            saccadeDetectionTimestamps.Add(saccadeDetection.GetTimestamp());
        }
    }

    #endregion

    public struct LogFile
    {
        public LogFile(int fileTimestamp, float fileSpeed, float fileAcceleration, float fileEyeOpenessLeft, float fileEyeOpenessRight, Vector3 fileEyeDirectionLocal, Vector3 fileEyeOriginLocal, Vector3 fileEyeDirectionGlobal, Vector3 fileEyeOriginGlobal, Vector3 fileEyeDirectionLeftLocal, Vector3 fileEyeOriginLeftLocal, Vector3 fileEyeDirectionRightLocal, Vector3 fileEyeOriginRightLocal, Vector3 fileEyeDirectionLeftGlobal, Vector3 fileEyeOriginLeftGlobal, Vector3 fileEyeDirectionRightGlobal, Vector3 fileEyeOriginRightGlobal)
        {
            timestamp = fileTimestamp;
            speed = fileSpeed;
            acceleration = fileAcceleration;
            eyeOpenessLeft = fileEyeOpenessLeft;
            eyeOpenessRight = fileEyeOpenessRight;

            // combined
            eyeDirectionLocal = fileEyeDirectionLocal;
            eyeOriginLocal = fileEyeOriginLocal;
            eyeDirectionGlobal = fileEyeDirectionGlobal;
            eyeOriginGlobal = fileEyeOriginGlobal;

            // left, right
            eyeDirectionLeftLocal = fileEyeDirectionLeftLocal;
            eyeOriginLeftLocal = fileEyeOriginLeftLocal;
            eyeDirectionRightLocal = fileEyeDirectionRightLocal;
            eyeOriginRightLocal = fileEyeOriginRightLocal;

            eyeDirectionLeftGlobal = fileEyeDirectionLeftGlobal;
            eyeOriginLeftGlobal = fileEyeOriginLeftGlobal;
            eyeDirectionRightGlobal = fileEyeDirectionRightGlobal;
            eyeOriginRightGlobal = fileEyeOriginRightGlobal;
        }

        public int timestamp { get; set; }
        public float speed { get; set; }
        public float acceleration { get; set; }
        public float eyeOpenessLeft { get; set; }
        public float eyeOpenessRight { get; set; }

        // saccadeTruth and saccadeSize added in WriteDataToString()
        // since data comes from testScenario timestamps did not match

        public Vector3 eyeDirectionLocal { get; set; }
        public Vector3 eyeOriginLocal { get; set; }
        public Vector3 eyeDirectionGlobal { get; set; }
        public Vector3 eyeOriginGlobal { get; set; }

        public Vector3 eyeDirectionLeftLocal { get; set; }
        public Vector3 eyeOriginLeftLocal { get; set; }
        public Vector3 eyeDirectionRightLocal { get; set; }
        public Vector3 eyeOriginRightLocal { get; set; }
        public Vector3 eyeDirectionLeftGlobal { get; set; }
        public Vector3 eyeOriginLeftGlobal { get; set; }
        public Vector3 eyeDirectionRightGlobal { get; set; }
        public Vector3 eyeOriginRightGlobal { get; set; }


        public override string ToString() => $"({timestamp}, {eyeDirectionLocal}, {eyeOriginLocal}, {eyeOpenessLeft}, {eyeOpenessRight})";
    }

    #region result file

    void defineResultsKonfigCode()
    {
        if (currentSettings.SeparateEye)
        {
            configName = "SE1_Sp" + currentSettings.Speed + "_Once" + currentSettings.OnceSpeed + "_No" + currentSettings.SpeedNoise + "_Ac" + currentSettings.Acceleration + "_Sa" + currentSettings.Sample + "_Br" + currentSettings.Break*100 + "_Cl" + currentSettings.ClosedEye*100;
        }
        else
        {
            configName = "SE0_Sp" + currentSettings.Speed + "_Once" + currentSettings.OnceSpeed + "_No" + currentSettings.SpeedNoise + "_Ac" + currentSettings.Acceleration + "_Sa" + currentSettings.Sample + "_Br" + currentSettings.Break*100 + "_Cl" + currentSettings.ClosedEye*100;
        }
    }

    void readResultFile(Result currResult)
    {
        // only read 1
        // if does not exist --> create ALL
        if (File.Exists(resultsDirectoryPath + currResult.name + fileType))
        {
            initialization = "";
            // read
            string text = File.ReadAllText(resultsDirectoryPath + currResult.name + fileType);
            // read rows
            string[] rows = text.Split(NEXT_ROW);
            int index = 0;

            // read settings,..
            while (index < rows.Length && !rows[index].Contains("Results"))
            {
                initialization += rows[index] + "\n";
                index++;
            }
            
            initialization += rows[index] + "\n";                   // add "Results:" 
            index ++;
           
            // fill table
            table = new string[rows.Length - index][];
            for (int i=0; i<rows.Length-index; i++)
            {
                table[i] = rows[index+i].Split(DELIMITER);
            }
        }
        else
        {
            // createFile & instantiate it
            setupResultFile(resultsDirectoryPath, currResult);
        }
    }

    void setupResultFile(string path, Result resultType)
    {
        initialization = "SETTINGS: " + NEXT_ROW +
        "Saccade Detection Mode:" + DELIMITER + "Separate Eye" + DELIMITER + NEXT_ROW +
        "Saccade Detection Thresholds:" + DELIMITER + "Speed" + DELIMITER + DELIMITER + "Acceleration" + DELIMITER + DELIMITER + "Sample" + DELIMITER + DELIMITER + "Break" + DELIMITER + DELIMITER + "Closed Eye" + DELIMITER + NEXT_ROW
        + "Simulation: " + DELIMITER + NEXT_ROW
        + "AllowedRange: " + DELIMITER + NEXT_ROW
        + NEXT_ROW
        + "column:, logFile,, row:, configuration" + NEXT_ROW
        + "Results: " + NEXT_ROW;
        
        setupTable(resultType);
    }

    int manageInsertionType()
    {
        // check if logFile column already exists
        if (table.Length <= logFileNumber)
        {
            // log does not exist
            insertion = insertionType.addLogRow;
        }

        // search position
        string[] configNames = table[0];
        // go through config columns
        for (int i=1; i<configNames.Length; i++)
        {
            if (configNames[i].Equals(configName))
            {
                // config exists
                if (insertion != insertionType.addLogRow)
                {
                    // log exists
                    if (table[logFileNumber].Length>i)
                    {
                        // field exists
                        insertion = insertionType.insertData;
                    }
                    else
                    {
                        // field does not exist
                        insertion = insertionType.addField;
                    }
                }
                return i;
            }
        }

        // config does not exist
        // if addRow everything done automatically

        if (insertion == insertionType.addLogRow) insertion = insertionType.addConfigAndLog;
        else insertion = insertionType.addConfigColumn;
        return configNames.Length;
    }




    void insert(Result currResult, int column)
    {
        // switch case
        switch (insertion)
        {
            case insertionType.addConfigAndLog:
                addConfigColumn(currResult, column);
                addLogRow(currResult, column);
                break;
            case insertionType.addLogRow:
                addLogRow(currResult, column);
                break;
            case insertionType.addConfigColumn:
                addConfigColumn(currResult, column);
                break;
            case insertionType.addField:
                addValueField(currResult, column);
                break;
            case insertionType.insertData:
                insertData(currResult, column);
                break;
        }
        //addConfigColumn(currResultType.value);        // special case
        // addLogRow(currResultType, 3);
        // addValueField(currResultType, 2);
        // insertData(currResultType, 2);
    }


    void insertData(Result currResult, int column)
    {
        table[logFileNumber][column] = currResult.value.ToString();
    }

    void addValueField(Result currResult, int column)
    {
        // add field at the end of row from logFile
        Array.Resize(ref table[logFileNumber], column + 1);
        table[logFileNumber][column] = currResult.value.ToString();
    }

    void addConfigColumn(Result currResult, int configColumn)
    {
        if (configColumn != table[0].Length) Debug.LogWarning("insertion column and first free column do not match. file: " + currResult.name);
        // add new column (add new field for each row in this column)
        for (int i=0; i < table.Length; i++)
        {
            Array.Resize(ref table[i], configColumn + 1);
            table[i][configColumn] = "";
        }

        // write config in column with corresponding result
        table[0][configColumn] = configName;
        if (insertion != insertionType.addConfigAndLog)
        {
            table[logFileNumber][configColumn] = currResult.value.ToString();
        }
    }

    void addLogRow(Result currResult, int configColumn)
    {
        // add new row(s)
        int oldTableSize = table.Length;
        Array.Resize(ref table, logFileNumber+1);

        // instantiate added rows with full line and logNumbers
        for (int i = oldTableSize; i <= logFileNumber; i++)
        {
            table[i] = new string[configColumn+1];
            for (int m=1; m <= configColumn; m++)
            {
                table[i][m] = "";
            }
            table[i][0] = i.ToString();
        }

        // write result & configName
        table[logFileNumber][configColumn] = currResult.value.ToString();
        table[0][configColumn] = configName;
    }

    void writeTableToFile(string fileName)
    {
        StringBuilder sb = new StringBuilder();

        for (int row=0; row < table.Length; row++)
        {
            for (int column=0; column < table[row].Length; column++)
            {
                if (column != 0) sb.Append(DELIMITER);
                sb.Append(table[row][column]);
            }
            // if not last row add new Line
            if (row+1 < table.Length) sb.Append("\n");
        }

        writeStringToFile(resultsDirectoryPath, fileName, initialization + sb.ToString());
    }

    void setupTable(Result resultType)
    {
        table = new string[logFileNumber+1][];

        // instantiate config row
        table[0] = new string[] {"", configName};
        // instantiate logFile column
        for (int i=1; i <= logFileNumber; i++)
        {
            table[i] = new string[] {i.ToString(), ""};
        }
    }

    void manuallyGetResults()
    {
        // instantiate results
        results = new Result[5];
       
        // store results
        results[0] = new Result ("_falseRate", falseRate);
        results[1] = new Result ("_detected", numSaccadesDetected);
        results[2] = new Result ("_correctRate_detected", correctRate_detected);
        results[3] = new Result ("_correctRate_truth", correctRate_truth);
        results[4] = new Result ("_avgDelay", avgDelay);
    }

    struct Results
    {
        public Results(Result falseRateName, Result detectedName, Result correctRate_detectedName, Result correctRate_truthName, Result avgDelayName)
        {
            falseRate = falseRateName;
            detected = detectedName;
            correctRate_detected = correctRate_detectedName;
            correctRate_truth = correctRate_truthName;
            avgDelay = avgDelayName;
        }

        Result falseRate;
        Result detected;
        Result correctRate_detected;
        Result correctRate_truth;
        Result avgDelay;
    }

    struct Result
    {
        public Result(string resultName, float resultValue)
        {
            name = resultName;
            value = resultValue;
        }

        public string name;
        public float value;
    }

    float getResult(string resultName)
    {
        foreach(Result result in results)
        {
            if (result.name.Equals(resultName))
            {
                return result.value;
            }
        }
        return -1;
    }

    void outputTable()
    {
        StringBuilder sb = new StringBuilder();
        for (int row = 0; row < table.Length; row++)
        {
            for (int column = 0; column < table[row].Length; column++)
            {
                if (column != 0) sb.Append(DELIMITER);
                if (table[row][column].Equals(""))
                {
                    sb.Append("E");
                }
                else
                {
                    sb.Append(table[row][column]);
                }
            }
            // if not last row add new Line
            if (row + 1 < table.Length) sb.Append("\n");
        }
        Debug.Log(sb.ToString());
    }

    enum insertionType {insertData, addField, addConfigColumn, addLogRow, addConfigAndLog};

    public void GetCurrentGroundTruth (bool inGroundTruth)
    {
        groundTruth = inGroundTruth;
    }

    #endregion
}
