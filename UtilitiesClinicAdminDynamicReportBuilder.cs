using PersonalProject.Models.DTOs;
using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace PersonalProject.Utilities
{
    public static class ClinicAdminDynamicReportBuilder
    {
        private const string Teal = "0F766E";
        private const string TealLight = "CCFBF1";
        private const string Slate = "0F172A";
        private const string SlateSoft = "475569";
        private const string Border = "CBD5E1";
        private const string Surface = "F8FAFC";
        private const string White = "FFFFFF";
        private const string Green = "DCFCE7";
        private const string GreenText = "166534";
        private const string Amber = "FEF3C7";
        private const string AmberText = "92400E";
        private const string Red = "FEE2E2";
        private const string RedText = "991B1B";

        public static byte[] BuildExcel(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            using var stream = new MemoryStream();

            using (
                var archive =
                    new ZipArchive(
                        stream,
                        ZipArchiveMode.Create,
                        leaveOpen: true
                    )
            )
            {
                WriteEntry(
                    archive,
                    "[Content_Types].xml",
                    ContentTypesXml()
                );

                WriteEntry(
                    archive,
                    "_rels/.rels",
                    RootRelsXml()
                );

                WriteEntry(
                    archive,
                    "xl/workbook.xml",
                    WorkbookXml()
                );

                WriteEntry(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    WorkbookRelsXml()
                );

                WriteEntry(
                    archive,
                    "xl/styles.xml",
                    StylesXml()
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    BuildDashboardSheet(report)
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/sheet2.xml",
                    BuildDataSheet(report)
                );
            }

            return stream.ToArray();
        }

        public static byte[] BuildPdf(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var pages =
                BuildPdfPages(report);

            return PdfDocumentBuilder.Build(
                pages
            );
        }

        // =====================================================
        // EXCEL
        // =====================================================

        private static string BuildDashboardSheet(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var sheet =
                new SheetXmlBuilder();

            var columnCount =
                Math.Max(
                    6,
                    report.Columns.Count
                );

            sheet.SetWidths(
                Enumerable
                    .Range(
                        1,
                        columnCount
                    )
                    .Select(
                        index =>
                            index == 1
                                ? 22d
                                : 16d
                    )
                    .ToArray()
            );

            sheet.Merge(
                1,
                1,
                1,
                columnCount
            );

            sheet.Cell(
                1,
                1,
                "PHILALINK CLINIC REPORT",
                1
            );

            sheet.Merge(
                2,
                1,
                2,
                columnCount
            );

            sheet.Cell(
                2,
                1,
                report.Title,
                2
            );

            sheet.Merge(
                3,
                1,
                3,
                columnCount
            );

            sheet.Cell(
                3,
                1,
                $"{report.ClinicName}  |  Requested by {report.RequestedBy}  |  Generated {report.GeneratedAt:yyyy-MM-dd HH:mm}",
                3
            );

            sheet.Merge(
                4,
                1,
                4,
                columnCount
            );

            sheet.Cell(
                4,
                1,
                RangeText(report),
                4
            );

            var summary =
                report.Summary
                    .Take(6)
                    .ToList();

            var summaryStart =
                6;

            for (
                var index = 0;
                index < summary.Count;
                index++
            )
            {
                var column =
                    index * 2 + 1;

                if (
                    column >
                    columnCount
                )
                {
                    break;
                }

                var endColumn =
                    Math.Min(
                        column + 1,
                        columnCount
                    );

                sheet.Merge(
                    summaryStart,
                    column,
                    summaryStart,
                    endColumn
                );

                sheet.Cell(
                    summaryStart,
                    column,
                    summary[index].Label,
                    5
                );

                sheet.Merge(
                    summaryStart + 1,
                    column,
                    summaryStart + 1,
                    endColumn
                );

                sheet.Cell(
                    summaryStart + 1,
                    column,
                    summary[index].Value,
                    6
                );
            }

            var tableTitleRow =
                10;

            sheet.Merge(
                tableTitleRow,
                1,
                tableTitleRow,
                columnCount
            );

            sheet.Cell(
                tableTitleRow,
                1,
                "FILTERED REPORT PREVIEW",
                7
            );

            var headerRow =
                tableTitleRow + 1;

            var previewColumns =
                report.Columns
                    .Take(
                        Math.Min(
                            report.Columns.Count,
                            columnCount
                        )
                    )
                    .ToList();

            for (
                var index = 0;
                index < previewColumns.Count;
                index++
            )
            {
                sheet.Cell(
                    headerRow,
                    index + 1,
                    previewColumns[index].Label,
                    8
                );
            }

            var previewRows =
                report.Rows
                    .Take(18)
                    .ToList();

            for (
                var rowIndex = 0;
                rowIndex < previewRows.Count;
                rowIndex++
            )
            {
                var row =
                    previewRows[rowIndex];

                for (
                    var columnIndex = 0;
                    columnIndex < previewColumns.Count;
                    columnIndex++
                )
                {
                    var column =
                        previewColumns[columnIndex];

                    row.TryGetValue(
                        column.Key,
                        out var value
                    );

                    sheet.Cell(
                        headerRow + 1 + rowIndex,
                        columnIndex + 1,
                        DisplayValue(
                            value,
                            column.DataType
                        ),
                        rowIndex % 2 == 0
                            ? 9
                            : 10
                    );
                }
            }

            if (
                report.Rows.Count >
                previewRows.Count
            )
            {
                var noteRow =
                    headerRow +
                    previewRows.Count +
                    2;

                sheet.Merge(
                    noteRow,
                    1,
                    noteRow,
                    columnCount
                );

                sheet.Cell(
                    noteRow,
                    1,
                    $"Preview shows {previewRows.Count} of {report.Rows.Count} rows. See the Data worksheet for the full filtered dataset.",
                    11
                );
            }

            sheet.FreezeRows =
                headerRow;

            return sheet.Build();
        }

        private static string BuildDataSheet(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var sheet =
                new SheetXmlBuilder();

            sheet.SetWidths(
                report.Columns
                    .Select(
                        column =>
                            GetColumnWidth(
                                column.Label
                            )
                    )
                    .ToArray()
            );

            sheet.Merge(
                1,
                1,
                1,
                Math.Max(
                    1,
                    report.Columns.Count
                )
            );

            sheet.Cell(
                1,
                1,
                report.Title,
                1
            );

            sheet.Merge(
                2,
                1,
                2,
                Math.Max(
                    1,
                    report.Columns.Count
                )
            );

            sheet.Cell(
                2,
                1,
                $"{report.ClinicName} | {RangeText(report)} | Requested by {report.RequestedBy}",
                3
            );

            var headerRow =
                4;

            for (
                var index = 0;
                index < report.Columns.Count;
                index++
            )
            {
                sheet.Cell(
                    headerRow,
                    index + 1,
                    report.Columns[index].Label,
                    8
                );
            }

            for (
                var rowIndex = 0;
                rowIndex < report.Rows.Count;
                rowIndex++
            )
            {
                var row =
                    report.Rows[rowIndex];

                for (
                    var columnIndex = 0;
                    columnIndex < report.Columns.Count;
                    columnIndex++
                )
                {
                    var column =
                        report.Columns[columnIndex];

                    row.TryGetValue(
                        column.Key,
                        out var value
                    );

                    var excelValue =
                        NormalizeExcelValue(
                            value,
                            column.DataType
                        );

                    var style =
                        GetExcelDataStyle(
                            column.DataType,
                            value,
                            rowIndex
                        );

                    sheet.Cell(
                        headerRow + 1 + rowIndex,
                        columnIndex + 1,
                        excelValue,
                        style
                    );
                }
            }

            if (
                report.Rows.Count >
                0 &&
                report.Columns.Count >
                0
            )
            {
                sheet.AutoFilterRef =
                    $"A{headerRow}:{ColumnName(report.Columns.Count)}{headerRow + report.Rows.Count}";
            }

            sheet.FreezeRows =
                headerRow;

            return sheet.Build();
        }

        private static object? NormalizeExcelValue(
            object? value,
            string dataType
        )
        {
            if (value == null)
            {
                return "";
            }

            if (
                dataType.Equals(
                    "number",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    decimal.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo.InvariantCulture
                        ),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var number
                    )
                )
                {
                    return number;
                }
            }

            if (
                dataType.Equals(
                    "date",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                dataType.Equals(
                    "datetime",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    value is DateTime date
                )
                {
                    return date;
                }

                if (
                    DateTime.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo.InvariantCulture
                        ),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal |
                        DateTimeStyles.AdjustToUniversal,
                        out var parsed
                    )
                )
                {
                    return parsed;
                }
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ) ?? "";
        }

        private static int GetExcelDataStyle(
            string dataType,
            object? value,
            int rowIndex
        )
        {
            if (
                dataType.Equals(
                    "date",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return 12;
            }

            if (
                dataType.Equals(
                    "datetime",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return 13;
            }

            if (
                dataType.Equals(
                    "number",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return 14;
            }

            if (
                dataType.Equals(
                    "status",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var status =
                    Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture
                    ) ?? "";

                if (
                    IsPositiveStatus(
                        status
                    )
                )
                {
                    return 15;
                }

                if (
                    IsWarningStatus(
                        status
                    )
                )
                {
                    return 16;
                }

                if (
                    IsNegativeStatus(
                        status
                    )
                )
                {
                    return 17;
                }
            }

            return rowIndex % 2 == 0
                ? 9
                : 10;
        }

        private static string ContentTypesXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """;
        }

        private static string RootRelsXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """;
        }

        private static string WorkbookXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Dashboard" sheetId="1" r:id="rId1"/>
                    <sheet name="Data" sheetId="2" r:id="rId2"/>
                  </sheets>
                </workbook>
                """;
        }

        private static string WorkbookRelsXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
                  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """;
        }

        private static string StylesXml()
        {
            return $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <numFmts count="2">
                    <numFmt numFmtId="164" formatCode="yyyy-mm-dd"/>
                    <numFmt numFmtId="165" formatCode="yyyy-mm-dd hh:mm"/>
                  </numFmts>
                  <fonts count="5">
                    <font><sz val="10"/><name val="Calibri"/><color rgb="FF{Slate}"/></font>
                    <font><b/><sz val="20"/><name val="Calibri"/><color rgb="FF{White}"/></font>
                    <font><b/><sz val="13"/><name val="Calibri"/><color rgb="FF{White}"/></font>
                    <font><b/><sz val="9"/><name val="Calibri"/><color rgb="FF{SlateSoft}"/></font>
                    <font><b/><sz val="15"/><name val="Calibri"/><color rgb="FF{Slate}"/></font>
                  </fonts>
                  <fills count="9">
                    <fill><patternFill patternType="none"/></fill>
                    <fill><patternFill patternType="gray125"/></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{Teal}"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{TealLight}"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{Surface}"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{Green}"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{Amber}"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{Red}"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF{White}"/><bgColor indexed="64"/></patternFill></fill>
                  </fills>
                  <borders count="2">
                    <border><left/><right/><top/><bottom/><diagonal/></border>
                    <border>
                      <left style="thin"><color rgb="FF{Border}"/></left>
                      <right style="thin"><color rgb="FF{Border}"/></right>
                      <top style="thin"><color rgb="FF{Border}"/></top>
                      <bottom style="thin"><color rgb="FF{Border}"/></bottom>
                      <diagonal/>
                    </border>
                  </borders>
                  <cellStyleXfs count="1">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
                  </cellStyleXfs>
                  <cellXfs count="18">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                    <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="4" borderId="0" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="3" fillId="3" borderId="0" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="3" fillId="4" borderId="1" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="4" fillId="8" borderId="1" xfId="0"><alignment vertical="center" horizontal="center"/></xf>
                    <xf numFmtId="0" fontId="3" fillId="3" borderId="1" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="8" borderId="1" xfId="0"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="4" borderId="1" xfId="0"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="3" borderId="1" xfId="0"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="164" fontId="0" fillId="8" borderId="1" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="165" fontId="0" fillId="8" borderId="1" xfId="0"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="8" borderId="1" xfId="0"><alignment vertical="center" horizontal="right"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="5" borderId="1" xfId="0"><alignment vertical="center" horizontal="center"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="6" borderId="1" xfId="0"><alignment vertical="center" horizontal="center"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="7" borderId="1" xfId="0"><alignment vertical="center" horizontal="center"/></xf>
                  </cellXfs>
                  <cellStyles count="1">
                    <cellStyle name="Normal" xfId="0" builtinId="0"/>
                  </cellStyles>
                </styleSheet>
                """;
        }

        // =====================================================
        // PDF
        // =====================================================

        private static List<string> BuildPdfPages(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var result =
                new List<string>();

            var firstPage =
                new PdfCanvas();

            firstPage.BrandHeader(
                report.Title,
                report.ClinicName
            );

            firstPage.Text(
                32,
                78,
                8,
                RangeText(report),
                false,
                "#475569"
            );

            firstPage.Text(
                32,
                92,
                8,
                $"Requested by: {report.RequestedBy}",
                false,
                "#475569"
            );

            firstPage.Text(
                810,
                92,
                8,
                $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm}",
                false,
                "#475569",
                rightAlign: true
            );

            var summary =
                report.Summary
                    .Take(4)
                    .ToList();

            var cardWidth =
                186d;

            for (
                var index = 0;
                index < summary.Count;
                index++
            )
            {
                var x =
                    32 +
                    index *
                    (cardWidth + 10);

                firstPage.Card(
                    x,
                    118,
                    cardWidth,
                    62,
                    summary[index].Label,
                    summary[index].Value
                );
            }

            firstPage.Text(
                32,
                206,
                10,
                "Filtered report data",
                true,
                "#0F172A"
            );

            var maxPreviewColumns =
                Math.Min(
                    6,
                    report.Columns.Count
                );

            var previewColumns =
                report.Columns
                    .Take(
                        maxPreviewColumns
                    )
                    .ToList();

            DrawPdfTable(
                firstPage,
                report,
                previewColumns,
                report.Rows
                    .Take(14)
                    .ToList(),
                startY: 226,
                footerText:
                    report.Rows.Count > 14
                        ? $"Previewing 14 of {report.Rows.Count} rows. Full data continues on following pages."
                        : $"{report.Rows.Count} rows."
            );

            result.Add(
                firstPage.Content
            );

            var detailColumns =
                report.Columns
                    .Take(8)
                    .ToList();

            const int rowsPerPage =
                22;

            for (
                var offset = 0;
                offset < report.Rows.Count;
                offset += rowsPerPage
            )
            {
                var page =
                    new PdfCanvas();

                page.BrandHeader(
                    report.Title,
                    report.ClinicName
                );

                page.Text(
                    32,
                    78,
                    8,
                    $"Detailed filtered data | {RangeText(report)}",
                    false,
                    "#475569"
                );

                DrawPdfTable(
                    page,
                    report,
                    detailColumns,
                    report.Rows
                        .Skip(offset)
                        .Take(rowsPerPage)
                        .ToList(),
                    startY: 104,
                    footerText:
                        $"Rows {offset + 1}-{Math.Min(offset + rowsPerPage, report.Rows.Count)} of {report.Rows.Count}"
                );

                result.Add(
                    page.Content
                );
            }

            if (
                report.Rows.Count == 0
            )
            {
                var emptyPage =
                    new PdfCanvas();

                emptyPage.BrandHeader(
                    report.Title,
                    report.ClinicName
                );

                emptyPage.Text(
                    32,
                    110,
                    12,
                    "No rows matched the selected filters.",
                    true,
                    "#0F172A"
                );

                result.Add(
                    emptyPage.Content
                );
            }

            for (
                var index = 0;
                index < result.Count;
                index++
            )
            {
                var canvas =
                    new PdfCanvas(
                        result[index]
                    );

                canvas.Footer(
                    index + 1,
                    result.Count,
                    report.ClinicName
                );

                result[index] =
                    canvas.Content;
            }

            return result;
        }

        private static void DrawPdfTable(
            PdfCanvas page,
            ClinicAdminDynamicReportPreviewDto report,
            List<ClinicAdminDynamicReportColumnDto> columns,
            List<Dictionary<string, object?>> rows,
            double startY,
            string footerText
        )
        {
            if (
                columns.Count == 0
            )
            {
                return;
            }

            const double left =
                32;

            const double tableWidth =
                778;

            const double headerHeight =
                24;

            const double rowHeight =
                19;

            var columnWidth =
                tableWidth /
                columns.Count;

            page.FillRect(
                left,
                startY,
                tableWidth,
                headerHeight,
                "#0F766E"
            );

            for (
                var index = 0;
                index < columns.Count;
                index++
            )
            {
                page.Text(
                    left +
                    index *
                    columnWidth +
                    5,
                    startY + 7,
                    6.7,
                    Truncate(
                        columns[index].Label,
                        18
                    ),
                    true,
                    "#FFFFFF"
                );
            }

            var y =
                startY +
                headerHeight;

            for (
                var rowIndex = 0;
                rowIndex < rows.Count;
                rowIndex++
            )
            {
                var row =
                    rows[rowIndex];

                page.FillRect(
                    left,
                    y,
                    tableWidth,
                    rowHeight,
                    rowIndex % 2 == 0
                        ? "#FFFFFF"
                        : "#F8FAFC"
                );

                page.Line(
                    left,
                    y + rowHeight,
                    left + tableWidth,
                    y + rowHeight,
                    "#E2E8F0",
                    0.45
                );

                for (
                    var columnIndex = 0;
                    columnIndex < columns.Count;
                    columnIndex++
                )
                {
                    var column =
                        columns[columnIndex];

                    row.TryGetValue(
                        column.Key,
                        out var value
                    );

                    var text =
                        DisplayValue(
                            value,
                            column.DataType
                        );

                    var color =
                        column.DataType.Equals(
                            "status",
                            StringComparison.OrdinalIgnoreCase
                        )
                            ? GetStatusPdfColor(
                                text
                            )
                            : "#334155";

                    page.Text(
                        left +
                        columnIndex *
                        columnWidth +
                        5,
                        y + 6,
                        6.4,
                        Truncate(
                            text,
                            24
                        ),
                        false,
                        color
                    );
                }

                y +=
                    rowHeight;
            }

            page.Text(
                left,
                y + 12,
                6.8,
                footerText,
                false,
                "#64748B"
            );
        }

        // =====================================================
        // SHARED HELPERS
        // =====================================================

        private static string RangeText(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            if (
                report.DateFrom == null &&
                report.DateTo == null
            )
            {
                return "Current clinic inventory / current-state report";
            }

            var from =
                report.DateFrom?
                    .ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    ) ??
                "Start";

            var to =
                report.DateTo?
                    .ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    ) ??
                "Today";

            return $"Reporting period: {from} to {to}";
        }

        private static string DisplayValue(
            object? value,
            string dataType
        )
        {
            if (value == null)
            {
                return "—";
            }

            if (
                dataType.Equals(
                    "date",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    value is DateTime date
                )
                {
                    return date.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    );
                }

                if (
                    DateTime.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo.InvariantCulture
                        ),
                        out var parsed
                    )
                )
                {
                    return parsed.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    );
                }
            }

            if (
                dataType.Equals(
                    "datetime",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    value is DateTime dateTime
                )
                {
                    return dateTime.ToString(
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture
                    );
                }

                if (
                    DateTime.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo.InvariantCulture
                        ),
                        out var parsedDateTime
                    )
                )
                {
                    return parsedDateTime.ToString(
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture
                    );
                }
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ) ?? "—";
        }

        private static bool IsPositiveStatus(
            string status
        )
        {
            var value =
                status.ToLowerInvariant();

            return value.Contains("active") ||
                   value.Contains("healthy") ||
                   value.Contains("collected") ||
                   value.Contains("completed") ||
                   value.Contains("confirmed") ||
                   value.Contains("taken");
        }

        private static bool IsWarningStatus(
            string status
        )
        {
            var value =
                status.ToLowerInvariant();

            return value.Contains("pending") ||
                   value.Contains("scheduled") ||
                   value.Contains("low stock");
        }

        private static bool IsNegativeStatus(
            string status
        )
        {
            var value =
                status.ToLowerInvariant();

            return value.Contains("cancel") ||
                   value.Contains("missed") ||
                   value.Contains("inactive") ||
                   value.Contains("overdue");
        }

        private static string GetStatusPdfColor(
            string status
        )
        {
            if (
                IsPositiveStatus(
                    status
                )
            )
            {
                return "#166534";
            }

            if (
                IsWarningStatus(
                    status
                )
            )
            {
                return "#92400E";
            }

            if (
                IsNegativeStatus(
                    status
                )
            )
            {
                return "#991B1B";
            }

            return "#334155";
        }

        private static double GetColumnWidth(
            string label
        )
        {
            return Math.Max(
                12,
                Math.Min(
                    28,
                    label.Length +
                    6
                )
            );
        }

        private static string Truncate(
            string? value,
            int max
        )
        {
            var text =
                value ?? "";

            if (
                text.Length <=
                max
            )
            {
                return text;
            }

            return text[
                ..Math.Max(
                    1,
                    max - 3
                )
            ] + "...";
        }

        private static string ColumnName(
            int index
        )
        {
            var result =
                "";

            while (
                index >
                0
            )
            {
                index--;

                result =
                    (char)(
                        'A' +
                        index %
                        26
                    ) +
                    result;

                index /=
                    26;
            }

            return result;
        }

        private static void WriteEntry(
            ZipArchive archive,
            string path,
            string content
        )
        {
            var entry =
                archive.CreateEntry(
                    path,
                    CompressionLevel.Fastest
                );

            using var stream =
                entry.Open();

            using var writer =
                new StreamWriter(
                    stream,
                    new UTF8Encoding(false)
                );

            writer.Write(
                content
            );
        }

        // =====================================================
        // EXCEL XML WRITER
        // =====================================================

        private sealed class SheetXmlBuilder
        {
            private readonly SortedDictionary<int, List<CellDefinition>>
                _rows =
                    new();

            private readonly List<string>
                _merges =
                    new();

            private double[] _widths =
                Array.Empty<double>();

            public int FreezeRows { get; set; }

            public string? AutoFilterRef { get; set; }

            public void SetWidths(
                params double[] widths
            )
            {
                _widths =
                    widths;
            }

            public void Merge(
                int row1,
                int column1,
                int row2,
                int column2
            )
            {
                _merges.Add(
                    $"{ColumnName(column1)}{row1}:{ColumnName(column2)}{row2}"
                );
            }

            public void Cell(
                int row,
                int column,
                object? value,
                int style
            )
            {
                if (
                    !_rows.TryGetValue(
                        row,
                        out var cells
                    )
                )
                {
                    cells =
                        new List<CellDefinition>();

                    _rows[row] =
                        cells;
                }

                cells.Add(
                    new CellDefinition(
                        column,
                        value,
                        style
                    )
                );
            }

            public string Build()
            {
                var builder =
                    new StringBuilder();

                builder.Append(
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                    """
                );

                if (
                    FreezeRows >
                    0
                )
                {
                    builder.Append(
                        $"""
                        <sheetViews>
                          <sheetView workbookViewId="0">
                            <pane ySplit="{FreezeRows}" topLeftCell="A{FreezeRows + 1}" activePane="bottomLeft" state="frozen"/>
                          </sheetView>
                        </sheetViews>
                        """
                    );
                }

                if (
                    _widths.Length >
                    0
                )
                {
                    builder.Append(
                        "<cols>"
                    );

                    for (
                        var index = 0;
                        index < _widths.Length;
                        index++
                    )
                    {
                        builder.Append(
                            $"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{_widths[index].ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>"
                        );
                    }

                    builder.Append(
                        "</cols>"
                    );
                }

                builder.Append(
                    "<sheetData>"
                );

                foreach (
                    var row in _rows
                )
                {
                    builder.Append(
                        $"<row r=\"{row.Key}\">"
                    );

                    foreach (
                        var cell in row.Value
                            .OrderBy(
                                item =>
                                    item.Column
                            )
                    )
                    {
                        builder.Append(
                            BuildCellXml(
                                row.Key,
                                cell
                            )
                        );
                    }

                    builder.Append(
                        "</row>"
                    );
                }

                builder.Append(
                    "</sheetData>"
                );

                if (
                    _merges.Count >
                    0
                )
                {
                    builder.Append(
                        $"<mergeCells count=\"{_merges.Count}\">"
                    );

                    foreach (
                        var merge in _merges
                    )
                    {
                        builder.Append(
                            $"<mergeCell ref=\"{merge}\"/>"
                        );
                    }

                    builder.Append(
                        "</mergeCells>"
                    );
                }

                if (
                    !string.IsNullOrWhiteSpace(
                        AutoFilterRef
                    )
                )
                {
                    builder.Append(
                        $"<autoFilter ref=\"{AutoFilterRef}\"/>"
                    );
                }

                builder.Append(
                    """
                    <pageMargins left="0.3" right="0.3" top="0.5" bottom="0.5" header="0.2" footer="0.2"/>
                    <pageSetup orientation="landscape" fitToWidth="1" fitToHeight="0"/>
                    </worksheet>
                    """
                );

                return builder.ToString();
            }

            private static string BuildCellXml(
                int row,
                CellDefinition cell
            )
            {
                var reference =
                    $"{ColumnName(cell.Column)}{row}";

                if (
                    cell.Value is DateTime date
                )
                {
                    var serial =
                        date.ToOADate()
                            .ToString(
                                "0.########",
                                CultureInfo.InvariantCulture
                            );

                    return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"n\"><v>{serial}</v></c>";
                }

                if (
                    cell.Value is decimal ||
                    cell.Value is int ||
                    cell.Value is long ||
                    cell.Value is double ||
                    cell.Value is float
                )
                {
                    var number =
                        Convert.ToString(
                            cell.Value,
                            CultureInfo.InvariantCulture
                        ) ?? "0";

                    return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"n\"><v>{number}</v></c>";
                }

                var text =
                    SecurityElement.Escape(
                        Convert.ToString(
                            cell.Value,
                            CultureInfo.InvariantCulture
                        ) ?? ""
                    ) ?? "";

                return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{text}</t></is></c>";
            }

            private sealed record CellDefinition(
                int Column,
                object? Value,
                int Style
            );
        }

        // =====================================================
        // PDF WRITER
        // =====================================================

        private sealed class PdfCanvas
        {
            private const double PageWidth =
                842;

            private const double PageHeight =
                595;

            private readonly StringBuilder _builder;

            public PdfCanvas()
            {
                _builder =
                    new StringBuilder();
            }

            public PdfCanvas(
                string existing
            )
            {
                _builder =
                    new StringBuilder(
                        existing
                    );
            }

            public string Content =>
                _builder.ToString();

            public void BrandHeader(
                string title,
                string clinicName
            )
            {
                FillRect(
                    0,
                    0,
                    PageWidth,
                    58,
                    "#0F766E"
                );

                Text(
                    32,
                    16,
                    8,
                    "PHILALINK",
                    true,
                    "#CCFBF1"
                );

                Text(
                    32,
                    31,
                    15,
                    title,
                    true,
                    "#FFFFFF"
                );

                Text(
                    810,
                    31,
                    8,
                    clinicName,
                    true,
                    "#FFFFFF",
                    rightAlign: true
                );
            }

            public void Card(
                double x,
                double y,
                double width,
                double height,
                string label,
                string value
            )
            {
                FillRect(
                    x,
                    y,
                    width,
                    height,
                    "#F8FAFC"
                );

                StrokeRect(
                    x,
                    y,
                    width,
                    height,
                    "#CBD5E1",
                    0.7
                );

                FillRect(
                    x,
                    y,
                    5,
                    height,
                    "#0F766E"
                );

                Text(
                    x + 14,
                    y + 15,
                    7,
                    label,
                    true,
                    "#64748B"
                );

                Text(
                    x + 14,
                    y + 34,
                    17,
                    value,
                    true,
                    "#0F172A"
                );
            }

            public void Footer(
                int page,
                int total,
                string clinic
            )
            {
                Line(
                    32,
                    560,
                    810,
                    560,
                    "#CBD5E1",
                    0.5
                );

                Text(
                    32,
                    570,
                    6.5,
                    $"PhilaLink | {clinic} | Confidential clinic report",
                    false,
                    "#64748B"
                );

                Text(
                    810,
                    570,
                    6.5,
                    $"Page {page} of {total}",
                    true,
                    "#64748B",
                    rightAlign: true
                );
            }

            public void FillRect(
                double x,
                double y,
                double width,
                double height,
                string color
            )
            {
                var rgb =
                    Rgb(color);

                var pdfY =
                    PageHeight -
                    y -
                    height;

                _builder.AppendLine(
                    $"q {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} rg {N(x)} {N(pdfY)} {N(width)} {N(height)} re f Q"
                );
            }

            public void StrokeRect(
                double x,
                double y,
                double width,
                double height,
                string color,
                double lineWidth
            )
            {
                var rgb =
                    Rgb(color);

                var pdfY =
                    PageHeight -
                    y -
                    height;

                _builder.AppendLine(
                    $"q {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} RG {N(lineWidth)} w {N(x)} {N(pdfY)} {N(width)} {N(height)} re S Q"
                );
            }

            public void Line(
                double x1,
                double y1,
                double x2,
                double y2,
                string color,
                double lineWidth
            )
            {
                var rgb =
                    Rgb(color);

                _builder.AppendLine(
                    $"q {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} RG {N(lineWidth)} w {N(x1)} {N(PageHeight - y1)} m {N(x2)} {N(PageHeight - y2)} l S Q"
                );
            }

            public void Text(
                double x,
                double y,
                double size,
                string text,
                bool bold,
                string color,
                bool rightAlign = false
            )
            {
                var safe =
                    PdfText(text);

                var rgb =
                    Rgb(color);

                var width =
                    safe.Length *
                    size *
                    (bold
                        ? 0.56
                        : 0.51);

                var drawX =
                    rightAlign
                        ? x - width
                        : x;

                _builder.AppendLine(
                    $"BT /{(bold ? "F2" : "F1")} {N(size)} Tf {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} rg {N(drawX)} {N(PageHeight - y - size)} Td ({safe}) Tj ET"
                );
            }

            private static string PdfText(
                string value
            )
            {
                var builder =
                    new StringBuilder();

                foreach (
                    var character in value
                )
                {
                    var safe =
                        character <=
                        127
                            ? character
                            : '-';

                    if (
                        safe == '\\' ||
                        safe == '(' ||
                        safe == ')'
                    )
                    {
                        builder.Append(
                            '\\'
                        );
                    }

                    builder.Append(
                        safe
                    );
                }

                return builder.ToString();
            }

            private static (
                double R,
                double G,
                double B
            ) Rgb(
                string color
            )
            {
                var hex =
                    color
                        .Trim()
                        .TrimStart('#');

                if (
                    hex.Length !=
                    6
                )
                {
                    hex =
                        "000000";
                }

                return (
                    Convert.ToInt32(
                        hex[..2],
                        16
                    ) / 255d,
                    Convert.ToInt32(
                        hex.Substring(
                            2,
                            2
                        ),
                        16
                    ) / 255d,
                    Convert.ToInt32(
                        hex.Substring(
                            4,
                            2
                        ),
                        16
                    ) / 255d
                );
            }

            private static string N(
                double value
            )
            {
                return value.ToString(
                    "0.###",
                    CultureInfo.InvariantCulture
                );
            }
        }

        private static class PdfDocumentBuilder
        {
            public static byte[] Build(
                List<string> pageContents
            )
            {
                var objects =
                    new List<string>();

                objects.Add(
                    "<< /Type /Catalog /Pages 2 0 R >>"
                );

                var pageObjectNumbers =
                    new List<int>();

                var fontRegularObject =
                    3;

                var fontBoldObject =
                    4;

                objects.Add(
                    string.Empty
                );

                objects.Add(
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
                );

                objects.Add(
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
                );

                foreach (
                    var content in pageContents
                )
                {
                    var contentBytes =
                        Encoding.ASCII.GetBytes(
                            content
                        );

                    var contentObject =
                        objects.Count + 1;

                    objects.Add(
                        $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream"
                    );

                    var pageObject =
                        objects.Count + 1;

                    pageObjectNumbers.Add(
                        pageObject
                    );

                    objects.Add(
                        $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 {fontRegularObject} 0 R /F2 {fontBoldObject} 0 R >> >> /Contents {contentObject} 0 R >>"
                    );
                }

                objects[1] =
                    $"<< /Type /Pages /Count {pageObjectNumbers.Count} /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] >>";

                using var stream =
                    new MemoryStream();

                using var writer =
                    new StreamWriter(
                        stream,
                        Encoding.ASCII,
                        1024,
                        leaveOpen: true
                    );

                writer.NewLine =
                    "\n";

                writer.Write(
                    "%PDF-1.4\n"
                );

                writer.Flush();

                var offsets =
                    new List<long>
                    {
                        0
                    };

                for (
                    var index = 0;
                    index < objects.Count;
                    index++
                )
                {
                    offsets.Add(
                        stream.Position
                    );

                    writer.Write(
                        $"{index + 1} 0 obj\n"
                    );

                    writer.Write(
                        objects[index]
                    );

                    writer.Write(
                        "\nendobj\n"
                    );

                    writer.Flush();
                }

                var xref =
                    stream.Position;

                writer.Write(
                    $"xref\n0 {objects.Count + 1}\n"
                );

                writer.Write(
                    "0000000000 65535 f \n"
                );

                for (
                    var index = 1;
                    index < offsets.Count;
                    index++
                )
                {
                    writer.Write(
                        $"{offsets[index]:0000000000} 00000 n \n"
                    );
                }

                writer.Write(
                    $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"
                );

                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}
