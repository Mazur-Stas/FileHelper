using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace Examen;


public class FileHelper
{
    private static Dictionary<string, int> statistics = new();

    private void SaveStats(string fileName)
    {
        string statDir = "Statistics";
        Directory.CreateDirectory(statDir);
        string path = Path.Combine(statDir, fileName); 
        using StreamWriter sw = new(path);
        foreach (var st in statistics)
        {
            sw.WriteLine($"{st.Key} => {st.Value}");
        }

        Console.WriteLine($"\n Statistics save in file: {path}");
        statistics.Clear();
    }
    public async Task SearchWordAsync(string folderPath, string word, CancellationToken token)
    {
        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        int total = files.Length, processed = 0, totalFound = 0;
        Console.Clear();
        foreach (string file in files)
        {
            if (token.IsCancellationRequested) return;

            string content = await File.ReadAllTextAsync(file);
            int count = Regex.Matches(content, $@"{Regex.Escape(word)}").Count;
            if (count > 0)
            {
                statistics[file] = count;
                totalFound += count;
            }

            processed++;
            Console.WriteLine($" {processed * 100 / total}% - {file}");
            await Task.Delay(1000);
        }
        Console.WriteLine($"\n Total matches found:");
        foreach (var st in statistics)
        {
            Console.WriteLine($"{st.Key} => {st.Value}");
        }

        SaveStats("SearchStats.txt");
    }

    public async Task CopyFilesWithReplacementAsync(string folderPath, string word, string replaceWith, CancellationToken token)
    {
        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        string outputFolder = Path.Combine(folderPath, "Copied"); 
        Directory.CreateDirectory(outputFolder);

        int total = files.Length, processed = 0;
        Console.Clear();
        foreach (var file in files)
        {
            if (token.IsCancellationRequested) return;

            string content = await File.ReadAllTextAsync(file);
            if (content.Contains(word))
            {
                int count = Regex.Matches(content, Regex.Escape(word)).Count;
                statistics[file] = count;

                string replaced = content.Replace(word, replaceWith);
                string fileName = Path.GetFileName(file);
                string newFile = Path.Combine(outputFolder, fileName);
                await File.WriteAllTextAsync(newFile, replaced);
            }

            processed++;
            Console.WriteLine($" {processed * 100 / total}% - {file}");
            await Task.Delay(1000);
        }
        Console.WriteLine("\n Copy end.");
        SaveStats("CopyStats.txt");
    }

    public async Task FindClassesAndInterfacesAsync(string folderPath, CancellationToken token)
    {
        string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        int total = files.Length, processed = 0;
        Console.Clear();
        foreach (var file in files)
        {
            if (token.IsCancellationRequested) return;

            string content = await File.ReadAllTextAsync(file);
            int count = Regex.Matches(content, "class").Count;
            count += Regex.Matches(content, "interface").Count;
            if (count > 0)
                statistics[file] = count;

            processed++;
            Console.WriteLine($" {processed * 100 / total}% - {file}");
            await Task.Delay(1000);
        }
        
        Console.WriteLine("\n search end.");
        Console.WriteLine($"\n Total matches found:");
        foreach (var st in statistics)
        {
            Console.WriteLine($"{st.Key} => {st.Value}");
        }
        SaveStats("CsStats.txt");

    }



}


internal class Program
    {
   static void ListenForCancel(CancellationTokenSource cts)
    {

        Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        Console.WriteLine("\n Operation cancled ");
                        cts.Cancel();
                    }
                }
            }
        });
    }
    static async Task Main(string[] args)
    {
        var helper = new FileHelper();

        Console.Write(" Enter file path: ");
        string path = Console.ReadLine();

        

        while (true)
        {
            Console.WriteLine("\n Choose action:");
            Console.WriteLine("1.  Search word in file");
            Console.WriteLine("2.  Copy with replace word");
            Console.WriteLine("3.  Search  class and and interface in .cs");
            Console.WriteLine("0.  Exit");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Enter word: ");
                    string word = Console.ReadLine();


                    var cts = new CancellationTokenSource();
                    ListenForCancel(cts);
                    await helper.SearchWordAsync(path, word, cts.Token);
                    break;

                case "2":
                    Console.Write("Enter word to replace: ");
                    string oldWord = Console.ReadLine();
                    Console.Write("Enter new word: ");
                    string newWord = Console.ReadLine();

                    var cts1 = new CancellationTokenSource();
                    ListenForCancel(cts1);
                    await helper.CopyFilesWithReplacementAsync(path, oldWord, newWord, cts1.Token);
                    break;

                case "3":
                    var cts2 = new CancellationTokenSource();
                    ListenForCancel(cts2);
                    await helper.FindClassesAndInterfacesAsync(path, cts2.Token);
                    break;

                case "0":
                    Console.WriteLine("Exit...");
                    return;

                default:
                    Console.Clear();
                    Console.WriteLine(" Error.");
                    break;
            }
        }
    }
}

