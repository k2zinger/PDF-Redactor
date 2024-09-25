using System;
using System.Data;
using System.Linq;
using UiPath.DocumentProcessing.Contracts;
using UiPath.DocumentProcessing.Contracts.Dom;
using UiPath.DocumentProcessing.Contracts.Results;

namespace UiPathTeam.PDFRedaction.Activities.Helpers;

public static class DocumentProcessor
{
    public static DataTable DomToDataTable(Document dom)
    {
        int X;
        int Y;
        int W;
        int H;
        int i = 0;
        int pageIndex;
        Box box;

        // Build DataTable for Words
        DataTable datatable = new("Document Words");
        DataRow dataRow;

        // Establish Column for Type
        DataColumn colType = new("Type")
        {
            DataType = Type.GetType("System.String")
        };
        datatable.Columns.Add(colType);

        // Establish Column for Page
        DataColumn colPage = new("Page")
        {
            DataType = Type.GetType("System.Int32")
        };
        datatable.Columns.Add(colPage);

        // Establish Column for Group
        DataColumn colGroup = new("Group")
        {
            DataType = Type.GetType("System.Int32")
        };
        datatable.Columns.Add(colGroup);

        // Establish Column for Word
        DataColumn colWord = new("Word")
        {
            DataType = Type.GetType("System.String")
        };
        datatable.Columns.Add(colWord);

        // Establish Column for X
        DataColumn colX = new("X")
        {
            DataType = Type.GetType("System.Int32")
        };
        datatable.Columns.Add(colX);

        // Establish Column for Y
        DataColumn colY = new("Y")
        {
            DataType = Type.GetType("System.Int32")
        };
        datatable.Columns.Add(colY);

        // Establish Column for W
        DataColumn colW = new("W")
        {
            DataType = Type.GetType("System.Int32")
        };
        datatable.Columns.Add(colW);

        // Establish Column for H
        DataColumn colH = new("H")
        {
            DataType = Type.GetType("System.Int32")
        };
        datatable.Columns.Add(colH);

        // Assume dom.pages is available as a list of pages
        foreach (Page page in dom.Pages)
        {
            pageIndex = page.PageIndex;
            box = page.Size;

            double currentPageWidth = box.Width;
            double currentPageHeight = box.Height;

            // Hardcode Scale for now - assumes Portrait of width <= Height
            double scale = currentPageWidth <= currentPageHeight ? 4.1666666667 : 3.78914;

            // Get Page Box
            X = (int)(box.Left * scale);
            Y = (int)(box.Top * scale);
            W = (int)(box.Width * scale);
            H = (int)(box.Height * scale);

            // Enter into Datatable
            dataRow = datatable.NewRow();
            dataRow["Type"] = "Page";
            dataRow["Page"] = pageIndex;
            dataRow["Word"] = "";
            dataRow["X"] = X;
            dataRow["Y"] = Y;
            dataRow["W"] = W;
            dataRow["H"] = H;
            datatable.Rows.Add(dataRow);

            // Get Page Words, Locations, Dimensions
            foreach (PageSection sect in page.Sections)
            {
                int j = 0;
                foreach (WordGroup wordGroup in sect.WordGroups)
                {
                    foreach (Word word in wordGroup.Words)
                    {
                        // Get Word Box
                        int groupIndex = j;
                        box = word.Box;
                        X = (int)Math.Floor(box.Left * scale);
                        Y = (int)Math.Floor(box.Top * scale);
                        W = (int)Math.Ceiling(box.Width * scale);
                        H = (int)Math.Ceiling(box.Height * scale);

                        // Enter into Datatable
                        dataRow = datatable.NewRow();
                        dataRow["Type"] = "Word";
                        dataRow["Page"] = pageIndex;
                        dataRow["Group"] = groupIndex;
                        dataRow["Word"] = word.Text;
                        dataRow["X"] = X;
                        dataRow["Y"] = Y;
                        dataRow["W"] = W;
                        dataRow["H"] = H;
                        datatable.Rows.Add(dataRow);
                    }
                    j++;
                }
            }
            i++;
        }

        return datatable;
    }
    public static DataTable EomToDataTable(ExtractionResult eom)
    {
        // General Variables
        DataRow dataRow;
        Box[] boxes;

        // Build DataTable for Header
        var dataTableHeader = new DataTable("Header");

        // Build DataTable for Fields
        var dataTableFields = new DataTable("Fields");

        // ------ HEADER COLUMNS ---------------------------------
        // Establish Column for Document Type
        DataColumn col = new("Type")
        {
            DataType = Type.GetType("System.String")
        };
        dataTableHeader.Columns.Add(col);

        // Establish Column for Page Count
        col = new DataColumn("Pages")
        {
            DataType = Type.GetType("System.Int32")
        };
        dataTableHeader.Columns.Add(col);

        // Establish Column for Page Confidence
        col = new DataColumn("Confidence")
        {
            DataType = Type.GetType("System.Double")
        };
        dataTableHeader.Columns.Add(col);

        // Establish Column for Page Width
        col = new DataColumn("Width")
        {
            DataType = Type.GetType("System.Double")
        };
        dataTableHeader.Columns.Add(col);

        // Establish Column for Page Height
        col = new DataColumn("Height")
        {
            DataType = Type.GetType("System.Double")
        };
        dataTableHeader.Columns.Add(col);

        // Get Header Information
        string docType;
        double docWidth = 0;
        double docHeight = 0;
        double docConfidence;
        int docPages = eom.ResultsDocument.Bounds.PageCount;

        int d = 0;

        try
        {
            // Console.WriteLine("1");
            docType = eom.ResultsDocument.DocumentTypeField.Value;
            // Console.WriteLine("2");
            docConfidence = eom.ResultsDocument.DocumentTypeField.Confidence;
            // Console.WriteLine("3");
            docHeight = eom.ResultsDocument.DocumentTypeField.Reference.Tokens[0].PageHeight;
            // Console.WriteLine("4");
            docWidth = eom.ResultsDocument.DocumentTypeField.Reference.Tokens[0].PageWidth;
            // Console.WriteLine("5");
        }
        catch (Exception)
        {
            docType = eom.GetDocumentType();
            docConfidence = eom.ResultsDocument.DocumentTypeField.Confidence;
            // Console.WriteLine("6");
            while (d < eom.ResultsDocument.Fields.Length)
            {
                if (!eom.ResultsDocument.Fields[d].IsMissing)
                {
                    docWidth = eom.ResultsDocument.Fields[d].Values[0].Reference.Tokens[0].PageWidth;
                    // Console.WriteLine("7");
                    docHeight = eom.ResultsDocument.Fields[d].Values[0].Reference.Tokens[0].PageHeight;
                    // Console.WriteLine("8");
                    break;
                }
                d++;
            }
        }

        // Hardcode Scale for now - assumes Portrait of width <= Height
        double Scale = docWidth <= docHeight ? 4.1666666667 : 3.78914;

        // Enter into Datatable
        dataRow = dataTableHeader.NewRow();
        dataRow["Type"] = docType;
        dataRow["Pages"] = docPages;
        dataRow["Confidence"] = docConfidence;
        dataRow["Width"] = docWidth;
        dataRow["Height"] = docHeight;
        dataTableHeader.Rows.Add(dataRow);

        // ------ FIELD COLUMNS  ---------------------------------

        // Establish Column for Field Name
        col = new DataColumn("Field")
        {
            DataType = Type.GetType("System.String")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for Field Value
        col = new DataColumn("Value")
        {
            DataType = Type.GetType("System.String")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for Is Numeric
        col = new DataColumn("Numeric")
        {
            DataType = Type.GetType("System.Boolean")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for Is Missing
        col = new DataColumn("Missing")
        {
            DataType = Type.GetType("System.Boolean")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for Confidence
        col = new DataColumn("Confidence")
        {
            DataType = Type.GetType("System.Double")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for Page No
        col = new DataColumn("Page")
        {
            DataType = Type.GetType("System.Int32")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for X
        col = new DataColumn("X")
        {
            DataType = Type.GetType("System.Int32")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for Y
        col = new DataColumn("Y")
        {
            DataType = Type.GetType("System.Int32")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for W
        col = new DataColumn("W")
        {
            DataType = Type.GetType("System.Int32")
        };
        dataTableFields.Columns.Add(col);

        // Establish Column for H
        col = new DataColumn("H")
        {
            DataType = Type.GetType("System.Int32")
        };
        dataTableFields.Columns.Add(col);

        // Get Fields
        string FieldName;
        bool FieldNumeric;
        string FieldValue;
        bool FieldMissing;
        double FieldConfidence;
        int PageNo;
        int X = 0;
        int Y = 0;
        int W = 0;
        int H = 0;

        foreach (var field in eom.ResultsDocument.Fields)
        {
            // Get this Field's Information
            FieldName = field.FieldName;
            FieldMissing = field.IsMissing;

            if (!FieldMissing)
            {
                foreach (var value in field.Values)
                {
                    // Some fields are not on any specific pages (e.g., Currency)
                    FieldValue = value.Value;
                    FieldConfidence = value.Confidence;
                    FieldNumeric = double.TryParse(FieldValue, out _);

                    // Initialize default values for PageNo and Boxes
                    PageNo = -1;
                    boxes = Array.Empty<Box>(); // Default empty array

                    // Check if Tokens exist and are not empty
                    if (value.Reference != null && value.Reference.Tokens != null && value.Reference.Tokens.Any())
                    {
                        PageNo = value.Reference.Tokens[0].Page;
                        boxes = value.Reference.Tokens[0].Boxes;
                    }

                    // If boxes are still empty, provide fallback logic
                    if (boxes == null || !boxes.Any())
                    {
                        // If there's no box information, set the coordinates to a default or error state
                        X = -1;
                        Y = -1;
                        W = -1;
                        H = -1;

                        // Enter into Datatable with default box values
                        dataRow = dataTableFields.NewRow();
                        dataRow["Field"] = FieldName;
                        dataRow["Value"] = FieldValue;
                        dataRow["Numeric"] = FieldNumeric;
                        dataRow["Missing"] = FieldMissing;
                        dataRow["Confidence"] = FieldConfidence;
                        dataRow["Page"] = PageNo;
                        dataRow["X"] = X;
                        dataRow["Y"] = Y;
                        dataRow["W"] = W;
                        dataRow["H"] = H;
                        dataTableFields.Rows.Add(dataRow);
                    }
                    else
                    {
                        // Iterate over boxes if they are available
                        foreach (var box in boxes)
                        {
                            try
                            {
                                X = (int)(box.Left * Scale);
                                Y = (int)(box.Top * Scale);
                                W = (int)(box.Width * Scale);
                                H = (int)(box.Height * Scale);
                            }
                            catch (Exception)
                            {
                                // Handle exceptions gracefully
                                X = -1;
                                Y = -1;
                                W = -1;
                                H = -1;
                            }
                            finally
                            {
                                // Enter into Datatable
                                dataRow = dataTableFields.NewRow();
                                dataRow["Field"] = FieldName;
                                dataRow["Value"] = FieldValue;
                                dataRow["Numeric"] = FieldNumeric;
                                dataRow["Missing"] = FieldMissing;
                                dataRow["Confidence"] = FieldConfidence;
                                dataRow["Page"] = PageNo;
                                dataRow["X"] = X;
                                dataRow["Y"] = Y;
                                dataRow["W"] = W;
                                dataRow["H"] = H;
                                dataTableFields.Rows.Add(dataRow);
                            }
                        }
                    }
                }
            }
        }

        return dataTableFields;
    }
    public static DataTable FilterDataTable(DataTable dataTable, object filterColumnValue)
    {
        var pageColumnName = "Page";

        // Validate the input parameters
        if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));
        if (!dataTable.Columns.Contains(pageColumnName)) throw new ArgumentException("The column does not exist in the DataTable.", pageColumnName);

        // Copy the original DataTable structure
        DataTable filteredTable = dataTable.Clone();

        // Filter rows based on pageColumnName and pageColumnValue
        foreach (DataRow row in dataTable.Rows)
        {
            if (row[pageColumnName].Equals(filterColumnValue))
            {
                filteredTable.ImportRow(row);
            }
        }

        return filteredTable;
    }
}