using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UiPathTeam.PDFRedaction.Activities.Helpers
{
    public static class WatermarkProcessor
    {
        private static readonly string[] DefaultFormats = new[] { ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".tif" };

        public static void ApplyWatermark(string waterMarkFile, Point waterMarkLocation, string fileInput, string fileOutput, bool silent, string pathWorking, string pathRedacted, string[] formats = null)
        {
            if (string.IsNullOrEmpty(waterMarkFile) || !File.Exists(waterMarkFile))
            {
                FilePathsProcessor.MoveWorkToRedacted(pathWorking, pathRedacted);
                return;
            }

            if (!silent)
            {
                Console.WriteLine("Applying Watermark 90%");
            }

            ApplyWatermarkToFiles(waterMarkFile, fileInput, fileOutput, waterMarkLocation, pathWorking, pathRedacted, formats);
        }

        private static void ApplyWatermarkToFiles(string watermarkFile, string inputFile, string outputFile, Point watermarkPosition, string inputFolder, string outputFolder, string[] formats = null, bool continueOnError = true)
        {
            formats ??= DefaultFormats;

            ValidateWatermarkFile(watermarkFile);

            Image watermark = LoadWatermarkImage(watermarkFile);

            string[] inputFiles = GetInputFiles(inputFile, outputFile, inputFolder, outputFolder, formats, out bool isMultiple);

            var errorsList = new List<string>();

            foreach (var item in inputFiles)
            {
                ProcessImage(item, watermark, watermarkPosition, isMultiple, outputFile, outputFolder, errorsList);
            }

            HandleErrors(errorsList, continueOnError);
        }

        private static void ValidateWatermarkFile(string watermarkFile)
        {
            if (string.IsNullOrEmpty(watermarkFile) || !File.Exists(watermarkFile))
            {
                throw new Exception("Valid watermark image file is required");
            }
        }

        private static Image LoadWatermarkImage(string watermarkFile)
        {
            try
            {

                return Image.FromFile(watermarkFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Watermark file access error: " + ex.Message);
            }
        }

        private static string[] GetInputFiles(string inputFile, string outputFile, string inputFolder, string outputFolder, string[] formats, out bool isMultiple)
        {
            isMultiple = false;

            if (!string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(outputFile) && File.Exists(inputFile) && Path.IsPathRooted(outputFile) && formats.Contains(Path.GetExtension(inputFile).ToLower()) && formats.Contains(Path.GetExtension(outputFile).ToLower()))
            {
                return new[] { inputFile };
            }
            else if (!string.IsNullOrEmpty(inputFolder) && !string.IsNullOrEmpty(outputFolder) && Directory.Exists(inputFolder) && Directory.Exists(outputFolder))
            {
                isMultiple = true;
                return Directory.GetFiles(inputFolder).Where(f => formats.Contains(Path.GetExtension(f).ToLower())).ToArray();
            }
            else
            {
                throw new Exception("Provide either (InputFile and OutputFile), or (InputFolder and OutputFolder)");
            }
        }

        private static void ProcessImage(string inputFile, Image watermark, Point watermarkPosition, bool isMultiple, string outputFile, string outputFolder, List<string> errorsList)
        {
            try
            {
                using (var img = Image.FromFile(inputFile))
                {
                    var format = GetImageFormat(inputFile);
                    if (format == null)
                    {
                        errorsList.Add($"{inputFile} || Unsupported file format");
                        return;
                    }

                    using var pb = new PictureBox { Image = new Bitmap(img) };
                    using (var g = Graphics.FromImage(pb.Image))
                    {
                        g.DrawImage(watermark, new PointF(watermarkPosition.X, watermarkPosition.Y));
                    }

                    if (isMultiple)
                    {
                        outputFile = Path.Combine(outputFolder, Path.GetFileName(inputFile));
                    }

                    SaveImage(pb.Image, outputFile, format, errorsList);
                }
            }
            catch (Exception ex)
            {
                errorsList.Add($"{inputFile} || File access error: {ex.Message}");
            }
        }

        private static void SaveImage(Image image, string outputFile, ImageFormat format, List<string> errorsList)
        {
            try
            {
                image.Save(outputFile, format);
            }
            catch (Exception ex)
            {
                errorsList.Add($"{outputFile} || Can't save file: {ex.Message}");
            }
        }

        private static void HandleErrors(List<string> errorsList, bool continueOnError)
        {
            if (errorsList.Count > 0 && !continueOnError)
            {
                throw new Exception(string.Join(Environment.NewLine, errorsList));
            }
        }

        private static ImageFormat GetImageFormat(string filename)
        {
            return Path.GetExtension(filename).ToLower() switch
            {
                ".jpeg" or ".jpg" => ImageFormat.Jpeg,
                ".png" => ImageFormat.Png,
                ".gif" => ImageFormat.Gif,
                ".tif" or ".tiff" => ImageFormat.Tiff,
                _ => null,
            };
        }
    }
}
