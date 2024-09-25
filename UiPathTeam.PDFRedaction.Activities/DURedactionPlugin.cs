using System;
using System.Activities;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.DocumentProcessing.Contracts.Dom;
using UiPath.DocumentProcessing.Contracts.Results;
using UiPathTeam.PDFRedaction.Activities.Helpers;

namespace UiPathTeam.PDFRedaction.Activities
{
    [DisplayName("DU Redaction Plugin"), Description("The DU Redaction Activity is a step up from the basic Redaction Activity in that it provides for more intelligent redaction, handwriting capability, and ability to redact paragraphs rather than just words. ")]
    public class DURedactionPlugin : AsyncTaskCodeActivity
    {
        [Category("Input"), Description("The Result from Document Understanding Digitize Activity.")]
        public InArgument<Document> DocumentObjectModel { get; set; }

        [Category("Input"), Description("The Result from the Document Understanding Extraction Scope Activity.")]
        public InArgument<ExtractionResult> ExtractionResult { get; set; }

        [Category("Input"), Description("Input File.  Relative Paths are not allowed.  Please provide a fully rooted path to the input file.")]
        [RequiredArgument]
        public InArgument<string> FileInput { get; set; }

        [Category("Input"), Description("User-Defined Custom Regex Patterns to use in the redaction")]
        public InArgument<string> Formula { get; set; }

        [Category("Input"), Description("Array of prebuilt regex patterns.  Specify which regex patters to apply in the redaction.  Here are the available options.\r\n{\"ssn\",\"phone\",\"email\",\"dates\",\"ein\",\"phone\",\"currency\"}")]
        public InArgument<string[]> FormulaAuto { get; set; }

        [Category("Input"), Description("True = highlight matching words.  False = redact matching words.  Default: False")]
        public InArgument<bool> HighlightOnly { get; set; } = new InArgument<bool>(false);

        [Category("Input"), Description("An array of keywords to redact - useful for names of people or company")]
        public InArgument<string[]> Keywords { get; set; }

        [Category("Input"), Description("Choose the color for the redaction annotation.  Default: System.Drawing.Color.Black")]
        public InArgument<Color> RedactColor { get; set; } = new InArgument<Color>(Color.Black);

        [Category("Input"), Description("An Array of the Fields from the existing taxonomy, that you want to redact.")]
        public InArgument<string[]> RedactFields { get; set; }

        [Category("Input"), Description("If set to True, documents will process without any status reports to screen.  Default: False")]
        public InArgument<bool> Silent { get; set; } = new InArgument<bool>(false);

        [Category("Input"), Description("File to a PNG use as a watermark or logo on the redacted document.")]
        public InArgument<string> WaterMarkFile { get; set; }

        [Category("Input"), Description("Set the origin point for the Logo/Watermark.  e.g. New System.Drawing.Point(0,0)")]
        public InArgument<Point> WaterMarkLocation { get; set; } = new InArgument<Point>(new Point(0, 0));

        [Category("Input"), Description("Ouptut PDF File.  Relative Paths are not allowed.  Please provide a fully rooted path to the output file.")]
        [RequiredArgument]
        public InArgument<string> FileOutput { get; set; }


        protected override Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Get the input arguments
            var document = DocumentObjectModel.Get(context);
            var extractionResult = ExtractionResult.Get(context);
            var fileInput = FileInput.Get(context);
            var formula = Formula.Get(context);
            var formulaAuto = FormulaAuto.Get(context);
            var highlightOnly = HighlightOnly.Get(context);
            var keywords = Keywords.Get(context);
            var redactColor = RedactColor.Get(context);
            var redactFields = RedactFields.Get(context);
            var silent = Silent.Get(context);
            var waterMarkFile = WaterMarkFile.Get(context);
            var waterMarkLocation = WaterMarkLocation.Get(context);
            var fileOutput = FileOutput.Get(context);

            if (string.IsNullOrEmpty(fileInput) || !Path.IsPathRooted(fileInput) || !File.Exists(fileInput))
            {
                throw new Exception("Error: Input File Not Found! Please provide a fully rooted path for the FileInput argument.");
            }

            if (string.IsNullOrEmpty(fileOutput) || !Path.IsPathRooted(fileOutput) || !Directory.Exists(Path.GetDirectoryName(fileOutput)))
            {
                throw new Exception("Error: FileOutput argument must be a fully rooted path.");
            }

            if (fileInput == fileOutput)
            {
                throw new Exception("The output filename cannot be the same as the input filename: OutputFile.");
            }

            if (File.Exists(fileInput))
            {
                if (RedactColor == null || redactColor.IsEmpty || !redactColor.IsKnownColor)
                {
                    redactColor = Color.Black;
                }

                var filePaths = FilePathsProcessor.CreateFilePaths(fileInput, fileOutput);

                DuRedactionProcessor.PerformDomRedaction(filePaths.PathStripped, filePaths.PathWorking, document, formula, formulaAuto, keywords, highlightOnly, redactColor, silent, fileInput);

                if (DuRedactionProcessor.RequiresEomRedaction(extractionResult, redactFields, silent, filePaths.PathWorking))
                {
                    DuRedactionProcessor.PerformEomRedaction(filePaths.PathStripped, filePaths.PathWorking, extractionResult, redactFields, highlightOnly, redactColor, silent);
                }

                WatermarkProcessor.ApplyWatermark(waterMarkFile, waterMarkLocation, fileInput, fileOutput, silent, filePaths.PathWorking, filePaths.PathRedacted);

                PdfProcessor.ExportToPdf(document, filePaths.PathRedacted, fileOutput, silent);
            }
            else
            {
                Console.WriteLine("Warning: Exiting / Process Cancelled");
            }

            return Task.CompletedTask;
        }
    }
}