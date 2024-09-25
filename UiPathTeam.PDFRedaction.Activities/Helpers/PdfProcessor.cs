using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;
using UiPath.DocumentProcessing.Contracts.Dom;
using UiPathTeam.PDFRedaction.Activities.Workaround;

namespace UiPathTeam.PDFRedaction.Activities.Helpers
{
    public class PdfProcessor
    {
        public static void ExportImagesFromPdf(string filePath, string outputPath)
        {
            var _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            NativeLibrariesWorkarounds.Load(_isWindows);

            using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IEnumerable<SKBitmap> images = PDFtoImage.Conversion.ToImages(input);

            int pageIndex = 1;
            foreach (var image in images)
            {
                var imagePath = Path.Combine(outputPath, $"pdf-{pageIndex:000}.png");
                using (var imageData = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    using var stream = File.OpenWrite(imagePath);
                    imageData.SaveTo(stream);
                }

                Console.WriteLine($"Saved page {pageIndex} as image: {imagePath}");
                pageIndex++;
            }
        }

        public static void ExportToPdf(Document document, string pathRedacted, string fileOutput, bool silent)
        {
            if (!silent)
            {
                Console.WriteLine("Saving the PDF in Progress..... 99%");
            }

            ExportDocumentToPdf(pathRedacted, fileOutput, document);
        }

        public static void ConvertPngToPdf(string folderPath, string filePDF, double resolution, PdfDocument dom = null)
        {
            try
            {
                // Create new pdf document
                PdfDocument doc = new PdfDocument();
                PdfPage oPage;

                string[] files = Directory.GetFiles(folderPath, "*.png");
                int i = 0;

                foreach (string filename in files)
                {
                    // Create new oPage
                    oPage = new PdfPage();

                    // Add the page to the pdf document and add the captured image to it
                    doc.Pages.Add(oPage);

                    // Create an XImage Object from Image File
                    using (XImage xImg = XImage.FromFile(filename))
                    {
                        if (dom == null)
                        {
                            if (resolution < 10)
                            {
                                resolution = 23;
                            }
                            oPage.Width = xImg.PixelWidth * resolution / xImg.HorizontalResolution;
                            oPage.Height = xImg.PixelHeight * resolution / xImg.VerticalResolution;
                        }
                        else
                        {
                            oPage.Width = dom.Pages[i].Width;
                            oPage.Height = dom.Pages[i].Height;
                        }

                        // Draw current image file to page
                        using XGraphics xgr = XGraphics.FromPdfPage(oPage);
                        xgr.DrawImage(xImg, 0, 0, oPage.Width, oPage.Height);
                    }

                    i++;
                }

                if (files.Length == 0)
                {
                    Console.WriteLine("No Files to Convert to PDF");
                    return;
                }

                // If the file already exists, delete it
                if (File.Exists(filePDF))
                {
                    File.Delete(filePDF);
                }

                // Save the document
                doc.Save(filePDF);
                doc.Close();

                if (File.Exists(filePDF))
                {
                    Console.WriteLine("Saved: " + filePDF);
                }
                else
                {
                    Console.WriteLine("Failed to Save: " + filePDF);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed To Save: " + filePDF + Environment.NewLine + "Please make sure the file is not currently in use, and try again");
            }
            finally
            {
                // No cleanup necessary here
            }
        }

        private static void ExportDocumentToPdf(string folder, string fileOut, Document dom, double resolution = 72, bool continueOnError = true)
        {
            try
            {
                var dirName = Path.GetDirectoryName(fileOut);
                if (!Directory.Exists(dirName))
                {
                    throw new DirectoryNotFoundException("Output folder does not exist: " + dirName);
                }

                using var doc = new PdfDocument();
                var files = Directory.GetFiles(folder, "*.png");

                if (files.Length == 0)
                {
                    throw new FileNotFoundException("No files to convert to PDF.");
                }

                foreach (var file in files)
                {
                    AddImageToPdf(doc, file, dom, resolution);
                }

                SavePdfDocument(doc, fileOut);
            }
            catch (Exception ex)
            {
                if (continueOnError)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void AddImageToPdf(PdfDocument doc, string file, Document dom, double resolution)
        {
            var page = doc.AddPage();

            using var xImg = XImage.FromFile(file);
            SetPageSize(page, xImg, dom, doc, resolution);
            DrawImageOnPage(page, xImg);
        }

        private static void SetPageSize(PdfPage page, XImage xImg, Document dom, PdfDocument doc, double resolution)
        {
            if (dom == null)
            {
                resolution = Math.Max(resolution, 23);
                page.Width = XUnit.FromPoint(xImg.PixelWidth * resolution / xImg.HorizontalResolution);
                page.Height = XUnit.FromPoint(xImg.PixelHeight * resolution / xImg.VerticalResolution);
            }
            else
            {
                var pageIndex = doc.Pages.Count - 1;
                page.Width = XUnit.FromMillimeter(dom.Pages[pageIndex].Size.Width);
                page.Height = XUnit.FromMillimeter(dom.Pages[pageIndex].Size.Height);
            }
        }

        private static void DrawImageOnPage(PdfPage page, XImage xImg)
        {
            using var xgr = XGraphics.FromPdfPage(page);
            xgr.DrawImage(xImg, 0, 0, page.Width, page.Height);
        }

        private static void SavePdfDocument(PdfDocument doc, string fileOut)
        {
            if (File.Exists(fileOut))
            {
                File.Delete(fileOut);
            }

            doc.Save(fileOut);
            Console.WriteLine(File.Exists(fileOut) ? $"Saved: {fileOut}" : $"Failed to save: {fileOut}");
        }
    }
}
