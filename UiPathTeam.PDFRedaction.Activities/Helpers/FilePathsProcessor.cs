using System;
using System.IO;
using System.Threading;

namespace UiPathTeam.PDFRedaction.Activities.Helpers;

public static class FilePathsProcessor
{
    public static (string FileOutput, string PathStripped, string PathWorking, string PathRedacted) CreateFilePaths(string fileInput, string fileOutput)
    {
        fileOutput ??= Path.Combine(Path.GetTempPath(), "Redacted", "Redacted-" + Path.GetFileName(fileInput));

        if (!Path.IsPathRooted(fileOutput))
        {
            fileOutput = Path.Combine(Path.GetTempPath(), "Redacted", fileOutput);
        }

        fileOutput = fileOutput.Replace("\\\\", "\\");

        string pathTemp = Path.GetTempPath();
        string pathStrip = Path.Combine(pathTemp, "Stripped");
        string pathWork = Path.Combine(pathTemp, "Working");
        string pathRedacted = Path.Combine(pathTemp, "Redacted");

        try
        {
            if (Directory.Exists(pathTemp))
            {
                Directory.Delete(pathTemp, true);
            }

            Thread.Sleep(2000);

            Directory.CreateDirectory(pathTemp);
            Directory.CreateDirectory(pathStrip);
            Directory.CreateDirectory(pathRedacted);
            Directory.CreateDirectory(pathWork);

            if (!Directory.Exists(Path.GetDirectoryName(fileOutput)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileOutput));
            }
        }
        catch
        {
            pathStrip = Path.Combine(pathTemp, "Stripped-" + Path.GetFileNameWithoutExtension(fileInput));

            if (!Directory.Exists(pathStrip))
            {
                Directory.CreateDirectory(pathStrip);
            }

            Directory.CreateDirectory(pathRedacted);
            Directory.CreateDirectory(pathWork);

            if (!Directory.Exists(Path.GetDirectoryName(fileOutput)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileOutput));
            }
        }

        return (fileOutput, pathStrip, pathWork, pathRedacted);
    }

    public static void MoveWorkToStrip(string pathWork, string pathStrip)
    {
        string[] files;

        try
        {
            // Delete files in pathStrip
            files = Directory.GetFiles(pathStrip, "*.png");
            foreach (string filename in files)
            {
                File.Delete(filename);
            }

            // Move files from pathWork to pathStrip
            files = Directory.GetFiles(pathWork, "*.png");
            foreach (string filename in files)
            {
                string destinationFileName = Path.Combine(pathStrip, Path.GetFileName(filename));
                File.Move(filename, destinationFileName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static void MoveWorkToRedacted(string pathWork, string pathRedacted)
    {
        try
        {
            var files = Directory.GetFiles(pathWork, "*.png");
            foreach (string filename in files)
            {
                File.Move(filename, Path.Combine(pathRedacted, Path.GetFileName(filename)));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
