using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using UiPath.OCR.Contracts;
using UiPathTeam.PDFRedaction.Activities.Models;

namespace UiPathTeam.PDFRedaction.Activities.Helpers;

public class PdfRedactionProcessor : RedactionProcessorBase
{
    public static async Task PerformImgRedactionAsync(string fileInput, string inputFolder, string fileOutput, string outputFolder, string formula, string[] keywords,
    string[] formulaAuto, Color redactColor, bool highlightOnly, string pathWorking, string method, IOCREngineActivity ocrActivity, bool silent)
    {
        try
        {
            var (success, files) = ValidateInputs(fileInput, inputFolder, fileOutput, outputFolder, silent);

            CreateFormula(formula, keywords, formulaAuto, silent);

            if (success)
            {
                if (files == null)
                {
                    Console.WriteLine(method + "ing: " + Path.GetFileName(fileInput));

                    using (var image = Image.FromFile(fileInput))
                    {
                        ocrActivity.ExtractWords = true;
                        var ocrResult = await ocrActivity.ExecuteOCRAsync(image, default);

                        RedactImage(image, ocrResult.Words, formula, fileOutput, highlightOnly, redactColor, 5);
                    }
                }
                else
                {
                    await BasicRedactionAsync(files.ToList(), redactColor, highlightOnly, formula, silent, pathWorking, method, ocrActivity, ProcessType.Img);
                }
            }
            else
            {
                Console.WriteLine("Exiting.....");
            }
        }
        catch (Exception e)
        {
            throw new Exception("Redaction Failed " + e.Message);
        }
    }

    public static async Task PerformPdfRedactionAsync(string fileInput, string fileOutput, string formula, string[] keywords, string[] formulaAuto, Color redactColor,
        bool highlightOnly, string pathWorking, string pathStripped, string method, IOCREngineActivity ocrActivity, bool silent)
    {
        try
        {
            formula = CreateFormula(formula, keywords, formulaAuto, silent);

            PdfProcessor.ExportImagesFromPdf(fileInput, pathStripped);

            var pathStrippedFiles = Directory.GetFiles(pathStripped, "*.png").ToList();

            await BasicRedactionAsync(pathStrippedFiles, redactColor, highlightOnly, formula, silent, pathWorking, method, ocrActivity, ProcessType.Pdf);

            if (!silent)
            {
                Console.WriteLine("Saving : " + Path.GetFileName(fileOutput));
            }

            var outputFolder = Path.GetDirectoryName(fileOutput);
            if (Directory.Exists(outputFolder))
            {
                PdfProcessor.ConvertPngToPdf(pathWorking, fileOutput, 72, null);
            }
            else
            {
                throw new Exception($"Error: Output Folder Does Not Exist: {outputFolder}");
            }

            Directory.Delete(pathStripped, true);
            Directory.Delete(pathWorking, true);
        }
        catch (Exception e)
        {
            throw new Exception("Redaction Failed " + e.Message);
        }
    }

    public static async Task BasicRedactionAsync(List<string> files, Color redactColor, bool highlightOnly, string formula, bool silent,
        string outputFolder, string method, IOCREngineActivity ocrActivity, ProcessType processType)
    {
        for (var i = 0; i < files.Count; i++)
        {
            var percent = (i + 1) / files.Count;

            if (!silent)
            {
                var logMessage = method[..1].ToUpper() + method[1..] + "ing Page: " + (i + 1).ToString() + " ( " + (100 * percent).ToString("0") + "% )";
                Console.WriteLine(logMessage);
            }

            var outputFile = Path.Combine(outputFolder, processType == ProcessType.Img ? Path.GetFileName(files[i]) : $"Redacted-{i:000}.png");

            using (var image = Image.FromFile(files[i]))
            {
                ocrActivity.ExtractWords = true;
                var ocrResult = await ocrActivity.ExecuteOCRAsync(image, default);

                RedactImage(image, ocrResult.Words, formula, outputFile, highlightOnly, redactColor, 5);
            }
        }
    }

    public static void RedactImage(Image imgIn, IEnumerable<OCRWord> words, string formula, string fileOut, bool highlightOnly, Color? redactColor, int thickness)
    {
        SolidBrush myBrush;
        Pen myPen;
        PictureBox pb = new();
        Rectangle rect;

        if (!redactColor.HasValue)
        {
            redactColor = Color.Black;
        }
        myBrush = new SolidBrush(redactColor.Value);
        myPen = new Pen(redactColor.Value, thickness);

        // Place image in PictureBox
        pb.Image = imgIn;

        // Redact the image
        using (Graphics g = Graphics.FromImage(pb.Image))
        {
            foreach (var item in words)
            {
                rect = item.Rectangle;
                if (!string.IsNullOrEmpty(formula) && Regex.IsMatch(item.Text.Trim(), formula))
                {
                    // Print found string
                    Console.WriteLine(item.Text.Trim());

                    if (highlightOnly)
                    {
                        g.DrawRectangle(myPen, rect);
                    }
                    else
                    {
                        g.FillRectangle(myBrush, rect);
                    }
                }
            }
        }

        // Save the image to disk
        try
        {
            pb.Image.Save(fileOut, System.Drawing.Imaging.ImageFormat.Png);
            pb.Image = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Can't Save File: " + ex.Message + " " + fileOut);
            fileOut = null;
        }
        finally
        {
            // Dispose Image
            imgIn.Dispose();
        }
    }

    public static (bool Success, string[] Files) ValidateInputs(string fileInput, string folderInput, string fileOutput, string folderOutput, bool silent)
    {
        string[] exts = { ".jpeg", "jpg", ".png", ".tif", ".tiff" };

        // Validate FileInput
        if (!string.IsNullOrEmpty(fileInput) && !File.Exists(fileInput))
        {
            Console.WriteLine("The input file was not found: " + fileInput);
            return (false, null);
        }

        // Validate FolderInput and FolderOutput
        if (string.IsNullOrEmpty(fileInput))
        {
            if (string.IsNullOrEmpty(folderInput) || string.IsNullOrEmpty(folderOutput) || !Path.IsPathRooted(folderOutput))
            {
                Console.WriteLine("Invalid FolderInput or FolderOutput parameters");
                return (false, null);
            }
        }
        else if (File.Exists(fileInput))
        {
            string ext = Path.GetExtension(fileInput).ToLower();
            if (!exts.Contains(ext))
            {
                Console.WriteLine("Unsupported File Type: " + ext.ToUpper() + "; Please provide an image file");
                return (false, null);
            }

            if (string.IsNullOrEmpty(fileOutput) || !Path.IsPathRooted(fileOutput) || fileOutput == fileInput)
            {
                Console.WriteLine("Invalid FileOutput parameter");
                return (false, null);
            }

            folderOutput = Path.GetDirectoryName(fileOutput);
            Directory.CreateDirectory(folderOutput);
            if (!silent) Console.WriteLine("Created directory: " + folderOutput);
            return (true, null);
        }

        // Validate and Create FolderOutput
        if (folderOutput == folderInput)
        {
            Console.WriteLine("The output Folder cannot be the same as the input folder: FOLDEROUTPUT");
            return (false, null);
        }

        Directory.CreateDirectory(folderOutput);
        if (!silent) Console.WriteLine("Created directory: " + folderOutput);

        // Process FolderInput
        if (Directory.Exists(folderInput))
        {
            var files = Directory.GetFiles(folderInput, "*.*")
                                  .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                                  .ToArray();

            if (files.Length == 0)
            {
                Console.WriteLine("Input Image Folder has no images: " + folderInput);
                return (false, null);
            }

            if (!silent)
            {
                Console.WriteLine("Input Folder: " + folderInput);
                Console.WriteLine("Output Folder: " + folderOutput);
            }
            return (true, files);
        }
        else
        {
            Console.WriteLine("Input Image Folder Path does not exist: " + folderInput);
            return (false, null);
        }
    }

}
