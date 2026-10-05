using PersonalProject.Models.DTOs;
using PersonalProject.Utilities;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class ReportBuilderSmokeTests
    {
        [Fact]
        public void
            CsvReportContainsClinicIdentity()
        {
            var report =
                LegacyReport();

            var bytes =
                ClinicAdminReportBuilder
                    .BuildCsv(
                        report
                    );

            var text =
                Encoding.UTF8
                    .GetString(
                        bytes
                    );

            Assert.Contains(
                "PhilaLink Clinic Operations Report",
                text
            );

            Assert.Contains(
                "Test Clinic",
                text
            );
        }

        [Fact]
        public void
            LegacyExcelReportProducesZipContainer()
        {
            var bytes =
                ClinicAdminReportBuilder
                    .BuildXlsx(
                        LegacyReport()
                    );

            Assert.True(
                bytes.Length >
                4
            );

            Assert.Equal(
                (byte)'P',
                bytes[0]
            );

            Assert.Equal(
                (byte)'K',
                bytes[1]
            );
        }

        [Fact]
        public void
            PdfReportProducesPdfDocument()
        {
            var bytes =
                ClinicAdminReportBuilder
                    .BuildPdf(
                        LegacyReport()
                    );

            Assert.True(
                bytes.Length >
                8
            );

            var header =
                Encoding.ASCII
                    .GetString(
                        bytes,
                        0,
                        Math.Min(
                            bytes.Length,
                            8
                        )
                    );

            Assert.StartsWith(
                "%PDF-",
                header
            );
        }

        // =====================================================
        // DYNAMIC XLSX STRUCTURE
        // =====================================================

        [Fact]
        public void
            DynamicExcelContainsRequiredWorkbookParts()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            using var stream =
                new MemoryStream(
                    bytes
                );

            using var archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Read
                );

            AssertEntry(
                archive,
                "[Content_Types].xml"
            );

            AssertEntry(
                archive,
                "_rels/.rels"
            );

            AssertEntry(
                archive,
                "xl/workbook.xml"
            );

            AssertEntry(
                archive,
                "xl/_rels/workbook.xml.rels"
            );

            AssertEntry(
                archive,
                "xl/styles.xml"
            );

            AssertEntry(
                archive,
                "xl/worksheets/sheet1.xml"
            );

            AssertEntry(
                archive,
                "xl/worksheets/sheet2.xml"
            );

            AssertEntry(
                archive,
                "xl/worksheets/sheet3.xml"
            );

            AssertEntry(
                archive,
                "xl/worksheets/sheet4.xml"
            );

            AssertEntry(
                archive,
                "xl/worksheets/_rels/sheet1.xml.rels"
            );

            AssertEntry(
                archive,
                "xl/drawings/drawing1.xml"
            );

            AssertEntry(
                archive,
                "xl/drawings/_rels/drawing1.xml.rels"
            );

            AssertEntry(
                archive,
                "xl/charts/chart1.xml"
            );

            AssertEntry(
                archive,
                "xl/charts/chart2.xml"
            );
        }

        [Fact]
        public void
            EveryGeneratedXmlPartIsWellFormed()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            using var stream =
                new MemoryStream(
                    bytes
                );

            using var archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Read
                );

            var entries =
                archive.Entries
                    .Where(
                        entry =>
                            entry.FullName
                                .EndsWith(
                                    ".xml",
                                    StringComparison.OrdinalIgnoreCase
                                ) ||
                            entry.FullName
                                .EndsWith(
                                    ".rels",
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .ToList();

            Assert.NotEmpty(
                entries
            );

            foreach (
                var entry in entries
            )
            {
                using var entryStream =
                    entry.Open();

                var document =
                    XDocument.Load(
                        entryStream
                    );

                Assert.NotNull(
                    document.Root
                );
            }
        }

        // =====================================================
        // EXCEL REPAIR REGRESSION
        // =====================================================

        [Fact]
        public void
            DataSheetWritesAutoFilterBeforeMergeCells()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var xml =
                ReadEntry(
                    bytes,
                    "xl/worksheets/sheet2.xml"
                );

            var autoFilter =
                xml.IndexOf(
                    "<autoFilter",
                    StringComparison.Ordinal
                );

            var mergeCells =
                xml.IndexOf(
                    "<mergeCells",
                    StringComparison.Ordinal
                );

            Assert.True(
                autoFilter >=
                0
            );

            Assert.True(
                mergeCells >=
                0
            );

            Assert.True(
                autoFilter <
                mergeCells,
                "autoFilter must appear before mergeCells in worksheet XML."
            );
        }

        // =====================================================
        // DASHBOARD FILTERS
        // =====================================================

        [Fact]
        public void
            DashboardContainsDropdownFilters()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var dashboard =
                ReadEntry(
                    bytes,
                    "xl/worksheets/sheet1.xml"
                );

            Assert.Contains(
                "<dataValidations",
                dashboard
            );

            Assert.Contains(
                "<formula1>FilterList1</formula1>",
                dashboard
            );

            Assert.Contains(
                "<formula1>FilterList2</formula1>",
                dashboard
            );

            var workbook =
                ReadEntry(
                    bytes,
                    "xl/workbook.xml"
                );

            Assert.Contains(
                "<definedNames>",
                workbook
            );

            Assert.Contains(
                "FilterList1",
                workbook
            );

            Assert.Contains(
                "FilterList2",
                workbook
            );
        }

        [Fact]
        public void
            WorkbookUsesAutomaticRecalculation()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var workbook =
                ReadEntry(
                    bytes,
                    "xl/workbook.xml"
                );

            Assert.Contains(
                "calcMode=\"auto\"",
                workbook
            );

            Assert.Contains(
                "fullCalcOnLoad=\"1\"",
                workbook
            );

            Assert.Contains(
                "forceFullCalc=\"1\"",
                workbook
            );
        }

        [Fact]
        public void
            CalculationSheetReferencesDashboardFilters()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var calc =
                ReadEntry(
                    bytes,
                    "xl/worksheets/sheet3.xml"
                );

            Assert.Contains(
                "Dashboard!$B$7",
                calc
            );

            Assert.Contains(
                "Dashboard!$E$7",
                calc
            );

            Assert.Contains(
                "SUMIFS",
                calc
            );

            Assert.Contains(
                "SUMPRODUCT",
                calc
            );
        }

        // =====================================================
        // KPIs
        // =====================================================

        [Fact]
        public void
            DashboardKpisReferenceCalculationSheet()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var dashboard =
                ReadEntry(
                    bytes,
                    "xl/worksheets/sheet1.xml"
                );

            Assert.Contains(
                "<f>Calc!$K$2</f>",
                dashboard
            );

            Assert.Contains(
                "<f>Calc!$K$3</f>",
                dashboard
            );

            Assert.Contains(
                "<f>Calc!$K$4</f>",
                dashboard
            );

            Assert.Contains(
                "<f>Calc!$K$5</f>",
                dashboard
            );
        }

        // =====================================================
        // FILTERED PREVIEW
        // =====================================================

        [Fact]
public void
    DashboardPreviewUsesRepairSafeScalarFormulas()
{
    var bytes =
        ClinicAdminDynamicReportBuilder
            .BuildExcel(
                DynamicInventoryReport()
            );

    var dashboard =
        ReadEntry(
            bytes,
            "xl/worksheets/sheet1.xml"
        );

    /*
     * FILTER() is intentionally forbidden here.
     *
     * The report builder writes worksheet XML itself,
     * and dynamic-array formulas require additional
     * OOXML metadata. Writing FILTER as a normal
     * formula caused Excel to repair sheet1.xml.
     */
    Assert.DoesNotContain(
        "FILTER(Data!A5:",
        dashboard
    );

    Assert.DoesNotContain(
        "_xlfn",
        dashboard
    );

    Assert.DoesNotContain(
        "t=\"array\"",
        dashboard
    );

    /*
     * The preview instead uses ordinary scalar
     * formulas which remain live when the dashboard
     * filter cells change.
     */
    Assert.Contains(
        "INDEX(Data!",
        dashboard
    );

    Assert.Contains(
        "AGGREGATE(15,6",
        dashboard
    );

    Assert.Contains(
        "Calc!$B$2:$B$",
        dashboard
    );

    Assert.Contains(
        "Preview shows up to 20 matching rows.",
        dashboard
    );
}

        // =====================================================
        // CHARTS
        // =====================================================

        [Fact]
        public void
            DashboardContainsDrawingRelationship()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var dashboard =
                ReadEntry(
                    bytes,
                    "xl/worksheets/sheet1.xml"
                );

            Assert.Contains(
                "<drawing r:id=\"rId1\"/>",
                dashboard
            );
        }

        [Fact]
        public void
            PieChartReadsDynamicCalculationRange()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var chart =
                ReadEntry(
                    bytes,
                    "xl/charts/chart1.xml"
                );

            Assert.Contains(
                "Calc!$D$2:$D$",
                chart
            );

            Assert.Contains(
                "Calc!$E$2:$E$",
                chart
            );

            Assert.Contains(
                "<c:pieChart>",
                chart
            );
        }

        [Fact]
        public void
            BarChartReadsDynamicCalculationRange()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var chart =
                ReadEntry(
                    bytes,
                    "xl/charts/chart2.xml"
                );

            Assert.Contains(
                "Calc!$G$2:$G$",
                chart
            );

            Assert.Contains(
                "Calc!$H$2:$H$",
                chart
            );

            Assert.Contains(
                "<c:barChart>",
                chart
            );
        }

        // =====================================================
        // WORKBOOK VISIBILITY
        // =====================================================

        [Fact]
        public void
            CalcAndListsSheetsAreHidden()
        {
            var bytes =
                ClinicAdminDynamicReportBuilder
                    .BuildExcel(
                        DynamicInventoryReport()
                    );

            var workbook =
                ReadEntry(
                    bytes,
                    "xl/workbook.xml"
                );

            Assert.Contains(
                "name=\"Dashboard\"",
                workbook
            );

            Assert.Contains(
                "name=\"Data\"",
                workbook
            );

            Assert.Contains(
                "name=\"Calc\" sheetId=\"3\" state=\"veryHidden\"",
                workbook
            );

            Assert.Contains(
                "name=\"Lists\" sheetId=\"4\" state=\"veryHidden\"",
                workbook
            );
        }

        // =====================================================
        // TEST HELPERS
        // =====================================================

        private static void AssertEntry(
            ZipArchive archive,
            string path
        )
        {
            Assert.NotNull(
                archive.GetEntry(
                    path
                )
            );
        }

        private static string ReadEntry(
            byte[] workbook,
            string path
        )
        {
            using var stream =
                new MemoryStream(
                    workbook
                );

            using var archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Read
                );

            var entry =
                archive.GetEntry(
                    path
                );

            Assert.NotNull(
                entry
            );

            using var entryStream =
                entry!.Open();

            using var reader =
                new StreamReader(
                    entryStream,
                    Encoding.UTF8
                );

            return reader.ReadToEnd();
        }

        private static ClinicAdminDynamicReportPreviewDto
            DynamicInventoryReport()
        {
            var now =
                DateTime.UtcNow;

            return new ClinicAdminDynamicReportPreviewDto
            {
                ReportType =
                    "Inventory",

                Title =
                    "Medication Inventory Report",

                ClinicName =
                    "Test Clinic",

                RequestedBy =
                    "Test Administrator",

                GeneratedAt =
                    now,

                Columns =
                    new()
                    {
                        new()
                        {
                            Key =
                                "medication",

                            Label =
                                "Medication",

                            DataType =
                                "text"
                        },

                        new()
                        {
                            Key =
                                "strength",

                            Label =
                                "Strength",

                            DataType =
                                "text"
                        },

                        new()
                        {
                            Key =
                                "form",

                            Label =
                                "Form",

                            DataType =
                                "text"
                        },

                        new()
                        {
                            Key =
                                "quantity",

                            Label =
                                "On Hand",

                            DataType =
                                "number"
                        },

                        new()
                        {
                            Key =
                                "unit",

                            Label =
                                "Unit",

                            DataType =
                                "text"
                        },

                        new()
                        {
                            Key =
                                "reorderLevel",

                            Label =
                                "Reorder Level",

                            DataType =
                                "number"
                        },

                        new()
                        {
                            Key =
                                "status",

                            Label =
                                "Stock Status",

                            DataType =
                                "status"
                        },

                        new()
                        {
                            Key =
                                "updated",

                            Label =
                                "Last Updated",

                            DataType =
                                "datetime"
                        }
                    },

                Rows =
                    new()
                    {
                        Row(
                            (
                                "medication",
                                "Metformin"
                            ),

                            (
                                "strength",
                                "500 mg"
                            ),

                            (
                                "form",
                                "Tablet"
                            ),

                            (
                                "quantity",
                                120
                            ),

                            (
                                "unit",
                                "tablets"
                            ),

                            (
                                "reorderLevel",
                                30
                            ),

                            (
                                "status",
                                "Healthy"
                            ),

                            (
                                "updated",
                                now.AddDays(
                                    -1
                                )
                            )
                        ),

                        Row(
                            (
                                "medication",
                                "Amlodipine"
                            ),

                            (
                                "strength",
                                "10 mg"
                            ),

                            (
                                "form",
                                "Tablet"
                            ),

                            (
                                "quantity",
                                12
                            ),

                            (
                                "unit",
                                "tablets"
                            ),

                            (
                                "reorderLevel",
                                20
                            ),

                            (
                                "status",
                                "Low stock"
                            ),

                            (
                                "updated",
                                now.AddDays(
                                    -2
                                )
                            )
                        ),

                        Row(
                            (
                                "medication",
                                "Amoxicillin"
                            ),

                            (
                                "strength",
                                "250 mg"
                            ),

                            (
                                "form",
                                "Capsule"
                            ),

                            (
                                "quantity",
                                0
                            ),

                            (
                                "unit",
                                "capsules"
                            ),

                            (
                                "reorderLevel",
                                10
                            ),

                            (
                                "status",
                                "Inactive"
                            ),

                            (
                                "updated",
                                now.AddDays(
                                    -3
                                )
                            )
                        )
                    },

                Summary =
                    new()
                    {
                        new()
                        {
                            Key =
                                "shown",

                            Label =
                                "Items shown",

                            Value =
                                "3"
                        },

                        new()
                        {
                            Key =
                                "units",

                            Label =
                                "Units on hand",

                            Value =
                                "132"
                        },

                        new()
                        {
                            Key =
                                "low",

                            Label =
                                "Low stock",

                            Value =
                                "1"
                        },

                        new()
                        {
                            Key =
                                "inactive",

                            Label =
                                "Inactive",

                            Value =
                                "1"
                        }
                    },

                FilterOptions =
                    new(
                        StringComparer.OrdinalIgnoreCase
                    )
                    {
                        [
                            "status"
                        ] =
                            new()
                            {
                                "All",
                                "Healthy",
                                "Low stock",
                                "Inactive"
                            },

                        [
                            "medication"
                        ] =
                            new()
                            {
                                "All",
                                "Amlodipine",
                                "Amoxicillin",
                                "Metformin"
                            }
                    }
            };
        }

        private static Dictionary<string, object?>
            Row(
                params (
                    string Key,
                    object? Value
                )[] values
            )
        {
            var result =
                new Dictionary<
                    string,
                    object?
                >(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                var (
                    key,
                    value
                ) in values
            )
            {
                result[key] =
                    value;
            }

            return result;
        }

        private static ClinicAdminReportDataDto
            LegacyReport()
        {
            var now =
                DateTime.UtcNow;

            return new ClinicAdminReportDataDto
            {
                ClinicName =
                    "Test Clinic",

                AdminName =
                    "Test Administrator",

                GeneratedAtUtc =
                    now,

                RangeStartUtc =
                    now.AddDays(
                        -30
                    ),

                RangeEndUtc =
                    now,

                Analytics =
                    new ClinicAdminAnalyticsDto
                    {
                        ClinicName =
                            "Test Clinic",

                        AdminName =
                            "Test Administrator",

                        GeneratedAtUtc =
                            now,

                        Kpis =
                            new ClinicAdminKpisDto
                            {
                                ActivePatients =
                                    10,

                                ActiveNurses =
                                    2,

                                ActiveProxies =
                                    3,

                                AppointmentsToday =
                                    4,

                                CollectionsDueToday =
                                    5,

                                LowStockItems =
                                    1
                            }
                    }
            };
        }
    }
}
