using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UiPath.DocumentProcessing.Contracts.Dom;
using UiPath.DocumentProcessing.Contracts.Results;
namespace UiPathTeam.PDFRedaction.Activities.Helpers;

public class DuRedactionProcessor : RedactionProcessorBase
{
    public static void PerformDomRedaction(string pathStripped, string pathWorking, Document document, string formula, string[] formulaAuto,
     string[] keywords, bool highlightOnly, Color redactColor, bool silent, string fileInput)
    {
        var dataTable = DocumentProcessor.DomToDataTable(document);
        if (!silent)
        {
            Console.WriteLine("Starting Redaction Process");
        }

        formula = CreateFormula(formula, keywords, formulaAuto, silent);

        if (Path.GetExtension(fileInput).ToLower() == ".pdf")
        {
            PdfProcessor.ExportImagesFromPdf(fileInput, pathStripped);
        }
        else
        {
            File.Copy(fileInput, Path.Combine(pathStripped, Path.GetFileName(fileInput)));
        }

        var pathStrippedFiles = Directory.GetFiles(pathStripped, "*.png").ToList();

        BasicRedaction(pathStrippedFiles, dataTable, redactColor, highlightOnly, formula, silent, pathWorking);
    }

    public static void PerformEomRedaction(string pathStripped, string pathWorking, ExtractionResult extractionResult, string[] redactFields,
        bool highlightOnly, Color redactColor, bool silent)
    {
        try
        {
            FilePathsProcessor.MoveWorkToStrip(pathWorking, pathStripped);
            var dataTableFields = DocumentProcessor.EomToDataTable(extractionResult);

            var files = Directory.GetFiles(pathStripped, "*.png").ToList();

            for (int i = 0; i < files.Count; i++)
            {
                var percent = ((i + 1) / (double)files.Count) * 0.40 + 0.4;

                if (!silent)
                {
                    Console.WriteLine($"Intelligent Redaction in progress..... {percent * 100}%");
                }

                var filePng = Path.Combine(pathWorking, $"Redacted-{i:000}.png");

                using (var image = Image.FromFile(files[i]))
                {
                    var filteredDataTable = DocumentProcessor.FilterDataTable(dataTableFields, i);
                    RedactEomImage(image, filteredDataTable, filePng, redactColor, highlightOnly, redactFields.ToList());
                }
            }
        }
        catch
        {
            Console.WriteLine("Redaction Failed");
        }
    }

    public static bool RequiresEomRedaction(object extractionResult, string[] redactFields, bool silent, string pathWorking)
    {
        if (Directory.GetFiles(pathWorking).Any())
        {
            if (extractionResult == null)
            {
                Console.WriteLine("Note: No DU Extraction Result Provided");
                return false;
            }

            if (redactFields == null || !redactFields.Any())
            {
                Console.WriteLine("No Redaction Fields were Provided");
                return false;
            }

            return true;
        }
        else
        {
            if (!silent)
            {
                Console.WriteLine("Redaction Failed");
            }

            return false;
        }
    }

    private static void BasicRedaction(List<string> files, DataTable dataTable, Color redactColor, bool highlightOnly, string formula,
        bool silent, string pathWorking)
    {
        for (var i = 0; i < files.Count; i++)
        {
            var percent = ((i + 1) / (double)files.Count) * 0.40;
            var pathWorkingFile = Path.Combine(pathWorking, $"Redacted-{i:000}.png");

            if (!silent)
            {
                Console.WriteLine($"Basic-Redaction in progress..... {percent * 100}%");
            }

            using (var image = Image.FromFile(files[i]))
            {
                var filteredDataTable = DocumentProcessor.FilterDataTable(dataTable, i);
                RedactDomImage(image, filteredDataTable, pathWorkingFile, redactColor, highlightOnly, formula);
            }
        }
    }

    private static void RedactDomImage(Image imgin, DataTable dataTable, string fileOut, Color? redactColor, bool highlightOnly, string formula)
    {
        // Initialize variables
        SolidBrush myBrush;
        Pen myPen;
        PictureBox pb = new();
        Rectangle rect;
        int thickness = 3;
        int j = 0;

        // Set the brush and pen colors
        if (redactColor == null) redactColor = Color.Black;
        myBrush = new SolidBrush((Color)redactColor);
        myPen = new Pen((Color)redactColor, thickness);

        // Place image in PictureBox
        pb.Image = new Bitmap(imgin, new Size(
            Convert.ToInt32(dataTable.Rows[0]["W"].ToString()),
            Convert.ToInt32(dataTable.Rows[0]["H"].ToString())
        ));

        // Redact the image
        using (Graphics g = Graphics.FromImage(pb.Image))
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow["Type"].ToString().ToLower() == "page") continue;

                rect = new Rectangle(
                    Convert.ToInt32(dataRow["X"].ToString()),
                    Convert.ToInt32(dataRow["Y"].ToString()),
                    Convert.ToInt32(dataRow["W"].ToString()),
                    Convert.ToInt32(dataRow["H"].ToString())
                );

                // Redact or Highlight Text
                try
                {
                    if (!string.IsNullOrEmpty(formula) && Regex.IsMatch(dataRow["Word"].ToString(), formula))
                    {
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
                catch
                {
                    Console.WriteLine("Failed to Redact row: " + j.ToString());
                }
                finally
                {
                    // Optionally add any cleanup code here
                }

                j++;
            }
        }

        // Save the image to disk
        try
        {
            pb.Image.Save(fileOut, System.Drawing.Imaging.ImageFormat.Png);
            pb.Image = null; // Clear the image from the PictureBox
        }
        catch (Exception ex)
        {
            Console.WriteLine("Can't Save File: " + ex.Message + " " + fileOut);
        }
        finally
        {
            // Optionally add any cleanup code here
        }

        // Dispose Image
        imgin.Dispose();
    }

    private static void RedactEomImage(Image imgin, DataTable dataTable, string fileOut, Color? redactColor, bool highlightOnly, List<string> fields)
    {
        // Initialize variables
        Brush myBrush;
        Pen myPen;
        PictureBox pb = new();
        Rectangle rect;
        int thickness = 3;

        // Set the brush and pen colors
        if (redactColor == null) redactColor = Color.Black;
        myBrush = new SolidBrush((Color)redactColor);
        myPen = new Pen((Color)redactColor, thickness);

        // Place image in PictureBox
        pb.Image = new Bitmap(imgin);

        // Redact the image
        using (Graphics g = Graphics.FromImage(pb.Image))
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                // If not on page or missing then skip
                if (Convert.ToBoolean(dataRow["Missing"].ToString())) continue;

                rect = new Rectangle(
                    Convert.ToInt32(dataRow["X"].ToString()),
                    Convert.ToInt32(dataRow["Y"].ToString()),
                    Convert.ToInt32(dataRow["W"].ToString()),
                    Convert.ToInt32(dataRow["H"].ToString())
                );

                // Redact or Highlight Text
                try
                {

                    if (fields.Contains(dataRow["Field"].ToString(), StringComparer.OrdinalIgnoreCase))
                    {
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
                catch
                {
                    Console.WriteLine("Field Not Found: " + dataRow["Field"].ToString());
                }
            }
        }

        // Save the image to disk
        try
        {
            pb.Image.Save(fileOut, System.Drawing.Imaging.ImageFormat.Png);
            pb.Image = null; // Clear the image from the PictureBox
        }
        catch (Exception ex)
        {
            Console.WriteLine("Can't Save File: " + ex.Message + " " + fileOut);
            fileOut = null;
        }
        finally
        {
            // Dispose Image
            imgin.Dispose();
        }
    }
}
