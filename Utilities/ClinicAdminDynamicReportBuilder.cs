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
        private const string TealDark = "0B5F59";
        private const string TealLight = "CCFBF1";
        private const string Slate = "0F172A";
        private const string SlateSoft = "475569";
        private const string Border = "CBD5E1";
        private const string Surface = "F8FAFC";
        private const string White = "FFFFFF";
        private const string Green = "DCFCE7";
        private const string Amber = "FEF3C7";
        private const string Red = "FEE2E2";
        private const string BlueLight = "DBEAFE";

        // =====================================================
        // PUBLIC EXPORTS
        // =====================================================

        public static byte[] BuildExcel(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            ArgumentNullException.ThrowIfNull(
                report
            );

            var filters =
                BuildDashboardFilters(
                    report
                );

            var visual =
                BuildVisualConfig(
                    report
                );

            var pieData =
                BuildChartData(
                    report,
                    visual.PieCategoryKey,
                    null,
                    8
                );

            var barData =
                BuildChartData(
                    report,
                    visual.BarCategoryKey,
                    visual.BarValueKey,
                    10
                );

            var kpis =
                BuildKpis(
                    report,
                    visual,
                    pieData
                );

            using var output =
                new MemoryStream();

            using (
                var archive =
                    new ZipArchive(
                        output,
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
                    RootRelationshipsXml()
                );

                WriteEntry(
                    archive,
                    "xl/workbook.xml",
                    WorkbookXml(
                        filters
                    )
                );

                WriteEntry(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    WorkbookRelationshipsXml()
                );

                WriteEntry(
                    archive,
                    "xl/styles.xml",
                    StylesXml()
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    DashboardSheetXml(
                        report,
                        filters,
                        kpis
                    )
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/_rels/sheet1.xml.rels",
                    DashboardRelationshipsXml()
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/sheet2.xml",
                    DataSheetXml(
                        report
                    )
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/sheet3.xml",
                    CalculationSheetXml(
                        report,
                        filters,
                        visual,
                        pieData,
                        barData,
                        kpis
                    )
                );

                WriteEntry(
                    archive,
                    "xl/worksheets/sheet4.xml",
                    ListsSheetXml(
                        filters
                    )
                );

                WriteEntry(
                    archive,
                    "xl/drawings/drawing1.xml",
                    DrawingXml()
                );

                WriteEntry(
                    archive,
                    "xl/drawings/_rels/drawing1.xml.rels",
                    DrawingRelationshipsXml()
                );

                WriteEntry(
                    archive,
                    "xl/charts/chart1.xml",
                    PieChartXml(
                        visual.PieTitle,
                        pieData
                    )
                );

                WriteEntry(
                    archive,
                    "xl/charts/chart2.xml",
                    BarChartXml(
                        visual.BarTitle,
                        barData
                    )
                );
            }

            return output.ToArray();
        }

        /*
         * Existing PDF entry point remains available.
         * The Excel redesign does not affect the secure PDF flow.
         */
        public static byte[] BuildPdf(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var pages =
                BuildPdfPages(
                    report
                );

            return PdfDocumentBuilder.Build(
                pages
            );
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        private static string DashboardSheetXml(
    ClinicAdminDynamicReportPreviewDto report,
    List<DashboardFilter> filters,
    List<KpiDefinition> kpis
)
{
    var sheet =
        new WorksheetBuilder();

    sheet.SetWidths(
        18,
        20,
        4,
        18,
        20,
        4,
        18,
        20,
        4,
        18,
        20,
        18
    );

    sheet.FreezeRows =
        4;

    sheet.DrawingRelationshipId =
        "rId1";

    sheet.Merge(
        1,
        1,
        1,
        12
    );

    sheet.Cell(
        1,
        1,
        "PHILALINK DYNAMIC REPORT",
        1
    );

    sheet.Merge(
        2,
        1,
        2,
        12
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
        12
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
        12
    );

    sheet.Cell(
        4,
        1,
        $"{RangeText(report)}  |  Dashboard filters recalculate KPIs, charts and the preview.",
        4
    );

    // =================================================
    // FILTER PANEL
    // =================================================

    sheet.Merge(
        6,
        1,
        6,
        12
    );

    sheet.Cell(
        6,
        1,
        "DASHBOARD FILTERS",
        5
    );

    for (
        var index = 0;
        index < filters.Count;
        index++
    )
    {
        var filter =
            filters[index];

        var position =
            FilterPosition(
                index
            );

        sheet.Cell(
            position.Row,
            position.LabelColumn,
            filter.Label,
            6
        );

        sheet.Cell(
            position.Row,
            position.ValueColumn,
            "All",
            7
        );

        sheet.AddListValidation(
            filter.ValueCell,
            filter.ListName
        );
    }

    var dateColumn =
        GetDateColumn(
            report
        );

    if (
        dateColumn !=
        null
    )
    {
        sheet.Cell(
            11,
            1,
            $"{dateColumn.Label} from",
            6
        );

        sheet.Cell(
            11,
            2,
            report.DateFrom ??
            MinDate(
                report,
                dateColumn.Key
            ),
            21
        );

        sheet.Cell(
            11,
            4,
            $"{dateColumn.Label} to",
            6
        );

        sheet.Cell(
            11,
            5,
            report.DateTo ??
            MaxDate(
                report,
                dateColumn.Key
            ),
            21
        );

        sheet.Merge(
            11,
            7,
            11,
            12
        );

        sheet.Cell(
            11,
            7,
            "Clear either date cell to remove that date boundary.",
            19
        );
    }
    else
    {
        sheet.Merge(
            11,
            1,
            11,
            12
        );

        sheet.Cell(
            11,
            1,
            "Current-state report: category filters still recalculate all dashboard visuals.",
            19
        );
    }

    // =================================================
    // KPI CARDS
    // =================================================

    sheet.Merge(
        13,
        1,
        13,
        12
    );

    sheet.Cell(
        13,
        1,
        "LIVE KPI SUMMARY",
        5
    );

    var cardStarts =
        new[]
        {
            1,
            4,
            7,
            10
        };

    for (
        var index = 0;
        index <
        Math.Min(
            4,
            kpis.Count
        );
        index++
    )
    {
        var start =
            cardStarts[index];

        var end =
            Math.Min(
                12,
                start +
                2
            );

        sheet.Merge(
            14,
            start,
            14,
            end
        );

        sheet.Cell(
            14,
            start,
            kpis[index].Label,
            8
        );

        sheet.Merge(
            15,
            start,
            16,
            end
        );

        sheet.Formula(
            15,
            start,
            $"Calc!$K${index + 2}",
            kpis[index]
                .IsPercentage
                    ? 20
                    : 9,
            kpis[index]
                .CachedValue
        );
    }

    // =================================================
    // CHART HEADINGS
    // =================================================

    sheet.Merge(
        18,
        1,
        18,
        6
    );

    sheet.Cell(
        18,
        1,
        "DISTRIBUTION",
        5
    );

    sheet.Merge(
        18,
        7,
        18,
        12
    );

    sheet.Cell(
        18,
        7,
        "COMPARISON",
        5
    );

    // =================================================
    // FILTERED TABLE PREVIEW
    //
    // IMPORTANT:
    // Do not use FILTER() here.
    //
    // FILTER is a dynamic-array formula and requires
    // additional OOXML dynamic-array metadata.
    //
    // The previous implementation wrote FILTER as an
    // ordinary formula, which caused Microsoft Excel
    // to repair sheet1.xml and remove the formula.
    //
    // A dynamic spill would also collide with the
    // dashboard footer for larger reports.
    //
    // These scalar INDEX + AGGREGATE formulas give us
    // a live filtered preview without dynamic arrays.
    // =================================================

    const int previewTitleRow =
        37;

    const int previewHeaderRow =
        38;

    const int previewFirstDataRow =
        39;

    const int previewMaximumRows =
        20;

    const int previewNoteRow =
        60;

    const int previewFooterRow =
        61;

    var previewColumns =
        report.Columns
            .Take(
                Math.Min(
                    8,
                    report.Columns.Count
                )
            )
            .ToList();

    sheet.Merge(
        previewTitleRow,
        1,
        previewTitleRow,
        12
    );

    sheet.Cell(
        previewTitleRow,
        1,
        "FILTERED TABLE PREVIEW",
        5
    );

    for (
        var index = 0;
        index < previewColumns.Count;
        index++
    )
    {
        sheet.Cell(
            previewHeaderRow,
            index +
            1,
            previewColumns[index]
                .Label,
            10
        );
    }

    if (
        report.Rows.Count >
        0 &&
        previewColumns.Count >
        0
    )
    {
        var lastDataRow =
            4 +
            report.Rows.Count;

        var lastCalcRow =
            1 +
            report.Rows.Count;

        var previewRows =
            Math.Min(
                previewMaximumRows,
                report.Rows.Count
            );

        for (
            var previewIndex = 0;
            previewIndex < previewRows;
            previewIndex++
        )
        {
            var dashboardRow =
                previewFirstDataRow +
                previewIndex;

            var matchNumber =
                previewIndex +
                1;

            for (
                var columnIndex = 0;
                columnIndex < previewColumns.Count;
                columnIndex++
            )
            {
                var dataColumn =
                    ColumnName(
                        columnIndex +
                        1
                    );

                var formula =
                    $"IFERROR(" +
                    $"INDEX(" +
                    $"Data!${dataColumn}$5:${dataColumn}${lastDataRow}," +
                    $"AGGREGATE(" +
                    $"15," +
                    $"6," +
                    $"(" +
                    $"ROW(Data!$A$5:$A${lastDataRow})-" +
                    $"ROW(Data!$A$5)+1" +
                    $")/" +
                    $"(" +
                    $"Calc!$B$2:$B${lastCalcRow}=1" +
                    $")," +
                    $"{matchNumber}" +
                    $")" +
                    $")," +
                    $"\"\"" +
                    $")";

                var column =
                    previewColumns[
                        columnIndex
                    ];

                var style =
                    column.DataType.Equals(
                        "date",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? 13
                        : column.DataType.Equals(
                            "datetime",
                            StringComparison.OrdinalIgnoreCase
                        )
                            ? 14
                            : column.DataType.Equals(
                                "number",
                                StringComparison.OrdinalIgnoreCase
                            )
                                ? 15
                                : previewIndex %
                                  2 ==
                                  0
                                    ? 11
                                    : 12;

                sheet.Formula(
                    dashboardRow,
                    columnIndex +
                    1,
                    formula,
                    style,
                    null
                );
            }
        }
    }
    else
    {
        sheet.Merge(
            previewFirstDataRow,
            1,
            previewFirstDataRow,
            12
        );

        sheet.Cell(
            previewFirstDataRow,
            1,
            "No rows matched the exported report.",
            19
        );
    }

    sheet.Merge(
        previewNoteRow,
        1,
        previewNoteRow,
        12
    );

    sheet.Cell(
        previewNoteRow,
        1,
        "Preview shows up to 20 matching rows. Use the Data sheet for the full export.",
        19
    );

    sheet.Merge(
        previewFooterRow,
        1,
        previewFooterRow,
        12
    );

    sheet.Cell(
        previewFooterRow,
        1,
        "The Data worksheet contains the full exported dataset and standard Excel header filters. Dashboard filters drive the live KPIs, charts and preview.",
        19
    );

    return sheet.Build();
}
        // =====================================================
        // DATA SHEET
        // =====================================================

        private static string DataSheetXml(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var sheet =
                new WorksheetBuilder();

            var columnCount =
                Math.Max(
                    1,
                    report.Columns.Count
                );

            sheet.SetWidths(
                report.Columns.Count >
                0
                    ? report.Columns
                        .Select(
                            column =>
                                ColumnWidth(
                                    column.Label
                                )
                        )
                        .ToArray()
                    : new[]
                    {
                        18d
                    }
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
                report.Title,
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
                $"{report.ClinicName} | {RangeText(report)} | Requested by {report.RequestedBy}",
                3
            );

            const int headerRow =
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
                    report.Columns[index]
                        .Label,
                    10
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
                        report.Columns[
                            columnIndex
                        ];

                    TryGetRowValue(
                        row,
                        column.Key,
                        out var value
                    );

                    sheet.Cell(
                        headerRow +
                        1 +
                        rowIndex,
                        columnIndex +
                        1,
                        NormalizeExcelValue(
                            value,
                            column.DataType
                        ),
                        ExcelDataStyle(
                            column.DataType,
                            value,
                            rowIndex
                        )
                    );
                }
            }

            if (
                report.Columns.Count >
                0
            )
            {
                var lastRow =
                    Math.Max(
                        headerRow,
                        headerRow +
                        report.Rows.Count
                    );

                sheet.AutoFilterRef =
                    $"A{headerRow}:{ColumnName(report.Columns.Count)}{lastRow}";
            }

            sheet.FreezeRows =
                headerRow;

            return sheet.Build();
        }

        // =====================================================
        // CALC SHEET
        // =====================================================

        private static string CalculationSheetXml(
            ClinicAdminDynamicReportPreviewDto report,
            List<DashboardFilter> filters,
            VisualConfig visual,
            List<ChartPoint> pieData,
            List<ChartPoint> barData,
            List<KpiDefinition> kpis
        )
        {
            var sheet =
                new WorksheetBuilder();

            sheet.SetWidths(
                9,
                10,
                4,
                28,
                14,
                4,
                28,
                14,
                4,
                30,
                14
            );

            sheet.Cell(
                1,
                1,
                "Row",
                10
            );

            sheet.Cell(
                1,
                2,
                "Visible",
                10
            );

            const int firstDataRow =
                5;

            for (
                var rowIndex = 0;
                rowIndex < report.Rows.Count;
                rowIndex++
            )
            {
                var calcRow =
                    rowIndex +
                    2;

                var dataRow =
                    firstDataRow +
                    rowIndex;

                sheet.Cell(
                    calcRow,
                    1,
                    rowIndex +
                    1,
                    15
                );

                sheet.Formula(
                    calcRow,
                    2,
                    VisibilityFormula(
                        report,
                        filters,
                        dataRow
                    ),
                    15,
                    1
                );
            }

            // Pie chart helper.
            sheet.Cell(
                1,
                4,
                visual.PieTitle,
                10
            );

            sheet.Cell(
                1,
                5,
                "Value",
                10
            );

            for (
                var index = 0;
                index < pieData.Count;
                index++
            )
            {
                var row =
                    index +
                    2;

                sheet.Cell(
                    row,
                    4,
                    pieData[index]
                        .Label,
                    11
                );

                sheet.Formula(
                    row,
                    5,
                    CountByCategoryFormula(
                        report,
                        visual.PieCategoryKey,
                        $"D{row}"
                    ),
                    15,
                    pieData[index]
                        .Value
                );
            }

            // Bar graph helper.
            sheet.Cell(
                1,
                7,
                visual.BarTitle,
                10
            );

            sheet.Cell(
                1,
                8,
                visual.BarValueKey ==
                null
                    ? "Records"
                    : ColumnLabel(
                        report,
                        visual.BarValueKey
                    ),
                10
            );

            for (
                var index = 0;
                index < barData.Count;
                index++
            )
            {
                var row =
                    index +
                    2;

                sheet.Cell(
                    row,
                    7,
                    barData[index]
                        .Label,
                    11
                );

                sheet.Formula(
                    row,
                    8,
                    BarValueFormula(
                        report,
                        visual.BarCategoryKey,
                        visual.BarValueKey,
                        $"G{row}"
                    ),
                    15,
                    barData[index]
                        .Value
                );
            }

            // KPI helper.
            sheet.Cell(
                1,
                10,
                "KPI",
                10
            );

            sheet.Cell(
                1,
                11,
                "Value",
                10
            );

            for (
                var index = 0;
                index < kpis.Count;
                index++
            )
            {
                var row =
                    index +
                    2;

                sheet.Cell(
                    row,
                    10,
                    kpis[index]
                        .Label,
                    11
                );

                sheet.Formula(
                    row,
                    11,
                    KpiFormula(
                        report,
                        kpis[index]
                    ),
                    kpis[index]
                        .IsPercentage
                            ? 20
                            : 15,
                    kpis[index]
                        .CachedValue
                );
            }

            return sheet.Build();
        }

        // =====================================================
        // LISTS SHEET
        // =====================================================

        private static string ListsSheetXml(
            List<DashboardFilter> filters
        )
        {
            var sheet =
                new WorksheetBuilder();

            sheet.SetWidths(
                Enumerable
                    .Range(
                        0,
                        Math.Max(
                            1,
                            filters.Count
                        )
                    )
                    .Select(
                        _ =>
                            28d
                    )
                    .ToArray()
            );

            if (
                filters.Count ==
                0
            )
            {
                sheet.Cell(
                    1,
                    1,
                    "No dashboard filters",
                    11
                );

                return sheet.Build();
            }

            for (
                var index = 0;
                index < filters.Count;
                index++
            )
            {
                var filter =
                    filters[index];

                sheet.Cell(
                    1,
                    index + 1,
                    filter.Label,
                    10
                );

                for (
                    var optionIndex = 0;
                    optionIndex < filter.Options.Count;
                    optionIndex++
                )
                {
                    sheet.Cell(
                        optionIndex +
                        2,
                        index +
                        1,
                        filter.Options[
                            optionIndex
                        ],
                        11
                    );
                }
            }

            return sheet.Build();
        }

        // =====================================================
        // FILTER CONFIG
        // =====================================================

        private static List<DashboardFilter>
            BuildDashboardFilters(
                ClinicAdminDynamicReportPreviewDto report
            )
        {
            var preferred =
                report.ReportType
                    .Trim()
                    .ToLowerInvariant()
                    switch
                    {
                        "inventory" =>
                            new[]
                            {
                                "clinic",
                                "medication",
                                "strength",
                                "form",
                                "status",
                                "unit"
                            },

                        "appointments" =>
                            new[]
                            {
                                "clinic",
                                "status",
                                "type",
                                "provider",
                                "mode",
                                "patient"
                            },

                        "collections" =>
                            new[]
                            {
                                "clinic",
                                "status",
                                "medication",
                                "collector",
                                "processedBy",
                                "patient"
                            },

                        "medication adherence" =>
                            new[]
                            {
                                "clinic",
                                "result",
                                "medication",
                                "form",
                                "dosage",
                                "patient"
                            },

                        "patients" =>
                            new[]
                            {
                                "clinic",
                                "status",
                                "gender",
                                "city",
                                "province",
                                "patient"
                            },

                        "staff" =>
                            new[]
                            {
                                "clinic",
                                "role",
                                "status"
                            },

                        "clinics" =>
                            new[]
                            {
                                "status",
                                "type",
                                "province",
                                "city",
                                "clinic"
                            },

                        "audit activity" =>
                            new[]
                            {
                                "clinic",
                                "action",
                                "role",
                                "actor"
                            },

                        _ =>
                            new[]
                            {
                                "clinic",
                                "status",
                                "result",
                                "role",
                                "type",
                                "medication"
                            }
                    };

            var result =
                new List<DashboardFilter>();

            foreach (
                var key in preferred
            )
            {
                if (
                    result.Count >=
                    6
                )
                {
                    break;
                }

                var column =
                    FindColumn(
                        report,
                        key
                    );

                if (
                    column ==
                    null
                )
                {
                    continue;
                }

                var options =
                    FilterOptions(
                        report,
                        key,
                        column.Key
                    );

                if (
                    options.Count <=
                    1
                )
                {
                    continue;
                }

                AddFilter(
                    result,
                    column,
                    options
                );
            }

            /*
             * Fill remaining slots from other categorical columns.
             * This lets SuperAdmin-specific columns participate even
             * when a report has extra system-level fields.
             */
            foreach (
                var column in report.Columns
            )
            {
                if (
                    result.Count >=
                    6
                )
                {
                    break;
                }

                if (
                    result.Any(
                        item =>
                            item.ColumnKey
                                .Equals(
                                    column.Key,
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                )
                {
                    continue;
                }

                if (
                    IsNumericOrDate(
                        column.DataType
                    )
                )
                {
                    continue;
                }

                var options =
                    UniqueValues(
                        report,
                        column.Key
                    );

                if (
                    options.Count <=
                    1 ||
                    options.Count >
                    50
                )
                {
                    continue;
                }

                AddFilter(
                    result,
                    column,
                    options
                );
            }

            return result;
        }

        private static void AddFilter(
            List<DashboardFilter> filters,
            ClinicAdminDynamicReportColumnDto column,
            List<string> options
        )
        {
            var index =
                filters.Count;

            var position =
                FilterPosition(
                    index
                );

            filters.Add(
                new DashboardFilter(
                    column.Key,
                    column.Label,
                    $"{ColumnName(position.ValueColumn)}{position.Row}",
                    $"FilterList{index + 1}",
                    index +
                    1,
                    options
                )
            );
        }

        private static FilterPositionDefinition
            FilterPosition(
                int index
            )
        {
            return index switch
            {
                0 =>
                    new(
                        7,
                        1,
                        2
                    ),

                1 =>
                    new(
                        7,
                        4,
                        5
                    ),

                2 =>
                    new(
                        7,
                        7,
                        8
                    ),

                3 =>
                    new(
                        9,
                        1,
                        2
                    ),

                4 =>
                    new(
                        9,
                        4,
                        5
                    ),

                _ =>
                    new(
                        9,
                        7,
                        8
                    )
            };
        }

        private static List<string> FilterOptions(
            ClinicAdminDynamicReportPreviewDto report,
            string preferredKey,
            string columnKey
        )
        {
            var queryKey =
                preferredKey
                    .ToLowerInvariant()
                    switch
                    {
                        "result" =>
                            "status",

                        "type" =>
                            "appointmentType",

                        _ =>
                            preferredKey
                    };

            if (
                report.FilterOptions
                    .TryGetValue(
                        queryKey,
                        out var supplied
                    )
            )
            {
                return new[]
                    {
                        "All"
                    }
                    .Concat(
                        supplied
                            .Where(
                                value =>
                                    !string.IsNullOrWhiteSpace(
                                        value
                                    ) &&
                                    !value.Equals(
                                        "All",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                            )
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToList();
            }

            return UniqueValues(
                report,
                columnKey
            );
        }

        private static List<string> UniqueValues(
            ClinicAdminDynamicReportPreviewDto report,
            string key
        )
        {
            var values =
                report.Rows
                    .Select(
                        row =>
                            RowText(
                                row,
                                key
                            )
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            ) &&
                            value !=
                            "—"
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            values.Insert(
                0,
                "All"
            );

            return values;
        }

        // =====================================================
        // VISUAL CONFIG
        // =====================================================

        private static VisualConfig BuildVisualConfig(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var type =
                report.ReportType
                    .Trim()
                    .ToLowerInvariant();

            return type switch
            {
                "inventory" =>
                    new(
                        ExistingKey(
                            report,
                            "status"
                        ),
                        "Stock status mix",
                        ExistingKey(
                            report,
                            "medication",
                            "form"
                        ),
                        ExistingKey(
                            report,
                            "quantity"
                        ),
                        "Units on hand by medication"
                    ),

                "appointments" =>
                    new(
                        ExistingKey(
                            report,
                            "status"
                        ),
                        "Appointment status",
                        ExistingKey(
                            report,
                            "type",
                            "provider",
                            "clinic"
                        ),
                        null,
                        "Appointments by type"
                    ),

                "collections" =>
                    new(
                        ExistingKey(
                            report,
                            "status"
                        ),
                        "Collection status",
                        ExistingKey(
                            report,
                            "medication",
                            "collector",
                            "clinic"
                        ),
                        null,
                        "Collections by medication"
                    ),

                "medication adherence" =>
                    new(
                        ExistingKey(
                            report,
                            "result",
                            "status"
                        ),
                        "Taken vs missed",
                        ExistingKey(
                            report,
                            "medication",
                            "clinic"
                        ),
                        null,
                        "Dose records by medication"
                    ),

                "patients" =>
                    new(
                        ExistingKey(
                            report,
                            "status",
                            "gender"
                        ),
                        "Patient distribution",
                        ExistingKey(
                            report,
                            "clinic",
                            "gender"
                        ),
                        null,
                        HasColumn(
                            report,
                            "clinic"
                        )
                            ? "Patients by clinic"
                            : "Patients by gender"
                    ),

                "staff" =>
                    new(
                        ExistingKey(
                            report,
                            "role",
                            "status"
                        ),
                        "Role distribution",
                        ExistingKey(
                            report,
                            "clinic",
                            "role"
                        ),
                        null,
                        HasColumn(
                            report,
                            "clinic"
                        )
                            ? "Staff by clinic"
                            : "Staff by role"
                    ),

                "clinics" =>
                    new(
                        ExistingKey(
                            report,
                            "status",
                            "type"
                        ),
                        "Clinic status",
                        ExistingKey(
                            report,
                            "type",
                            "province",
                            "city"
                        ),
                        null,
                        "Clinics by category"
                    ),

                "audit activity" =>
                    new(
                        ExistingKey(
                            report,
                            "action",
                            "role"
                        ),
                        "Audit action mix",
                        ExistingKey(
                            report,
                            "clinic",
                            "actor",
                            "role",
                            "action"
                        ),
                        null,
                        "Audit activity distribution"
                    ),

                _ =>
                    new(
                        ExistingKey(
                            report,
                            "status",
                            "result",
                            "role",
                            "type"
                        ),
                        "Distribution",
                        ExistingKey(
                            report,
                            "clinic",
                            "medication",
                            "type",
                            "role"
                        ),
                        null,
                        "Records by category"
                    )
            };
        }

        private static string ExistingKey(
            ClinicAdminDynamicReportPreviewDto report,
            params string[] candidates
        )
        {
            foreach (
                var candidate in candidates
            )
            {
                if (
                    HasColumn(
                        report,
                        candidate
                    )
                )
                {
                    return candidate;
                }
            }

            return report.Columns
                .FirstOrDefault(
                    column =>
                        !IsNumericOrDate(
                            column.DataType
                        )
                )?
                .Key ??
                report.Columns
                    .FirstOrDefault()?
                    .Key ??
                string.Empty;
        }

        private static bool HasColumn(
            ClinicAdminDynamicReportPreviewDto report,
            string key
        )
        {
            return report.Columns
                .Any(
                    column =>
                        column.Key.Equals(
                            key,
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        }

        private static bool IsNumericOrDate(
            string dataType
        )
        {
            return dataType.Equals(
                       "number",
                       StringComparison.OrdinalIgnoreCase
                   ) ||
                   dataType.Equals(
                       "date",
                       StringComparison.OrdinalIgnoreCase
                   ) ||
                   dataType.Equals(
                       "datetime",
                       StringComparison.OrdinalIgnoreCase
                   );
        }

        private static List<ChartPoint> BuildChartData(
            ClinicAdminDynamicReportPreviewDto report,
            string categoryKey,
            string? valueKey,
            int maxCategories
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    categoryKey
                ) ||
                report.Rows.Count ==
                0
            )
            {
                return new()
                {
                    new(
                        "No data",
                        0
                    )
                };
            }

            var values =
                report.Rows
                    .GroupBy(
                        row =>
                        {
                            var text =
                                RowText(
                                    row,
                                    categoryKey
                                );

                            return string.IsNullOrWhiteSpace(
                                text
                            )
                                ? "Unspecified"
                                : text;
                        },
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Select(
                        group =>
                            new ChartPoint(
                                group.Key,
                                valueKey ==
                                null
                                    ? group.Count()
                                    : group.Sum(
                                        row =>
                                            RowNumber(
                                                row,
                                                valueKey
                                            )
                                    )
                            )
                    )
                    .OrderByDescending(
                        point =>
                            point.Value
                    )
                    .ThenBy(
                        point =>
                            point.Label
                    )
                    .Take(
                        maxCategories
                    )
                    .ToList();

            return values.Count >
                0
                    ? values
                    : new()
                    {
                        new(
                            "No data",
                            0
                        )
                    };
        }

        // =====================================================
        // KPI CONFIG
        // =====================================================

        private static List<KpiDefinition> BuildKpis(
            ClinicAdminDynamicReportPreviewDto report,
            VisualConfig visual,
            List<ChartPoint> pieData
        )
        {
            var type =
                report.ReportType
                    .Trim()
                    .ToLowerInvariant();

            List<KpiDefinition> result =
                type switch
                {
                    "inventory" =>
                        new()
                        {
                            CountVisible(
                                "Items shown"
                            ),

                            SumVisible(
                                "Units on hand",
                                "quantity"
                            ),

                            CountEquals(
                                "Low stock",
                                "status",
                                "Low stock"
                            ),

                            CountEquals(
                                "Inactive",
                                "status",
                                "Inactive"
                            )
                        },

                    "collections" =>
                        new()
                        {
                            CountVisible(
                                "Collections shown"
                            ),

                            CountEquals(
                                "Collected",
                                "status",
                                "Collected"
                            ),

                            CountEquals(
                                "Missed",
                                "status",
                                "Missed"
                            ),

                            CountEquals(
                                "Scheduled",
                                "status",
                                "Scheduled"
                            )
                        },

                    "appointments" =>
                        new()
                        {
                            CountVisible(
                                "Appointments shown"
                            ),

                            CountEquals(
                                "Completed",
                                "status",
                                "Completed"
                            ),

                            CountEquals(
                                "Scheduled / pending",
                                "status",
                                "Scheduled",
                                "Pending",
                                "Confirmed"
                            ),

                            CountEquals(
                                "Cancelled",
                                "status",
                                "Cancelled",
                                "Canceled"
                            )
                        },

                    "medication adherence" =>
                        new()
                        {
                            CountVisible(
                                "Logs shown"
                            ),

                            CountEquals(
                                "Taken",
                                ExistingKey(
                                    report,
                                    "result",
                                    "status"
                                ),
                                "Taken"
                            ),

                            CountEquals(
                                "Missed",
                                ExistingKey(
                                    report,
                                    "result",
                                    "status"
                                ),
                                "Missed"
                            ),

                            RatioEquals(
                                "Adherence",
                                ExistingKey(
                                    report,
                                    "result",
                                    "status"
                                ),
                                "Taken"
                            )
                        },

                    "patients" =>
                        new()
                        {
                            CountVisible(
                                "Patients shown"
                            ),

                            CountEquals(
                                "Active",
                                "status",
                                "Active"
                            ),

                            CountEquals(
                                "Inactive",
                                "status",
                                "Inactive"
                            )
                        },

                    "staff" =>
                        new()
                        {
                            CountVisible(
                                "Staff shown"
                            ),

                            CountEquals(
                                "Nurses",
                                "role",
                                "Nurse"
                            ),

                            CountEquals(
                                "Proxies",
                                "role",
                                "Proxy"
                            ),

                            CountEquals(
                                "Active",
                                "status",
                                "Active"
                            )
                        },

                    "clinics" =>
                        new()
                        {
                            CountVisible(
                                "Clinics shown"
                            ),

                            CountEquals(
                                "Active",
                                "status",
                                "Active"
                            ),

                            CountEquals(
                                "Inactive",
                                "status",
                                "Inactive"
                            )
                        },

                    _ =>
                        new()
                        {
                            CountVisible(
                                "Records shown"
                            )
                        }
                };

            foreach (
                var point in pieData
            )
            {
                if (
                    result.Count >=
                    4
                )
                {
                    break;
                }

                if (
                    point.Label.Equals(
                        "No data",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                if (
                    result.Any(
                        existing =>
                            existing.Label.Equals(
                                point.Label,
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                )
                {
                    continue;
                }

                result.Add(
                    CountEquals(
                        point.Label,
                        visual.PieCategoryKey,
                        point.Label
                    )
                );
            }

            return result
                .Take(
                    4
                )
                .Select(
                    definition =>
                        definition with
                        {
                            CachedValue =
                                CachedKpiValue(
                                    report,
                                    definition
                                )
                        }
                )
                .ToList();
        }

        private static KpiDefinition CountVisible(
            string label
        )
        {
            return new(
                label,
                KpiMode.CountVisible,
                null,
                null,
                Array.Empty<string>(),
                false,
                0
            );
        }

        private static KpiDefinition SumVisible(
            string label,
            string valueKey
        )
        {
            return new(
                label,
                KpiMode.SumVisible,
                null,
                valueKey,
                Array.Empty<string>(),
                false,
                0
            );
        }

        private static KpiDefinition CountEquals(
            string label,
            string columnKey,
            params string[] values
        )
        {
            return new(
                label,
                KpiMode.CountEquals,
                columnKey,
                null,
                values,
                false,
                0
            );
        }

        private static KpiDefinition RatioEquals(
            string label,
            string columnKey,
            params string[] values
        )
        {
            return new(
                label,
                KpiMode.RatioEquals,
                columnKey,
                null,
                values,
                true,
                0
            );
        }

        private static double CachedKpiValue(
            ClinicAdminDynamicReportPreviewDto report,
            KpiDefinition definition
        )
        {
            return definition.Mode switch
            {
                KpiMode.CountVisible =>
                    report.Rows.Count,

                KpiMode.SumVisible =>
                    report.Rows.Sum(
                        row =>
                            RowNumber(
                                row,
                                definition.ValueKey ??
                                string.Empty
                            )
                    ),

                KpiMode.CountEquals =>
                    report.Rows.Count(
                        row =>
                            definition.Values
                                .Any(
                                    value =>
                                        RowText(
                                            row,
                                            definition.ColumnKey ??
                                            string.Empty
                                        )
                                        .Equals(
                                            value,
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                )
                    ),

                KpiMode.RatioEquals =>
                    report.Rows.Count ==
                    0
                        ? 0
                        : report.Rows.Count(
                            row =>
                                definition.Values
                                    .Any(
                                        value =>
                                            RowText(
                                                row,
                                                definition.ColumnKey ??
                                                string.Empty
                                            )
                                            .Equals(
                                                value,
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                    )
                        ) /
                        (double)
                        report.Rows.Count,

                _ =>
                    0
            };
        }

        // =====================================================
        // LIVE FORMULAS
        // =====================================================

        private static string VisibilityFormula(
            ClinicAdminDynamicReportPreviewDto report,
            List<DashboardFilter> filters,
            int dataRow
        )
        {
            var conditions =
                new List<string>();

            foreach (
                var filter in filters
            )
            {
                var columnIndex =
                    ColumnIndex(
                        report,
                        filter.ColumnKey
                    );

                if (
                    columnIndex <=
                    0
                )
                {
                    continue;
                }

                var dataCell =
                    $"Data!${ColumnName(columnIndex)}{dataRow}";

                var dashboardCell =
                    AbsoluteReference(
                        filter.ValueCell
                    );

                conditions.Add(
                    $"OR(Dashboard!{dashboardCell}=\"All\",{dataCell}=Dashboard!{dashboardCell})"
                );
            }

            var dateColumn =
                GetDateColumn(
                    report
                );

            if (
                dateColumn !=
                null
            )
            {
                var columnIndex =
                    ColumnIndex(
                        report,
                        dateColumn.Key
                    );

                if (
                    columnIndex >
                    0
                )
                {
                    var dataCell =
                        $"Data!${ColumnName(columnIndex)}{dataRow}";

                    conditions.Add(
                        $"OR(Dashboard!$B$11=\"\",INT({dataCell})>=Dashboard!$B$11)"
                    );

                    conditions.Add(
                        $"OR(Dashboard!$E$11=\"\",INT({dataCell})<=Dashboard!$E$11)"
                    );
                }
            }

            return conditions.Count ==
                0
                    ? "1"
                    : $"--AND({string.Join(",", conditions)})";
        }

        private static string CountByCategoryFormula(
            ClinicAdminDynamicReportPreviewDto report,
            string categoryKey,
            string criteriaCell
        )
        {
            if (
                report.Rows.Count ==
                0
            )
            {
                return "0";
            }

            var categoryColumn =
                ColumnIndex(
                    report,
                    categoryKey
                );

            if (
                categoryColumn <=
                0
            )
            {
                return "0";
            }

            var lastDataRow =
                4 +
                report.Rows.Count;

            var lastCalcRow =
                1 +
                report.Rows.Count;

            var letter =
                ColumnName(
                    categoryColumn
                );

            return $"SUMIFS($B$2:$B${lastCalcRow},Data!${letter}$5:${letter}${lastDataRow},{criteriaCell})";
        }

        private static string BarValueFormula(
            ClinicAdminDynamicReportPreviewDto report,
            string categoryKey,
            string? valueKey,
            string criteriaCell
        )
        {
            if (
                report.Rows.Count ==
                0
            )
            {
                return "0";
            }

            var categoryColumn =
                ColumnIndex(
                    report,
                    categoryKey
                );

            if (
                categoryColumn <=
                0
            )
            {
                return "0";
            }

            var lastDataRow =
                4 +
                report.Rows.Count;

            var lastCalcRow =
                1 +
                report.Rows.Count;

            var categoryLetter =
                ColumnName(
                    categoryColumn
                );

            if (
                string.IsNullOrWhiteSpace(
                    valueKey
                )
            )
            {
                return $"SUMIFS($B$2:$B${lastCalcRow},Data!${categoryLetter}$5:${categoryLetter}${lastDataRow},{criteriaCell})";
            }

            var valueColumn =
                ColumnIndex(
                    report,
                    valueKey
                );

            if (
                valueColumn <=
                0
            )
            {
                return "0";
            }

            var valueLetter =
                ColumnName(
                    valueColumn
                );

            return $"SUMPRODUCT($B$2:$B${lastCalcRow},--(Data!${categoryLetter}$5:${categoryLetter}${lastDataRow}={criteriaCell}),Data!${valueLetter}$5:${valueLetter}${lastDataRow})";
        }

        private static string KpiFormula(
            ClinicAdminDynamicReportPreviewDto report,
            KpiDefinition definition
        )
        {
            if (
                report.Rows.Count ==
                0
            )
            {
                return "0";
            }

            var lastCalcRow =
                1 +
                report.Rows.Count;

            var lastDataRow =
                4 +
                report.Rows.Count;

            switch (
                definition.Mode
            )
            {
                case KpiMode.CountVisible:
                    return $"SUM($B$2:$B${lastCalcRow})";

                case KpiMode.SumVisible:
                {
                    var column =
                        ColumnIndex(
                            report,
                            definition.ValueKey ??
                            string.Empty
                        );

                    if (
                        column <=
                        0
                    )
                    {
                        return "0";
                    }

                    var letter =
                        ColumnName(
                            column
                        );

                    return $"SUMPRODUCT($B$2:$B${lastCalcRow},Data!${letter}$5:${letter}${lastDataRow})";
                }

                case KpiMode.CountEquals:
                    return CountCriteriaFormula(
                        report,
                        definition.ColumnKey,
                        definition.Values
                    );

                case KpiMode.RatioEquals:
                {
                    var numerator =
                        CountCriteriaFormula(
                            report,
                            definition.ColumnKey,
                            definition.Values
                        );

                    return $"IFERROR(({numerator})/SUM($B$2:$B${lastCalcRow}),0)";
                }

                default:
                    return "0";
            }
        }

        private static string CountCriteriaFormula(
            ClinicAdminDynamicReportPreviewDto report,
            string? key,
            IReadOnlyCollection<string> values
        )
        {
            if (
                report.Rows.Count ==
                0 ||
                string.IsNullOrWhiteSpace(
                    key
                ) ||
                values.Count ==
                0
            )
            {
                return "0";
            }

            var column =
                ColumnIndex(
                    report,
                    key
                );

            if (
                column <=
                0
            )
            {
                return "0";
            }

            var lastDataRow =
                4 +
                report.Rows.Count;

            var lastCalcRow =
                1 +
                report.Rows.Count;

            var letter =
                ColumnName(
                    column
                );

            return string.Join(
                "+",
                values.Select(
                    value =>
                        $"SUMIFS($B$2:$B${lastCalcRow},Data!${letter}$5:${letter}${lastDataRow},\"{FormulaText(value)}\")"
                )
            );
        }

        // =====================================================
        // PACKAGE
        // =====================================================

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
                  <Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/worksheets/sheet4.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/drawings/drawing1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawing+xml"/>
                  <Override PartName="/xl/charts/chart1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.chart+xml"/>
                  <Override PartName="/xl/charts/chart2.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.chart+xml"/>
                </Types>
                """;
        }

        private static string RootRelationshipsXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1"
                                Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"
                                Target="xl/workbook.xml"/>
                </Relationships>
                """;
        }

        private static string WorkbookXml(
            List<DashboardFilter> filters
        )
        {
            var builder =
                new StringBuilder();

            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <workbookPr/>
                  <bookViews>
                    <workbookView activeTab="0"/>
                  </bookViews>
                  <sheets>
                    <sheet name="Dashboard" sheetId="1" r:id="rId1"/>
                    <sheet name="Data" sheetId="2" r:id="rId2"/>
                    <sheet name="Calc" sheetId="3" state="veryHidden" r:id="rId3"/>
                    <sheet name="Lists" sheetId="4" state="veryHidden" r:id="rId4"/>
                  </sheets>
                """
            );

            if (
                filters.Count >
                0
            )
            {
                builder.Append(
                    "<definedNames>"
                );

                foreach (
                    var filter in filters
                )
                {
                    var lastRow =
                        filter.Options.Count +
                        1;

                    builder.Append(
                        $"<definedName name=\"{filter.ListName}\">'Lists'!${ColumnName(filter.ListColumn)}$2:${ColumnName(filter.ListColumn)}${lastRow}</definedName>"
                    );
                }

                builder.Append(
                    "</definedNames>"
                );
            }

            builder.Append(
                """
                  <calcPr calcId="191029"
                          calcMode="auto"
                          fullCalcOnLoad="1"
                          forceFullCalc="1"/>
                </workbook>
                """
            );

            return builder.ToString();
        }

        private static string WorkbookRelationshipsXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
                  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>
                  <Relationship Id="rId4" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet4.xml"/>
                  <Relationship Id="rId5" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """;
        }

        private static string DashboardRelationshipsXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1"
                                Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing"
                                Target="../drawings/drawing1.xml"/>
                </Relationships>
                """;
        }

        private static string DrawingRelationshipsXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1"
                                Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart"
                                Target="../charts/chart1.xml"/>
                  <Relationship Id="rId2"
                                Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart"
                                Target="../charts/chart2.xml"/>
                </Relationships>
                """;
        }

        // =====================================================
        // DRAWING
        // =====================================================

        private static string DrawingXml()
        {
            return """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <xdr:wsDr
                    xmlns:xdr="http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing"
                    xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
                    xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart"
                    xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">

                  <xdr:twoCellAnchor>
                    <xdr:from>
                      <xdr:col>0</xdr:col>
                      <xdr:colOff>0</xdr:colOff>
                      <xdr:row>18</xdr:row>
                      <xdr:rowOff>0</xdr:rowOff>
                    </xdr:from>

                    <xdr:to>
                      <xdr:col>6</xdr:col>
                      <xdr:colOff>0</xdr:colOff>
                      <xdr:row>34</xdr:row>
                      <xdr:rowOff>0</xdr:rowOff>
                    </xdr:to>

                    <xdr:graphicFrame macro="">
                      <xdr:nvGraphicFramePr>
                        <xdr:cNvPr id="2" name="PhilaLink Pie Chart"/>
                        <xdr:cNvGraphicFramePr/>
                      </xdr:nvGraphicFramePr>

                      <xdr:xfrm/>

                      <a:graphic>
                        <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/chart">
                          <c:chart r:id="rId1"/>
                        </a:graphicData>
                      </a:graphic>
                    </xdr:graphicFrame>

                    <xdr:clientData/>
                  </xdr:twoCellAnchor>

                  <xdr:twoCellAnchor>
                    <xdr:from>
                      <xdr:col>6</xdr:col>
                      <xdr:colOff>0</xdr:colOff>
                      <xdr:row>18</xdr:row>
                      <xdr:rowOff>0</xdr:rowOff>
                    </xdr:from>

                    <xdr:to>
                      <xdr:col>12</xdr:col>
                      <xdr:colOff>0</xdr:colOff>
                      <xdr:row>34</xdr:row>
                      <xdr:rowOff>0</xdr:rowOff>
                    </xdr:to>

                    <xdr:graphicFrame macro="">
                      <xdr:nvGraphicFramePr>
                        <xdr:cNvPr id="3" name="PhilaLink Bar Chart"/>
                        <xdr:cNvGraphicFramePr/>
                      </xdr:nvGraphicFramePr>

                      <xdr:xfrm/>

                      <a:graphic>
                        <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/chart">
                          <c:chart r:id="rId2"/>
                        </a:graphicData>
                      </a:graphic>
                    </xdr:graphicFrame>

                    <xdr:clientData/>
                  </xdr:twoCellAnchor>
                </xdr:wsDr>
                """;
        }

        // =====================================================
        // CHARTS
        // =====================================================

        private static string PieChartXml(
            string title,
            List<ChartPoint> points
        )
        {
            var lastRow =
                points.Count +
                1;

            return $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <c:chartSpace
                    xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart"
                    xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">

                  <c:chart>
                    {ChartTitleXml(title)}

                    <c:plotArea>
                      <c:layout/>

                      <c:pieChart>
                        <c:varyColors val="1"/>

                        <c:ser>
                          <c:idx val="0"/>
                          <c:order val="0"/>

                          <c:cat>
                            <c:strRef>
                              <c:f>Calc!$D$2:$D${lastRow}</c:f>
                              {StringCacheXml(points)}
                            </c:strRef>
                          </c:cat>

                          <c:val>
                            <c:numRef>
                              <c:f>Calc!$E$2:$E${lastRow}</c:f>
                              {NumberCacheXml(points)}
                            </c:numRef>
                          </c:val>
                        </c:ser>

                        <c:dLbls>
                          <c:showLegendKey val="0"/>
                          <c:showVal val="1"/>
                          <c:showCatName val="1"/>
                          <c:showSerName val="0"/>
                          <c:showPercent val="1"/>
                        </c:dLbls>
                      </c:pieChart>
                    </c:plotArea>

                    <c:legend>
                      <c:legendPos val="r"/>
                      <c:layout/>
                    </c:legend>

                    <c:plotVisOnly val="1"/>
                    <c:dispBlanksAs val="zero"/>
                  </c:chart>
                </c:chartSpace>
                """;
        }

        private static string BarChartXml(
            string title,
            List<ChartPoint> points
        )
        {
            var lastRow =
                points.Count +
                1;

            return $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <c:chartSpace
                    xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart"
                    xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">

                  <c:chart>
                    {ChartTitleXml(title)}

                    <c:plotArea>
                      <c:layout/>

                      <c:barChart>
                        <c:barDir val="col"/>
                        <c:grouping val="clustered"/>
                        <c:varyColors val="0"/>

                        <c:ser>
                          <c:idx val="0"/>
                          <c:order val="0"/>

                          <c:spPr>
                            <a:solidFill>
                              <a:srgbClr val="{Teal}"/>
                            </a:solidFill>
                          </c:spPr>

                          <c:cat>
                            <c:strRef>
                              <c:f>Calc!$G$2:$G${lastRow}</c:f>
                              {StringCacheXml(points)}
                            </c:strRef>
                          </c:cat>

                          <c:val>
                            <c:numRef>
                              <c:f>Calc!$H$2:$H${lastRow}</c:f>
                              {NumberCacheXml(points)}
                            </c:numRef>
                          </c:val>
                        </c:ser>

                        <c:dLbls>
                          <c:showVal val="1"/>
                        </c:dLbls>

                        <c:gapWidth val="70"/>
                        <c:axId val="47822112"/>
                        <c:axId val="47823232"/>
                      </c:barChart>

                      <c:catAx>
                        <c:axId val="47822112"/>
                        <c:scaling>
                          <c:orientation val="minMax"/>
                        </c:scaling>
                        <c:delete val="0"/>
                        <c:axPos val="b"/>
                        <c:tickLblPos val="nextTo"/>
                        <c:crossAx val="47823232"/>
                        <c:crosses val="autoZero"/>
                        <c:auto val="1"/>
                        <c:lblAlgn val="ctr"/>
                        <c:lblOffset val="100"/>
                      </c:catAx>

                      <c:valAx>
                        <c:axId val="47823232"/>
                        <c:scaling>
                          <c:orientation val="minMax"/>
                        </c:scaling>
                        <c:delete val="0"/>
                        <c:axPos val="l"/>
                        <c:majorGridlines/>
                        <c:numFmt formatCode="0.##" sourceLinked="0"/>
                        <c:tickLblPos val="nextTo"/>
                        <c:crossAx val="47822112"/>
                        <c:crosses val="autoZero"/>
                        <c:crossBetween val="between"/>
                      </c:valAx>
                    </c:plotArea>

                    <c:legend>
                      <c:legendPos val="r"/>
                      <c:layout/>
                    </c:legend>

                    <c:plotVisOnly val="1"/>
                    <c:dispBlanksAs val="zero"/>
                  </c:chart>
                </c:chartSpace>
                """;
        }

        private static string ChartTitleXml(
            string title
        )
        {
            return $"""
                <c:title>
                  <c:tx>
                    <c:rich>
                      <a:bodyPr/>
                      <a:lstStyle/>
                      <a:p>
                        <a:r>
                          <a:rPr lang="en-ZA"
                                 sz="1200"
                                 b="1"/>
                          <a:t>{Xml(title)}</a:t>
                        </a:r>
                        <a:endParaRPr lang="en-ZA"/>
                      </a:p>
                    </c:rich>
                  </c:tx>
                  <c:layout/>
                  <c:overlay val="0"/>
                </c:title>
                """;
        }

        private static string StringCacheXml(
            List<ChartPoint> points
        )
        {
            var builder =
                new StringBuilder();

            builder.Append(
                $"<c:strCache><c:ptCount val=\"{points.Count}\"/>"
            );

            for (
                var index = 0;
                index < points.Count;
                index++
            )
            {
                builder.Append(
                    $"<c:pt idx=\"{index}\"><c:v>{Xml(points[index].Label)}</c:v></c:pt>"
                );
            }

            builder.Append(
                "</c:strCache>"
            );

            return builder.ToString();
        }

        private static string NumberCacheXml(
            List<ChartPoint> points
        )
        {
            var builder =
                new StringBuilder();

            builder.Append(
                $"<c:numCache><c:formatCode>0.##</c:formatCode><c:ptCount val=\"{points.Count}\"/>"
            );

            for (
                var index = 0;
                index < points.Count;
                index++
            )
            {
                builder.Append(
                    $"<c:pt idx=\"{index}\"><c:v>{points[index].Value.ToString("0.########", CultureInfo.InvariantCulture)}</c:v></c:pt>"
                );
            }

            builder.Append(
                "</c:numCache>"
            );

            return builder.ToString();
        }

        // =====================================================
        // STYLES
        // =====================================================

        private static string StylesXml()
        {
            return $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">

                  <numFmts count="2">
                    <numFmt numFmtId="164" formatCode="yyyy-mm-dd"/>
                    <numFmt numFmtId="165" formatCode="yyyy-mm-dd hh:mm"/>
                  </numFmts>

                  <fonts count="7">
                    <font>
                      <sz val="10"/>
                      <name val="Calibri"/>
                      <color rgb="FF{Slate}"/>
                    </font>

                    <font>
                      <b/>
                      <sz val="18"/>
                      <name val="Calibri"/>
                      <color rgb="FF{White}"/>
                    </font>

                    <font>
                      <b/>
                      <sz val="13"/>
                      <name val="Calibri"/>
                      <color rgb="FF{White}"/>
                    </font>

                    <font>
                      <b/>
                      <sz val="9"/>
                      <name val="Calibri"/>
                      <color rgb="FF{SlateSoft}"/>
                    </font>

                    <font>
                      <b/>
                      <sz val="11"/>
                      <name val="Calibri"/>
                      <color rgb="FF{TealDark}"/>
                    </font>

                    <font>
                      <b/>
                      <sz val="17"/>
                      <name val="Calibri"/>
                      <color rgb="FF{Slate}"/>
                    </font>

                    <font>
                      <i/>
                      <sz val="9"/>
                      <name val="Calibri"/>
                      <color rgb="FF{SlateSoft}"/>
                    </font>
                  </fonts>

                  <fills count="10">
                    <fill>
                      <patternFill patternType="none"/>
                    </fill>

                    <fill>
                      <patternFill patternType="gray125"/>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{Teal}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{TealLight}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{Surface}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{Green}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{Amber}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{Red}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{White}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>

                    <fill>
                      <patternFill patternType="solid">
                        <fgColor rgb="FF{BlueLight}"/>
                        <bgColor indexed="64"/>
                      </patternFill>
                    </fill>
                  </fills>

                  <borders count="3">
                    <border>
                      <left/>
                      <right/>
                      <top/>
                      <bottom/>
                      <diagonal/>
                    </border>

                    <border>
                      <left style="thin">
                        <color rgb="FF{Border}"/>
                      </left>
                      <right style="thin">
                        <color rgb="FF{Border}"/>
                      </right>
                      <top style="thin">
                        <color rgb="FF{Border}"/>
                      </top>
                      <bottom style="thin">
                        <color rgb="FF{Border}"/>
                      </bottom>
                      <diagonal/>
                    </border>

                    <border>
                      <left style="medium">
                        <color rgb="FF{Teal}"/>
                      </left>
                      <right style="thin">
                        <color rgb="FF{Border}"/>
                      </right>
                      <top style="thin">
                        <color rgb="FF{Border}"/>
                      </top>
                      <bottom style="thin">
                        <color rgb="FF{Border}"/>
                      </bottom>
                      <diagonal/>
                    </border>
                  </borders>

                  <cellStyleXfs count="1">
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="0"
                        borderId="0"/>
                  </cellStyleXfs>

                  <cellXfs count="22">

                    <!-- 0 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="0"
                        borderId="0"
                        xfId="0"/>

                    <!-- 1 -->
                    <xf numFmtId="0"
                        fontId="1"
                        fillId="2"
                        borderId="0"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 2 -->
                    <xf numFmtId="0"
                        fontId="2"
                        fillId="2"
                        borderId="0"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 3 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="4"
                        borderId="0"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 4 -->
                    <xf numFmtId="0"
                        fontId="3"
                        fillId="3"
                        borderId="0"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 5 -->
                    <xf numFmtId="0"
                        fontId="4"
                        fillId="3"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 6 -->
                    <xf numFmtId="0"
                        fontId="3"
                        fillId="4"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 7 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="8"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 8 -->
                    <xf numFmtId="0"
                        fontId="3"
                        fillId="4"
                        borderId="2"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 9 -->
                    <xf numFmtId="0"
                        fontId="5"
                        fillId="8"
                        borderId="2"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="center"/>
                    </xf>

                    <!-- 10 -->
                    <xf numFmtId="0"
                        fontId="2"
                        fillId="2"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 11 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="8"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 wrapText="1"/>
                    </xf>

                    <!-- 12 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="4"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 wrapText="1"/>
                    </xf>

                    <!-- 13 -->
                    <xf numFmtId="164"
                        fontId="0"
                        fillId="8"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 14 -->
                    <xf numFmtId="165"
                        fontId="0"
                        fillId="8"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"/>
                    </xf>

                    <!-- 15 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="8"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="right"/>
                    </xf>

                    <!-- 16 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="5"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="center"/>
                    </xf>

                    <!-- 17 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="6"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="center"/>
                    </xf>

                    <!-- 18 -->
                    <xf numFmtId="0"
                        fontId="0"
                        fillId="7"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="center"/>
                    </xf>

                    <!-- 19 -->
                    <xf numFmtId="0"
                        fontId="6"
                        fillId="4"
                        borderId="0"
                        xfId="0">
                      <alignment vertical="center"
                                 wrapText="1"/>
                    </xf>

                    <!-- 20 percentage KPI -->
                    <xf numFmtId="10"
                        fontId="5"
                        fillId="8"
                        borderId="2"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="center"/>
                    </xf>

                    <!-- 21 dashboard date filter -->
                    <xf numFmtId="164"
                        fontId="0"
                        fillId="9"
                        borderId="1"
                        xfId="0">
                      <alignment vertical="center"
                                 horizontal="center"/>
                    </xf>
                  </cellXfs>

                  <cellStyles count="1">
                    <cellStyle name="Normal"
                               xfId="0"
                               builtinId="0"/>
                  </cellStyles>
                </styleSheet>
                """;
        }

        // =====================================================
        // VALUE HELPERS
        // =====================================================

        private static object? NormalizeExcelValue(
            object? value,
            string dataType
        )
        {
            if (
                value ==
                null
            )
            {
                return "";
            }

            if (
                dataType.Equals(
                    "number",
                    StringComparison.OrdinalIgnoreCase
                ) &&
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
                    value is DateTime dateTime
                )
                {
                    return dateTime;
                }

                if (
                    value is DateOnly dateOnly
                )
                {
                    return dateOnly.ToDateTime(
                        TimeOnly.MinValue
                    );
                }

                if (
                    DateTime.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo.InvariantCulture
                        ),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal,
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
            ) ??
            "";
        }

        private static int ExcelDataStyle(
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
                return 13;
            }

            if (
                dataType.Equals(
                    "datetime",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return 14;
            }

            if (
                dataType.Equals(
                    "number",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return 15;
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
                    ) ??
                    "";

                if (
                    IsPositiveStatus(
                        status
                    )
                )
                {
                    return 16;
                }

                if (
                    IsWarningStatus(
                        status
                    )
                )
                {
                    return 17;
                }

                if (
                    IsNegativeStatus(
                        status
                    )
                )
                {
                    return 18;
                }
            }

            return rowIndex %
                2 ==
                0
                    ? 11
                    : 12;
        }

        private static ClinicAdminDynamicReportColumnDto?
            GetDateColumn(
                ClinicAdminDynamicReportPreviewDto report
            )
        {
            var preferred =
                new[]
                {
                    "scheduled",
                    "collected",
                    "recorded",
                    "registered",
                    "created",
                    "updated",
                    "occurred",
                    "date"
                };

            foreach (
                var key in preferred
            )
            {
                var column =
                    report.Columns
                        .FirstOrDefault(
                            item =>
                                item.Key.Equals(
                                    key,
                                    StringComparison.OrdinalIgnoreCase
                                ) &&
                                (
                                    item.DataType.Equals(
                                        "date",
                                        StringComparison.OrdinalIgnoreCase
                                    ) ||
                                    item.DataType.Equals(
                                        "datetime",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                        );

                if (
                    column !=
                    null
                )
                {
                    return column;
                }
            }

            return report.Columns
                .FirstOrDefault(
                    item =>
                        item.DataType.Equals(
                            "date",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        item.DataType.Equals(
                            "datetime",
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        }

        private static DateTime? MinDate(
            ClinicAdminDynamicReportPreviewDto report,
            string key
        )
        {
            var values =
                report.Rows
                    .Select(
                        row =>
                            RowDate(
                                row,
                                key
                            )
                    )
                    .Where(
                        value =>
                            value.HasValue
                    )
                    .Select(
                        value =>
                            value!.Value
                    )
                    .ToList();

            return values.Count ==
                0
                    ? null
                    : values.Min();
        }

        private static DateTime? MaxDate(
            ClinicAdminDynamicReportPreviewDto report,
            string key
        )
        {
            var values =
                report.Rows
                    .Select(
                        row =>
                            RowDate(
                                row,
                                key
                            )
                    )
                    .Where(
                        value =>
                            value.HasValue
                    )
                    .Select(
                        value =>
                            value!.Value
                    )
                    .ToList();

            return values.Count ==
                0
                    ? null
                    : values.Max();
        }

        private static DateTime? RowDate(
            Dictionary<string, object?> row,
            string key
        )
        {
            if (
                !TryGetRowValue(
                    row,
                    key,
                    out var value
                ) ||
                value ==
                null
            )
            {
                return null;
            }

            if (
                value is DateTime dateTime
            )
            {
                return dateTime;
            }

            if (
                value is DateOnly dateOnly
            )
            {
                return dateOnly.ToDateTime(
                    TimeOnly.MinValue
                );
            }

            return DateTime.TryParse(
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture
                ),
                out var parsed
            )
                ? parsed
                : null;
        }

        private static string RowText(
            Dictionary<string, object?> row,
            string key
        )
        {
            if (
                !TryGetRowValue(
                    row,
                    key,
                    out var value
                ) ||
                value ==
                null
            )
            {
                return "";
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ) ??
            "";
        }

        private static double RowNumber(
            Dictionary<string, object?> row,
            string key
        )
        {
            if (
                !TryGetRowValue(
                    row,
                    key,
                    out var value
                ) ||
                value ==
                null
            )
            {
                return 0;
            }

            return double.TryParse(
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture
                ),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var number
            )
                ? number
                : 0;
        }

        private static bool TryGetRowValue(
            Dictionary<string, object?> row,
            string key,
            out object? value
        )
        {
            if (
                row.TryGetValue(
                    key,
                    out value
                )
            )
            {
                return true;
            }

            var actual =
                row.Keys
                    .FirstOrDefault(
                        candidate =>
                            candidate.Equals(
                                key,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

            if (
                actual ==
                null
            )
            {
                value =
                    null;

                return false;
            }

            value =
                row[actual];

            return true;
        }

        private static ClinicAdminDynamicReportColumnDto?
            FindColumn(
                ClinicAdminDynamicReportPreviewDto report,
                string key
            )
        {
            return report.Columns
                .FirstOrDefault(
                    column =>
                        column.Key.Equals(
                            key,
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        }

        private static int ColumnIndex(
            ClinicAdminDynamicReportPreviewDto report,
            string key
        )
        {
            for (
                var index = 0;
                index < report.Columns.Count;
                index++
            )
            {
                if (
                    report.Columns[index]
                        .Key
                        .Equals(
                            key,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                {
                    return index +
                        1;
                }
            }

            return 0;
        }

        private static string ColumnLabel(
            ClinicAdminDynamicReportPreviewDto report,
            string key
        )
        {
            return FindColumn(
                report,
                key
            )?
            .Label ??
            key;
        }

        private static string AbsoluteReference(
            string reference
        )
        {
            var letters =
                new string(
                    reference
                        .TakeWhile(
                            char.IsLetter
                        )
                        .ToArray()
                );

            var digits =
                new string(
                    reference
                        .SkipWhile(
                            char.IsLetter
                        )
                        .ToArray()
                );

            return $"${letters}${digits}";
        }

        private static double ColumnWidth(
            string label
        )
        {
            return Math.Max(
                13,
                Math.Min(
                    30,
                    label.Length +
                    8
                )
            );
        }

        private static string FormulaText(
            string value
        )
        {
            return value.Replace(
                "\"",
                "\"\""
            );
        }

        private static string Xml(
            string? value
        )
        {
            return SecurityElement.Escape(
                value ??
                ""
            ) ??
            "";
        }

        // =====================================================
        // WORKSHEET WRITER
        // =====================================================

        private sealed class WorksheetBuilder
        {
            private readonly SortedDictionary<
                int,
                List<CellDefinition>
            >
            _rows =
                new();

            private readonly List<string>
                _merges =
                    new();

            private readonly List<
                ValidationDefinition
            >
            _validations =
                new();

            private double[] _widths =
                Array.Empty<double>();

            public int FreezeRows
            {
                get;
                set;
            }

            public string? AutoFilterRef
            {
                get;
                set;
            }

            public string? DrawingRelationshipId
            {
                get;
                set;
            }

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
                AddCell(
                    row,
                    new CellDefinition(
                        column,
                        value,
                        style,
                        null,
                        null
                    )
                );
            }

            public void Formula(
                int row,
                int column,
                string formula,
                int style,
                object? cachedValue
            )
            {
                AddCell(
                    row,
                    new CellDefinition(
                        column,
                        null,
                        style,
                        formula,
                        cachedValue
                    )
                );
            }

            public void AddListValidation(
                string cellReference,
                string definedName
            )
            {
                _validations.Add(
                    new ValidationDefinition(
                        cellReference,
                        definedName
                    )
                );
            }

            private void AddCell(
                int row,
                CellDefinition cell
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
                    cell
                );
            }

            public string Build()
            {
                var xml =
                    new StringBuilder();

                xml.Append(
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <worksheet
                        xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                        xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                    """
                );

                xml.Append(
                    "<sheetViews><sheetView workbookViewId=\"0\">"
                );

                if (
                    FreezeRows >
                    0
                )
                {
                    xml.Append(
                        $"<pane ySplit=\"{FreezeRows}\" topLeftCell=\"A{FreezeRows + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/>"
                    );
                }

                xml.Append(
                    "</sheetView></sheetViews>"
                );

                xml.Append(
                    "<sheetFormatPr defaultRowHeight=\"15\"/>"
                );

                if (
                    _widths.Length >
                    0
                )
                {
                    xml.Append(
                        "<cols>"
                    );

                    for (
                        var index = 0;
                        index < _widths.Length;
                        index++
                    )
                    {
                        xml.Append(
                            $"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{_widths[index].ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>"
                        );
                    }

                    xml.Append(
                        "</cols>"
                    );
                }

                xml.Append(
                    "<sheetData>"
                );

                foreach (
                    var row in _rows
                )
                {
                    xml.Append(
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
                        xml.Append(
                            CellXml(
                                row.Key,
                                cell
                            )
                        );
                    }

                    xml.Append(
                        "</row>"
                    );
                }

                xml.Append(
                    "</sheetData>"
                );

                /*
                 * IMPORTANT:
                 *
                 * autoFilter must appear before mergeCells in the
                 * worksheet schema. The previous exporter wrote the
                 * elements in the opposite order, causing Excel to
                 * repair sheet2.xml.
                 */
                if (
                    !string.IsNullOrWhiteSpace(
                        AutoFilterRef
                    )
                )
                {
                    xml.Append(
                        $"<autoFilter ref=\"{AutoFilterRef}\"/>"
                    );
                }

                if (
                    _merges.Count >
                    0
                )
                {
                    xml.Append(
                        $"<mergeCells count=\"{_merges.Count}\">"
                    );

                    foreach (
                        var merge in _merges
                    )
                    {
                        xml.Append(
                            $"<mergeCell ref=\"{merge}\"/>"
                        );
                    }

                    xml.Append(
                        "</mergeCells>"
                    );
                }

                if (
                    _validations.Count >
                    0
                )
                {
                    xml.Append(
                        $"<dataValidations count=\"{_validations.Count}\">"
                    );

                    foreach (
                        var validation in _validations
                    )
                    {
                        xml.Append(
                            $"<dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" errorTitle=\"Invalid filter\" error=\"Choose a value from the dropdown list.\" sqref=\"{validation.CellReference}\"><formula1>{validation.DefinedName}</formula1></dataValidation>"
                        );
                    }

                    xml.Append(
                        "</dataValidations>"
                    );
                }

                xml.Append(
                    """
                    <pageMargins left="0.3"
                                 right="0.3"
                                 top="0.5"
                                 bottom="0.5"
                                 header="0.2"
                                 footer="0.2"/>

                    <pageSetup orientation="landscape"
                               fitToWidth="1"
                               fitToHeight="0"/>
                    """
                );

                if (
                    !string.IsNullOrWhiteSpace(
                        DrawingRelationshipId
                    )
                )
                {
                    xml.Append(
                        $"<drawing r:id=\"{DrawingRelationshipId}\"/>"
                    );
                }

                xml.Append(
                    "</worksheet>"
                );

                return xml.ToString();
            }

            private static string CellXml(
                int row,
                CellDefinition cell
            )
            {
                var reference =
                    $"{ColumnName(cell.Column)}{row}";

                if (
                    cell.Formula !=
                    null
                )
                {
                    return $"<c r=\"{reference}\" s=\"{cell.Style}\"><f>{Xml(cell.Formula)}</f>{CachedValueXml(cell.CachedValue)}</c>";
                }

                if (
                    cell.Value is DateTime dateTime
                )
                {
                    return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"n\"><v>{dateTime.ToOADate().ToString("0.########", CultureInfo.InvariantCulture)}</v></c>";
                }

                if (
                    cell.Value is DateOnly dateOnly
                )
                {
                    var date =
                        dateOnly.ToDateTime(
                            TimeOnly.MinValue
                        );

                    return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"n\"><v>{date.ToOADate().ToString("0.########", CultureInfo.InvariantCulture)}</v></c>";
                }

                if (
                    cell.Value is decimal ||
                    cell.Value is int ||
                    cell.Value is long ||
                    cell.Value is double ||
                    cell.Value is float
                )
                {
                    return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"n\"><v>{Convert.ToString(cell.Value, CultureInfo.InvariantCulture)}</v></c>";
                }

                if (
                    cell.Value is bool boolean
                )
                {
                    return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"b\"><v>{(boolean ? 1 : 0)}</v></c>";
                }

                var text =
                    Xml(
                        Convert.ToString(
                            cell.Value,
                            CultureInfo.InvariantCulture
                        ) ??
                        ""
                    );

                return $"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{text}</t></is></c>";
            }

            private static string CachedValueXml(
                object? value
            )
            {
                if (
                    value ==
                    null
                )
                {
                    return "";
                }

                if (
                    value is decimal ||
                    value is int ||
                    value is long ||
                    value is double ||
                    value is float
                )
                {
                    return $"<v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v>";
                }

                if (
                    value is bool boolean
                )
                {
                    return $"<v>{(boolean ? 1 : 0)}</v>";
                }

                return "";
            }

            private sealed record CellDefinition(
                int Column,
                object? Value,
                int Style,
                string? Formula,
                object? CachedValue
            );

            private sealed record ValidationDefinition(
                string CellReference,
                string DefinedName
            );
        }

        // =====================================================
        // PDF
        // =====================================================

        private static List<string> BuildPdfPages(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            var pages =
                new List<string>();

            var first =
                new PdfCanvas();

            first.BrandHeader(
                report.Title,
                report.ClinicName
            );

            first.Text(
                32,
                78,
                8,
                RangeText(
                    report
                ),
                false,
                "#475569"
            );

            first.Text(
                32,
                92,
                8,
                $"Requested by: {report.RequestedBy}",
                false,
                "#475569"
            );

            first.Text(
                810,
                92,
                8,
                $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm}",
                false,
                "#475569",
                true
            );

            var summary =
                report.Summary
                    .Take(
                        4
                    )
                    .ToList();

            const double cardWidth =
                186;

            for (
                var index = 0;
                index < summary.Count;
                index++
            )
            {
                first.Card(
                    32 +
                    index *
                    (
                        cardWidth +
                        10
                    ),
                    118,
                    cardWidth,
                    62,
                    summary[index]
                        .Label,
                    summary[index]
                        .Value
                );
            }

            first.Text(
                32,
                206,
                10,
                "Filtered report data",
                true,
                "#0F172A"
            );

            var previewColumns =
                report.Columns
                    .Take(
                        Math.Min(
                            6,
                            report.Columns.Count
                        )
                    )
                    .ToList();

            DrawPdfTable(
                first,
                previewColumns,
                report.Rows
                    .Take(
                        14
                    )
                    .ToList(),
                226,
                report.Rows.Count >
                14
                    ? $"Previewing 14 of {report.Rows.Count} rows."
                    : $"{report.Rows.Count} rows."
            );

            pages.Add(
                first.Content
            );

            var detailColumns =
                report.Columns
                    .Take(
                        8
                    )
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
                    detailColumns,
                    report.Rows
                        .Skip(
                            offset
                        )
                        .Take(
                            rowsPerPage
                        )
                        .ToList(),
                    104,
                    $"Rows {offset + 1}-{Math.Min(offset + rowsPerPage, report.Rows.Count)} of {report.Rows.Count}"
                );

                pages.Add(
                    page.Content
                );
            }

            if (
                report.Rows.Count ==
                0
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
                    110,
                    12,
                    "No rows matched the selected filters.",
                    true,
                    "#0F172A"
                );

                pages.Add(
                    page.Content
                );
            }

            for (
                var index = 0;
                index < pages.Count;
                index++
            )
            {
                var page =
                    new PdfCanvas(
                        pages[index]
                    );

                page.Footer(
                    index +
                    1,
                    pages.Count,
                    report.ClinicName
                );

                pages[index] =
                    page.Content;
            }

            return pages;
        }

        private static void DrawPdfTable(
            PdfCanvas page,
            List<ClinicAdminDynamicReportColumnDto> columns,
            List<Dictionary<string, object?>> rows,
            double startY,
            string footer
        )
        {
            if (
                columns.Count ==
                0
            )
            {
                return;
            }

            const double left =
                32;

            const double width =
                778;

            const double headerHeight =
                24;

            const double rowHeight =
                19;

            var columnWidth =
                width /
                columns.Count;

            page.FillRect(
                left,
                startY,
                width,
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
                    startY +
                    7,
                    6.7,
                    Truncate(
                        columns[index]
                            .Label,
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
                    width,
                    rowHeight,
                    rowIndex %
                    2 ==
                    0
                        ? "#FFFFFF"
                        : "#F8FAFC"
                );

                page.Line(
                    left,
                    y +
                    rowHeight,
                    left +
                    width,
                    y +
                    rowHeight,
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

                    TryGetRowValue(
                        row,
                        column.Key,
                        out var value
                    );

                    var text =
                        DisplayValue(
                            value,
                            column.DataType
                        );

                    page.Text(
                        left +
                        columnIndex *
                        columnWidth +
                        5,
                        y +
                        6,
                        6.4,
                        Truncate(
                            text,
                            24
                        ),
                        false,
                        column.DataType.Equals(
                            "status",
                            StringComparison.OrdinalIgnoreCase
                        )
                            ? StatusPdfColor(
                                text
                            )
                            : "#334155"
                    );
                }

                y +=
                    rowHeight;
            }

            page.Text(
                left,
                y +
                12,
                6.8,
                footer,
                false,
                "#64748B"
            );
        }

        // =====================================================
        // SHARED DISPLAY HELPERS
        // =====================================================

        private static string RangeText(
            ClinicAdminDynamicReportPreviewDto report
        )
        {
            if (
                report.DateFrom ==
                null &&
                report.DateTo ==
                null
            )
            {
                return "Current-state report";
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
            if (
                value ==
                null
            )
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
                        out var parsed
                    )
                )
                {
                    return parsed.ToString(
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture
                    );
                }
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ) ??
            "—";
        }

        private static bool IsPositiveStatus(
            string status
        )
        {
            var value =
                status.ToLowerInvariant();

            return value.Contains(
                       "active"
                   ) ||
                   value.Contains(
                       "healthy"
                   ) ||
                   value.Contains(
                       "collected"
                   ) ||
                   value.Contains(
                       "completed"
                   ) ||
                   value.Contains(
                       "confirmed"
                   ) ||
                   value.Contains(
                       "taken"
                   );
        }

        private static bool IsWarningStatus(
            string status
        )
        {
            var value =
                status.ToLowerInvariant();

            return value.Contains(
                       "pending"
                   ) ||
                   value.Contains(
                       "scheduled"
                   ) ||
                   value.Contains(
                       "low stock"
                   );
        }

        private static bool IsNegativeStatus(
            string status
        )
        {
            var value =
                status.ToLowerInvariant();

            return value.Contains(
                       "cancel"
                   ) ||
                   value.Contains(
                       "missed"
                   ) ||
                   value.Contains(
                       "inactive"
                   ) ||
                   value.Contains(
                       "overdue"
                   );
        }

        private static string StatusPdfColor(
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

        private static string Truncate(
            string? value,
            int maximum
        )
        {
            var text =
                value ??
                "";

            if (
                text.Length <=
                maximum
            )
            {
                return text;
            }

            return text[
                ..Math.Max(
                    1,
                    maximum -
                    3
                )
            ] +
            "...";
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
                    new UTF8Encoding(
                        false
                    )
                );

            writer.Write(
                content
            );
        }

        // =====================================================
        // PDF PRIMITIVES
        // =====================================================

        private sealed class PdfCanvas
        {
            private const double PageWidth =
                842;

            private const double PageHeight =
                595;

            private readonly StringBuilder
                _builder;

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
                string scope
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
                    scope,
                    true,
                    "#FFFFFF",
                    true
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
                    x +
                    14,
                    y +
                    15,
                    7,
                    label,
                    true,
                    "#64748B"
                );

                Text(
                    x +
                    14,
                    y +
                    34,
                    17,
                    value,
                    true,
                    "#0F172A"
                );
            }

            public void Footer(
                int page,
                int total,
                string scope
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
                    $"PhilaLink | {scope} | Confidential report",
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
                    true
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
                    Rgb(
                        color
                    );

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
                    Rgb(
                        color
                    );

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
                    Rgb(
                        color
                    );

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
                    PdfText(
                        text
                    );

                var rgb =
                    Rgb(
                        color
                    );

                var width =
                    safe.Length *
                    size *
                    (
                        bold
                            ? 0.56
                            : 0.51
                    );

                var drawX =
                    rightAlign
                        ? x -
                          width
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
                        safe ==
                        '\\' ||
                        safe ==
                        '(' ||
                        safe ==
                        ')'
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
                        .TrimStart(
                            '#'
                        );

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
                    ) /
                    255d,

                    Convert.ToInt32(
                        hex.Substring(
                            2,
                            2
                        ),
                        16
                    ) /
                    255d,

                    Convert.ToInt32(
                        hex.Substring(
                            4,
                            2
                        ),
                        16
                    ) /
                    255d
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

                objects.Add(
                    string.Empty
                );

                objects.Add(
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
                );

                objects.Add(
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
                );

                var pageObjectNumbers =
                    new List<int>();

                const int fontRegularObject =
                    3;

                const int fontBoldObject =
                    4;

                foreach (
                    var content in pageContents
                )
                {
                    var bytes =
                        Encoding.ASCII.GetBytes(
                            content
                        );

                    var contentObject =
                        objects.Count +
                        1;

                    objects.Add(
                        $"<< /Length {bytes.Length} >>\nstream\n{content}\nendstream"
                    );

                    var pageObject =
                        objects.Count +
                        1;

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
                        true
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

        // =====================================================
        // INTERNAL RECORDS
        // =====================================================

        private sealed record DashboardFilter(
            string ColumnKey,
            string Label,
            string ValueCell,
            string ListName,
            int ListColumn,
            List<string> Options
        );

        private sealed record FilterPositionDefinition(
            int Row,
            int LabelColumn,
            int ValueColumn
        );

        private sealed record VisualConfig(
            string PieCategoryKey,
            string PieTitle,
            string BarCategoryKey,
            string? BarValueKey,
            string BarTitle
        );

        private sealed record ChartPoint(
            string Label,
            double Value
        );

        private enum KpiMode
        {
            CountVisible,
            SumVisible,
            CountEquals,
            RatioEquals
        }

        private sealed record KpiDefinition(
            string Label,
            KpiMode Mode,
            string? ColumnKey,
            string? ValueKey,
            IReadOnlyCollection<string> Values,
            bool IsPercentage,
            double CachedValue
        );
    }
}
