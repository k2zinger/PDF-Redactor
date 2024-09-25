using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.OCR.Contracts;
using UiPath.Shared.Activities;
using UiPathTeam.PDFRedaction.Activities.Helpers;
using UiPathTeam.PDFRedaction.Activities.Models;

namespace UiPathTeam.PDFRedaction.Activities
{
    [DisplayName("PDF Redaction"), Description("This activity redacts PDF Documents without the use of any other third party desktop software. ")]
    public class PDFRedaction : AsyncTaskNativeActivity
    {
        [Category("Input"), Description("Input File.  Relative Paths are not allowed.  Please provide a fully rooted path to the input file.")]
        [RequiredArgument]
        public InArgument<string> FileInput { get; set; }

        [Category("Input"), Description("User-Defined Custom Regex Patterns to use in the redaction")]
        public InArgument<string> Formula { get; set; }

        [Category("Input"), Description("Array of prebuilt regex patterns.  Specify which regex patters to apply in the redaction.  Here are the available options.\r\n{\"ssn\",\"phone\",\"email\",\"dates\",\"ein\",\"phone\",\"currency\"}")]
        public InArgument<string[]> FormulaAuto { get; set; }

        [Category("Input"), Description("An array of keywords to redact - useful for names of people or company")]
        public InArgument<string[]> Keywords { get; set; }

        [Category("Input"), Description("True = highlight matching words.  False = redact matching words.  Default: False")]
        public InArgument<bool> HighlightOnly { get; set; } = new InArgument<bool>(false);

        [Category("Input"), Description("Choose the color for the redaction annotation.  Default: System.Drawing.Color.Black")]
        public InArgument<Color> RedactColor { get; set; } = new InArgument<Color>(Color.Black);

        [Category("Input"), Description("If set to True, documents will process without any status reports to screen.  Default: False")]
        public InArgument<bool> Silent { get; set; } = new InArgument<bool>(false);

        [Category("Input"), Description("Ouptut PDF File.  Relative Paths are not allowed.  Please provide a fully rooted path to the output file.")]
        [RequiredArgument]
        public InArgument<string> FileOutput { get; set; }

        [Browsable(false)]
        public ActivityFunc<Image, IEnumerable<KeyValuePair<Rectangle, string>>> OCREngine { get; set; }

        public PDFRedaction()
        {
            this.OCREngine = new ActivityFunc<Image, IEnumerable<KeyValuePair<Rectangle, string>>>()
            {
                Argument = new DelegateInArgument<Image>("Image")
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(OCREngine);
            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            // Get the input arguments
            var fileInput = FileInput.Get(context);
            var formula = Formula.Get(context);
            var formulaAuto = FormulaAuto.Get(context);
            var highlightOnly = HighlightOnly.Get(context);
            var keywords = Keywords.Get(context);
            var redactColor = RedactColor.Get(context);
            var silent = Silent.Get(context);
            var fileOutput = FileOutput.Get(context);
            var method = highlightOnly ? "Highlight" : "Redact";
            var ocrEngine = OCREngine.Handler as IOCREngineActivity;

            if (string.IsNullOrEmpty(fileInput) || !Path.IsPathRooted(fileInput) || !File.Exists(fileInput))
            {
                throw new Exception("Error: Input File Not Found! Please provide a fully rooted path for the FileInput argument");
            }

            if (string.IsNullOrEmpty(fileOutput) || !Path.IsPathRooted(fileOutput) || !Directory.Exists(Path.GetDirectoryName(fileOutput)))
            {
                throw new Exception("Error: FileOutput argument must be a fully rooted path.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileOutput)))
            {
                throw new Exception("Error: FileOutput directory path does not exist.");
            }

            if (RedactColor == null || redactColor.IsEmpty || !redactColor.IsKnownColor)
            {
                redactColor = Color.Black;
            }

            var processType = GetProcessType(fileInput, silent);
            var filePaths = FilePathsProcessor.CreateFilePaths(fileInput, fileOutput);

            switch (processType)
            {
                case ProcessType.Img:
                    await PdfRedactionProcessor.PerformImgRedactionAsync(fileInput, string.Empty, fileOutput, string.Empty, formula, keywords, formulaAuto, redactColor,
                        highlightOnly, filePaths.PathWorking, method, ocrEngine, silent);
                    break;
                case ProcessType.Pdf:
                    await PdfRedactionProcessor.PerformPdfRedactionAsync(fileInput, fileOutput, formula, keywords, formulaAuto, redactColor, highlightOnly,
                        filePaths.PathWorking, filePaths.PathStripped, method, ocrEngine, silent);
                    break;
                default:
                    Console.WriteLine("Exiting Redaction / Cancelled");
                    break;
            }

            return ctx => { };
        }

        public static ProcessType GetProcessType(string fileInput, bool silent)
        {
            var fileExtension = Path.GetExtension(fileInput).ToLower();
            if (!silent)
            {
                Console.WriteLine("File Type: " + fileExtension.ToUpper());
            }

            ProcessType processType;
            switch (fileExtension)
            {
                case ".pdf":
                    processType = ProcessType.Pdf;
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                    processType = ProcessType.Img;
                    break;
                default:
                    return ProcessType.Default;
            }

            if (!silent && processType != ProcessType.Default)
            {
                Console.WriteLine("Preparing to process " + processType);
            }

            return processType;
        }
    }
}