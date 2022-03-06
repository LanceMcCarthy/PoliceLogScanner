using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model;

#pragma warning disable CA1416 // Validate platform compatibility
class Program
{
    private static string? outputText = "";

    public static void Main(string[] args)
    {
        // ***** Phase 1: Set up resources we need to perform the operations ***** //

        Console.Title = "Police Log Scanner";
        Console.CancelKeyPress += Console_CancelKeyPress;
        outputText = "";

        // ***** Phase 2: Get Folder Path and Search Term from User Input ***** //

        UpdateStatus("                                      ", ConsoleColor.White, ConsoleColor.DarkBlue);
        UpdateStatus("  Welcome to the Police Log Scanner!  ", ConsoleColor.White, ConsoleColor.DarkBlue);
        UpdateStatus("                                      ", ConsoleColor.White, ConsoleColor.DarkBlue);

        string? folderPath = "";
        string? searchTerm = "";

        while (true)
        {
            UpdateStatus("Enter folder path to scan for PDF files (default: 'C:\\Users\\lance\\Downloads\\Logs'):", ConsoleColor.DarkYellow, ConsoleColor.Black);

            Console.ResetColor();

            folderPath = Console.ReadLine();

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = @"C:\Users\lance\Downloads\Logs";
            }

            if (!Directory.Exists(folderPath))
            {
                UpdateStatus("The folder path you provided is not valid. Try again, or use CTRL+C to quit.", ConsoleColor.White, ConsoleColor.DarkRed);
                continue;
            }

            while (true)
            {
                UpdateStatus("Enter a search term (default: '65 Main St'):", ConsoleColor.Yellow, ConsoleColor.Black);

                Console.ResetColor();

                searchTerm = Console.ReadLine();

                if (string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = "65 Main Street";
                }

                break;
            }

            break;
        }

        Console.ResetColor();


        // ***** Phase 3: Find PDF files and build a list of jobs that open and search each file ***** //

        var dInfo = new DirectoryInfo(folderPath);

        var fileInfos = dInfo.GetFiles("*.pdf");

        UpdateStatus($"Discovered {fileInfos.Length} PDF files.", ConsoleColor.Cyan, ConsoleColor.Black);

        UpdateStatus($"Building jobs list...", ConsoleColor.DarkCyan, ConsoleColor.Black);

        var actions = new List<Action>();

        for (int i = 0; i < fileInfos.Length - 1; i++)
        {
            var fileInfo = fileInfos[i];

            actions.Add(() => { SearchDocument(fileInfo, searchTerm); });
        }

        UpdateStatus($"{actions.Count} jobs created.", ConsoleColor.DarkGreen, ConsoleColor.Black);


        // ***** Phase 4: run the list of jobs in parallel with multipl threads ***** //

        UpdateStatus($"Running jobs in parallel...", ConsoleColor.White, ConsoleColor.DarkGreen);

        Parallel.Invoke(actions.ToArray());

        Console.Title = $"Done!";

        UpdateStatus($"Operation Complete. All files have been scanned, check the results above for any hits.", ConsoleColor.White, ConsoleColor.Black);

        SaveResults(folderPath);
    }

    private static void SearchDocument(FileInfo fInfo, string searchTerm)
    {
        var provider = new PdfFormatProvider();

        RadFixedDocument document;

        UpdateStatus($"[SCANNING] {fInfo.Name}...", ConsoleColor.White, ConsoleColor.Black);

        using (Stream stream = fInfo.OpenRead())
        {
            document = provider.Import(stream);
        }

        byte[] documentContent = provider.Export(document);

        var documentText = System.Text.Encoding.Default.GetString(documentContent);

        if (documentText.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase))
        {
            UpdateStatus($"[***** MATCH *****] {fInfo.Name}", ConsoleColor.Green, ConsoleColor.Black);
        }
        else
        {
            UpdateStatus($"[NO MATCH] {fInfo.Name}", ConsoleColor.DarkGray, ConsoleColor.Black);
        }
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        UpdateStatus("Requesting cancel, please wait...", ConsoleColor.Black, ConsoleColor.Yellow);

        e.Cancel = true;

        UpdateStatus("Cancel request complete.", ConsoleColor.White, ConsoleColor.DarkRed);
    }

    private static void UpdateStatus(string message, ConsoleColor textColor, ConsoleColor backColor, bool replaceLastLine = false)
    {
        if (replaceLastLine)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);

            try
            {
                int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, currentLineCursor);
            }
            catch (Exception)
            {
                // ignored -- may fail in some consoles (e.g. VSCode on macOS)
            }
        }

        Console.BackgroundColor = backColor;
        Console.ForegroundColor = textColor;
        Console.WriteLine(message);

        outputText = outputText + "\r" + message;
    }

    private static void SaveResults(string logsFolder)
    {
        var resultsFolder = Path.Join(logsFolder, "Results");

        if (!Directory.Exists(resultsFolder))
        {
            Directory.CreateDirectory(resultsFolder);
        }

        var resultsFileName = $"{DateTime.Now.ToString("yyyy.MMdd.hhmmss")} LogScanner Result.txt";
        var resultsFilePath = Path.Join(resultsFolder, resultsFileName);

        // Write the results to a file so that this operation can be done in the background.
        File.WriteAllText(resultsFilePath, outputText);
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
