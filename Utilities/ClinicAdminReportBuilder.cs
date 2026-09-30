using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using PersonalProject.Models.DTOs;

namespace PersonalProject.Utilities
{
    public static class ClinicAdminReportBuilder
    {
        private const string DarkTeal = "0F766E";
        private const string Teal = "14B8A6";
        private const string PaleTeal = "CCFBF1";
        private const string LightTeal = "F0FDFA";
        private const string Slate = "0F172A";
        private const string MidSlate = "475569";
        private const string LightSlate = "E2E8F0";
        private const string VeryLightSlate = "F8FAFC";
        private const string Amber = "F59E0B";
        private const string PaleAmber = "FEF3C7";
        private const string Green = "16A34A";
        private const string PaleGreen = "DCFCE7";
        private const string Red = "DC2626";
        private const string PaleRed = "FEE2E2";
        private const string White = "FFFFFF";

        // =====================================================
        // CSV
        // =====================================================

        public static byte[] BuildCsv(
            ClinicAdminReportDataDto report
        )
        {
            var builder = new StringBuilder();

            Csv(builder, "PhilaLink Clinic Operations Report");
            Csv(builder, "Clinic", report.ClinicName);
            Csv(builder, "Report requested by", report.AdminName);
            Csv(builder, "Generated at", DateTimeText(report.GeneratedAtUtc));
            Csv(
                builder,
                "Reporting period",
                $"{DateText(report.RangeStartUtc)} - {DateText(report.RangeEndUtc)}"
            );

            builder.AppendLine();

            Csv(builder, "EXECUTIVE SUMMARY");
            Csv(builder, "Metric", "Value");

            var kpis = report.Analytics.Kpis;

            Csv(builder, "Active patients", kpis.ActivePatients);
            Csv(builder, "Active nurses", kpis.ActiveNurses);
            Csv(builder, "Active proxies", kpis.ActiveProxies);
            Csv(builder, "Appointments today", kpis.AppointmentsToday);
            Csv(builder, "Pending appointments", kpis.PendingAppointments);
            Csv(builder, "Collections due today", kpis.CollectionsDueToday);
            Csv(builder, "Overdue collections", kpis.OverdueCollections);
            Csv(builder, "Low stock items", kpis.LowStockItems);
            Csv(builder, "Collected this month", kpis.CollectedThisMonth);
            Csv(builder, "New patients this month", kpis.NewPatientsThisMonth);

            builder.AppendLine();

            Csv(
                builder,
                "MONTHLY ACTIVITY"
            );

            Csv(
                builder,
                "Period",
                "New patients",
                "Appointments",
                "Completed appointments",
                "Collections",
                "Completed collections"
            );

            foreach (var item in report.Analytics.MonthlyActivity)
            {
                Csv(
                    builder,
                    item.Period,
                    item.NewPatients,
                    item.Appointments,
                    item.CompletedAppointments,
                    item.Collections,
                    item.CompletedCollections
                );
            }

            builder.AppendLine();

            Csv(builder, "APPOINTMENTS");
            Csv(
                builder,
                "Scheduled",
                "Patient",
                "Type",
                "Mode",
                "Status",
                "Nurse"
            );

            foreach (var item in report.Appointments)
            {
                Csv(
                    builder,
                    DateTimeText(item.ScheduledAt),
                    item.PatientName,
                    item.Type,
                    item.Mode,
                    item.Status,
                    item.NurseName
                );
            }

            builder.AppendLine();

            Csv(builder, "COLLECTIONS");
            Csv(
                builder,
                "Scheduled",
                "Collected",
                "Patient",
                "Status",
                "Proxy",
                "Processed by",
                "Quantity"
            );

            foreach (var item in report.Collections)
            {
                Csv(
                    builder,
                    DateTimeText(item.ScheduledCollectionDate),
                    item.CollectedAt.HasValue
                        ? DateTimeText(item.CollectedAt.Value)
                        : "",
                    item.PatientName,
                    item.Status,
                    item.ProxyName,
                    item.ProcessedByNurseName,
                    item.TotalQuantity
                );
            }

            builder.AppendLine();

            Csv(builder, "INVENTORY");
            Csv(
                builder,
                "Medication",
                "Strength",
                "Form",
                "Unit",
                "Quantity",
                "Reorder level",
                "Status",
                "Active"
            );

            foreach (var item in report.Inventory)
            {
                Csv(
                    builder,
                    item.MedicationName,
                    item.Strength,
                    item.Form,
                    item.Unit,
                    item.QuantityOnHand,
                    item.ReorderLevel,
                    item.Status,
                    item.IsActive ? "Yes" : "No"
                );
            }

            builder.AppendLine();

            Csv(builder, "STAFF");
            Csv(
                builder,
                "Full name",
                "Role",
                "ID number",
                "Phone",
                "Email",
                "Active",
                "Created"
            );

            foreach (var item in report.Staff)
            {
                Csv(
                    builder,
                    item.FullName,
                    item.Role,
                    item.IdNumber,
                    item.PhoneNumber,
                    item.Email,
                    item.IsActive ? "Yes" : "No",
                    DateTimeText(item.CreatedAt)
                );
            }

            builder.AppendLine();

            Csv(builder, "NEW PATIENTS");
            Csv(
                builder,
                "Full name",
                "Patient number",
                "Phone",
                "Registered"
            );

            foreach (var item in report.NewPatients)
            {
                Csv(
                    builder,
                    item.FullName,
                    item.PatientNumber,
                    item.PhoneNumber,
                    DateTimeText(item.CreatedAt)
                );
            }

            var body = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = Encoding.UTF8.GetPreamble();
            var output = new byte[bom.Length + body.Length];

            Buffer.BlockCopy(bom, 0, output, 0, bom.Length);
            Buffer.BlockCopy(body, 0, output, bom.Length, body.Length);

            return output;
        }

        // =====================================================
        // XLSX
        // =====================================================

        public static byte[] BuildXlsx(
            ClinicAdminReportDataDto report
        )
        {
            var sheets = BuildWorkbook(report);

            using var memory = new MemoryStream();

            using (
                var archive = new ZipArchive(
                    memory,
                    ZipArchiveMode.Create,
                    true
                )
            )
            {
                WriteEntry(
                    archive,
                    "[Content_Types].xml",
                    ContentTypes(sheets.Count)
                );

                WriteEntry(
                    archive,
                    "_rels/.rels",
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship
                        Id="rId1"
                        Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"
                        Target="xl/workbook.xml"/>
                    </Relationships>
                    """
                );

                WriteEntry(
                    archive,
                    "xl/workbook.xml",
                    Workbook(sheets)
                );

                WriteEntry(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    WorkbookRelationships(sheets.Count)
                );

                WriteEntry(
                    archive,
                    "xl/styles.xml",
                    Styles()
                );

                for (var index = 0; index < sheets.Count; index++)
                {
                    WriteEntry(
                        archive,
                        $"xl/worksheets/sheet{index + 1}.xml",
                        Worksheet(sheets[index])
                    );
                }
            }

            return memory.ToArray();
        }

        private static List<SheetDefinition> BuildWorkbook(
            ClinicAdminReportDataDto report
        )
        {
            return new()
            {
                SummarySheet(report),
                AppointmentSheet(report),
                CollectionSheet(report),
                InventorySheet(report),
                StaffSheet(report),
                NewPatientsSheet(report)
            };
        }

        private static SheetDefinition SummarySheet(
            ClinicAdminReportDataDto report
        )
        {
            var sheet = new SheetDefinition(
                "Executive Dashboard",
                new[]
                {
                    16d, 16d, 16d, 16d, 16d,
                    16d, 18d, 18d, 18d, 18d
                }
            )
            {
                FreezeRows = 15,
                Landscape = true
            };

            sheet.Merge("A1:J2");
            sheet.Cell(1, 1, "PHILALINK CLINIC OPERATIONS REPORT", 1);
            sheet.Cell(
                3,
                1,
                $"{report.ClinicName} | Executive management dashboard",
                2
            );
            sheet.Merge("A3:J3");

            Meta(sheet, 4, 1, "Clinic", report.ClinicName, "A4:B4", "C4:D4");
            Meta(
                sheet,
                4,
                5,
                "Reporting period",
                $"{DateText(report.RangeStartUtc)} - {DateText(report.RangeEndUtc)}",
                "E4:F4",
                "G4:H4"
            );
            Meta(
                sheet,
                4,
                9,
                "Generated",
                DateTimeText(report.GeneratedAtUtc),
                "I4:I4",
                "J4:J4"
            );

            var kpis = report.Analytics.Kpis;

            Kpi(sheet, 6, 1, "ACTIVE PATIENTS", kpis.ActivePatients, "A6:B6", "A7:B8");
            Kpi(sheet, 6, 3, "ACTIVE NURSES", kpis.ActiveNurses, "C6:D6", "C7:D8");
            Kpi(sheet, 6, 5, "ACTIVE PROXIES", kpis.ActiveProxies, "E6:F6", "E7:F8");
            Kpi(sheet, 6, 7, "APPOINTMENTS TODAY", kpis.AppointmentsToday, "G6:H6", "G7:H8");
            Kpi(sheet, 6, 9, "PENDING APPOINTMENTS", kpis.PendingAppointments, "I6:J6", "I7:J8");

            Kpi(sheet, 10, 1, "COLLECTIONS DUE", kpis.CollectionsDueToday, "A10:B10", "A11:B12");
            Kpi(sheet, 10, 3, "OVERDUE COLLECTIONS", kpis.OverdueCollections, "C10:D10", "C11:D12", kpis.OverdueCollections > 0 ? 20 : 7);
            Kpi(sheet, 10, 5, "LOW STOCK ITEMS", kpis.LowStockItems, "E10:F10", "E11:F12", kpis.LowStockItems > 0 ? 20 : 7);
            Kpi(sheet, 10, 7, "COLLECTED THIS MONTH", kpis.CollectedThisMonth, "G10:H10", "G11:H12");
            Kpi(sheet, 10, 9, "NEW PATIENTS", kpis.NewPatientsThisMonth, "I10:J10", "I11:J12");

            sheet.Merge("A14:F14");
            sheet.Cell(14, 1, "MONTHLY ACTIVITY", 5);
            sheet.Merge("G14:J14");
            sheet.Cell(14, 7, "STATUS DISTRIBUTION", 5);

            var activityHeaders = new[]
            {
                "Period",
                "New patients",
                "Appointments",
                "Completed appts",
                "Collections",
                "Completed collections"
            };

            for (var col = 0; col < activityHeaders.Length; col++)
            {
                sheet.Cell(15, col + 1, activityHeaders[col], 8);
            }

            sheet.Cell(15, 7, "Appointment status", 8);
            sheet.Cell(15, 8, "Count", 8);
            sheet.Cell(15, 9, "Collection status", 8);
            sheet.Cell(15, 10, "Count", 8);

            var activity = report.Analytics.MonthlyActivity;
            var appointmentStatuses = report.Analytics.AppointmentStatuses;
            var collectionStatuses = report.Analytics.CollectionStatuses;

            var statusRowCount = Math.Max(
                appointmentStatuses.Count,
                collectionStatuses.Count
            );

            var chartRows = Math.Max(
                Math.Max(activity.Count, statusRowCount),
                1
            );

            for (var index = 0; index < chartRows; index++)
            {
                var row = 16 + index;
                var alt = index % 2 == 1;

                if (index < activity.Count)
                {
                    var item = activity[index];
                    sheet.Cell(row, 1, item.Period, alt ? 10 : 9);
                    sheet.Cell(row, 2, item.NewPatients, alt ? 12 : 11);
                    sheet.Cell(row, 3, item.Appointments, alt ? 12 : 11);
                    sheet.Cell(row, 4, item.CompletedAppointments, alt ? 12 : 11);
                    sheet.Cell(row, 5, item.Collections, alt ? 12 : 11);
                    sheet.Cell(row, 6, item.CompletedCollections, alt ? 12 : 11);
                }

                if (index < appointmentStatuses.Count)
                {
                    sheet.Cell(row, 7, appointmentStatuses[index].Status, alt ? 10 : 9);
                    sheet.Cell(row, 8, appointmentStatuses[index].Count, alt ? 12 : 11);
                }

                if (index < collectionStatuses.Count)
                {
                    sheet.Cell(row, 9, collectionStatuses[index].Status, alt ? 10 : 9);
                    sheet.Cell(row, 10, collectionStatuses[index].Count, alt ? 12 : 11);
                }
            }

            var chartStart = 16;
            var chartEnd = 15 + chartRows;

            if (activity.Count > 0)
            {
                sheet.DataBar($"B{chartStart}:B{15 + activity.Count}", Teal);
                sheet.DataBar($"C{chartStart}:C{15 + activity.Count}", DarkTeal);
                sheet.DataBar($"D{chartStart}:D{15 + activity.Count}", Green);
                sheet.DataBar($"E{chartStart}:E{15 + activity.Count}", "38BDF8");
                sheet.DataBar($"F{chartStart}:F{15 + activity.Count}", "2563EB");
            }

            if (appointmentStatuses.Count > 0)
            {
                sheet.DataBar($"H{chartStart}:H{15 + appointmentStatuses.Count}", DarkTeal);
            }

            if (collectionStatuses.Count > 0)
            {
                sheet.DataBar($"J{chartStart}:J{15 + collectionStatuses.Count}", Teal);
            }

            var lowerStart = chartEnd + 2;

            sheet.Merge($"A{lowerStart}:F{lowerStart}");
            sheet.Cell(lowerStart, 1, "LOW STOCK / INVENTORY ATTENTION", 5);
            sheet.Merge($"G{lowerStart}:J{lowerStart}");
            sheet.Cell(lowerStart, 7, "RECENT CLINIC ACTIVITY", 5);

            var lowerHeader = lowerStart + 1;

            var lowHeaders = new[]
            {
                "Medication",
                "Strength",
                "Form",
                "Quantity",
                "Reorder",
                "Status"
            };

            for (var col = 0; col < lowHeaders.Length; col++)
            {
                sheet.Cell(lowerHeader, col + 1, lowHeaders[col], 8);
            }

            sheet.Cell(lowerHeader, 7, "Action", 8);
            sheet.Cell(lowerHeader, 8, "Performed by", 8);
            sheet.Cell(lowerHeader, 9, "When", 8);
            sheet.Cell(lowerHeader, 10, "Details", 8);

            var lowStock = report.Analytics.LowStockItems.Take(8).ToList();
            var recent = report.Analytics.RecentActivity.Take(8).ToList();
            var lowerRows = Math.Max(
                Math.Max(lowStock.Count, recent.Count),
                1
            );

            for (var index = 0; index < lowerRows; index++)
            {
                var row = lowerHeader + 1 + index;
                var alt = index % 2 == 1;

                if (index < lowStock.Count)
                {
                    var item = lowStock[index];
                    sheet.Cell(row, 1, item.MedicationName, alt ? 10 : 9);
                    sheet.Cell(row, 2, item.Strength, alt ? 10 : 9);
                    sheet.Cell(row, 3, item.Form, alt ? 10 : 9);
                    sheet.Cell(row, 4, item.QuantityOnHand, 13);
                    sheet.Cell(row, 5, item.ReorderLevel, alt ? 12 : 11);
                    sheet.Cell(row, 6, "LOW STOCK", 13);
                }

                if (index < recent.Count)
                {
                    var item = recent[index];
                    sheet.Cell(row, 7, item.Action, alt ? 10 : 9);
                    sheet.Cell(row, 8, item.PerformedBy, alt ? 10 : 9);
                    sheet.Cell(row, 9, DateTimeText(item.Timestamp), alt ? 10 : 9);
                    sheet.Cell(row, 10, item.Details, alt ? 10 : 9);
                }
            }

            var noteRow = lowerHeader + lowerRows + 2;
            sheet.Merge($"A{noteRow}:J{noteRow}");
            sheet.Cell(
                noteRow,
                1,
                "PhilaLink official clinic operations report. Raw data is provided on the following worksheets for filtering, formulas and further analysis.",
                19
            );

            return sheet;
        }

        private static SheetDefinition AppointmentSheet(
            ClinicAdminReportDataDto report
        )
        {
            var sheet = DetailSheet(
                "Appointments",
                report,
                new[]
                {
                    21d, 25d, 24d, 16d, 18d, 25d
                },
                new[]
                {
                    "Scheduled",
                    "Patient",
                    "Type",
                    "Mode",
                    "Status",
                    "Nurse / Provider"
                }
            );

            var row = 6;

            foreach (var item in report.Appointments)
            {
                var alt = (row - 6) % 2 == 1;

                sheet.Cell(row, 1, DateTimeText(item.ScheduledAt), alt ? 10 : 9);
                sheet.Cell(row, 2, item.PatientName, alt ? 10 : 9);
                sheet.Cell(row, 3, item.Type, alt ? 10 : 9);
                sheet.Cell(row, 4, item.Mode, alt ? 18 : 17);
                sheet.Cell(row, 5, item.Status, StatusStyle(item.Status));
                sheet.Cell(row, 6, item.NurseName, alt ? 10 : 9);

                row++;
            }

            AddCountFooter(
                sheet,
                row,
                6,
                "Total appointments",
                report.Appointments.Count
            );

            sheet.AutoFilterRef = $"A5:F{Math.Max(row - 1, 5)}";

            return sheet;
        }

        private static SheetDefinition CollectionSheet(
            ClinicAdminReportDataDto report
        )
        {
            var sheet = DetailSheet(
                "Collections",
                report,
                new[]
                {
                    20d, 20d, 24d, 16d, 22d, 24d, 12d
                },
                new[]
                {
                    "Scheduled",
                    "Collected",
                    "Patient",
                    "Status",
                    "Proxy",
                    "Processed by",
                    "Quantity"
                }
            );

            var row = 6;

            foreach (var item in report.Collections)
            {
                var alt = (row - 6) % 2 == 1;

                sheet.Cell(row, 1, DateTimeText(item.ScheduledCollectionDate), alt ? 10 : 9);
                sheet.Cell(
                    row,
                    2,
                    item.CollectedAt.HasValue
                        ? DateTimeText(item.CollectedAt.Value)
                        : "",
                    alt ? 10 : 9
                );
                sheet.Cell(row, 3, item.PatientName, alt ? 10 : 9);
                sheet.Cell(row, 4, item.Status, StatusStyle(item.Status));
                sheet.Cell(row, 5, item.ProxyName, alt ? 10 : 9);
                sheet.Cell(row, 6, item.ProcessedByNurseName, alt ? 10 : 9);
                sheet.Cell(row, 7, item.TotalQuantity, alt ? 12 : 11);

                row++;
            }

            sheet.Cell(row, 1, "TOTAL", 15);
            sheet.Merge($"A{row}:F{row}");
            sheet.Cell(
                row,
                7,
                null,
                15,
                report.Collections.Count > 0
                    ? $"SUM(G6:G{row - 1})"
                    : null,
                report.Collections.Sum(item => item.TotalQuantity)
            );

            sheet.AutoFilterRef = $"A5:G{Math.Max(row - 1, 5)}";

            return sheet;
        }

        private static SheetDefinition InventorySheet(
            ClinicAdminReportDataDto report
        )
        {
            var sheet = DetailSheet(
                "Inventory",
                report,
                new[]
                {
                    27d, 18d, 16d, 14d, 14d, 15d, 16d, 12d
                },
                new[]
                {
                    "Medication",
                    "Strength",
                    "Form",
                    "Unit",
                    "Quantity",
                    "Reorder level",
                    "Status",
                    "Active"
                }
            );

            var row = 6;

            foreach (var item in report.Inventory)
            {
                var alt = (row - 6) % 2 == 1;

                sheet.Cell(row, 1, item.MedicationName, alt ? 10 : 9);
                sheet.Cell(row, 2, item.Strength, alt ? 10 : 9);
                sheet.Cell(row, 3, item.Form, alt ? 10 : 9);
                sheet.Cell(row, 4, item.Unit, alt ? 10 : 9);
                sheet.Cell(row, 5, item.QuantityOnHand, alt ? 12 : 11);
                sheet.Cell(row, 6, item.ReorderLevel, alt ? 12 : 11);

                var low =
                    item.IsActive &&
                    item.QuantityOnHand <= item.ReorderLevel;

                sheet.Cell(
                    row,
                    7,
                    item.Status,
                    low
                        ? 13
                        : item.IsActive
                            ? 14
                            : 16
                );

                sheet.Cell(
                    row,
                    8,
                    item.IsActive ? "Yes" : "No",
                    item.IsActive ? 14 : 16
                );

                row++;
            }

            sheet.Cell(row, 1, "TOTAL QUANTITY", 15);
            sheet.Merge($"A{row}:D{row}");
            sheet.Cell(
                row,
                5,
                null,
                15,
                report.Inventory.Count > 0
                    ? $"SUM(E6:E{row - 1})"
                    : null,
                report.Inventory.Sum(item => item.QuantityOnHand)
            );
            sheet.Cell(row, 6, "LOW STOCK ITEMS", 15);
            sheet.Cell(
                row,
                7,
                report.Inventory.Count(
                    item =>
                        item.IsActive &&
                        item.QuantityOnHand <= item.ReorderLevel
                ),
                15
            );
            sheet.Cell(row, 8, "", 15);

            sheet.AutoFilterRef = $"A5:H{Math.Max(row - 1, 5)}";

            return sheet;
        }

        private static SheetDefinition StaffSheet(
            ClinicAdminReportDataDto report
        )
        {
            var sheet = DetailSheet(
                "Staff",
                report,
                new[]
                {
                    25d, 16d, 19d, 18d, 30d, 12d, 20d
                },
                new[]
                {
                    "Full name",
                    "Role",
                    "ID number",
                    "Phone",
                    "Email",
                    "Active",
                    "Created"
                }
            );

            var row = 6;

            foreach (var item in report.Staff)
            {
                var alt = (row - 6) % 2 == 1;

                sheet.Cell(row, 1, item.FullName, alt ? 10 : 9);
                sheet.Cell(row, 2, item.Role, alt ? 10 : 9);
                sheet.Cell(row, 3, item.IdNumber, alt ? 10 : 9);
                sheet.Cell(row, 4, item.PhoneNumber, alt ? 10 : 9);
                sheet.Cell(row, 5, item.Email, alt ? 10 : 9);
                sheet.Cell(row, 6, item.IsActive ? "Yes" : "No", item.IsActive ? 14 : 16);
                sheet.Cell(row, 7, DateTimeText(item.CreatedAt), alt ? 10 : 9);

                row++;
            }

            AddCountFooter(
                sheet,
                row,
                7,
                "Total staff",
                report.Staff.Count
            );

            sheet.AutoFilterRef = $"A5:G{Math.Max(row - 1, 5)}";

            return sheet;
        }

        private static SheetDefinition NewPatientsSheet(
            ClinicAdminReportDataDto report
        )
        {
            var sheet = DetailSheet(
                "New Patients",
                report,
                new[]
                {
                    29d, 22d, 20d, 22d
                },
                new[]
                {
                    "Full name",
                    "Patient number",
                    "Phone",
                    "Registered"
                }
            );

            var row = 6;

            foreach (var item in report.NewPatients)
            {
                var alt = (row - 6) % 2 == 1;

                sheet.Cell(row, 1, item.FullName, alt ? 10 : 9);
                sheet.Cell(row, 2, item.PatientNumber, alt ? 10 : 9);
                sheet.Cell(row, 3, item.PhoneNumber, alt ? 10 : 9);
                sheet.Cell(row, 4, DateTimeText(item.CreatedAt), alt ? 10 : 9);

                row++;
            }

            AddCountFooter(
                sheet,
                row,
                4,
                "New patients in range",
                report.NewPatients.Count
            );

            sheet.AutoFilterRef = $"A5:D{Math.Max(row - 1, 5)}";

            return sheet;
        }

        private static SheetDefinition DetailSheet(
            string name,
            ClinicAdminReportDataDto report,
            IReadOnlyList<double> widths,
            IReadOnlyList<string> headers
        )
        {
            var sheet = new SheetDefinition(
                name,
                widths
            )
            {
                FreezeRows = 5,
                Landscape = true
            };

            var lastColumn = Column(headers.Count);

            sheet.Merge($"A1:{lastColumn}2");
            sheet.Cell(
                1,
                1,
                $"PHILALINK | {name.ToUpperInvariant()} REPORT",
                1
            );

            sheet.Merge($"A3:B3");
            sheet.Cell(3, 1, $"Clinic: {report.ClinicName}", 3);

            if (headers.Count >= 4)
            {
                sheet.Merge($"C3:{Column(headers.Count - 1)}3");
                sheet.Cell(
                    3,
                    3,
                    $"Period: {DateText(report.RangeStartUtc)} - {DateText(report.RangeEndUtc)}",
                    3
                );
            }

            sheet.Cell(
                3,
                headers.Count,
                $"Requested by: {report.AdminName}",
                3
            );

            for (var index = 0; index < headers.Count; index++)
            {
                sheet.Cell(
                    5,
                    index + 1,
                    headers[index],
                    8
                );
            }

            return sheet;
        }

        private static void Meta(
            SheetDefinition sheet,
            int row,
            int column,
            string label,
            string value,
            string labelRange,
            string valueRange
        )
        {
            sheet.Merge(labelRange);
            sheet.Merge(valueRange);
            sheet.Cell(row, column, label, 3);
            sheet.Cell(
                row,
                ColumnIndex(valueRange.Split(':')[0]),
                value,
                4
            );
        }

        private static void Kpi(
            SheetDefinition sheet,
            int row,
            int column,
            string label,
            int value,
            string labelRange,
            string valueRange,
            int valueStyle = 7
        )
        {
            sheet.Merge(labelRange);
            sheet.Merge(valueRange);
            sheet.Cell(row, column, label, 6);
            sheet.Cell(row + 1, column, value, valueStyle);
        }

        private static void AddCountFooter(
            SheetDefinition sheet,
            int row,
            int columnCount,
            string label,
            int count
        )
        {
            sheet.Cell(row, 1, label, 15);

            if (columnCount > 2)
            {
                sheet.Merge($"A{row}:{Column(columnCount - 1)}{row}");
            }

            sheet.Cell(row, columnCount, count, 15);
        }

        private static int StatusStyle(
            string status
        )
        {
            var normalized =
                (status ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            if (
                normalized.Contains("complete") ||
                normalized.Contains("collect") ||
                normalized.Contains("confirm")
            )
            {
                return 14;
            }

            if (
                normalized.Contains("overdue") ||
                normalized.Contains("cancel") ||
                normalized.Contains("miss")
            )
            {
                return 13;
            }

            if (
                normalized.Contains("pending") ||
                normalized.Contains("scheduled")
            )
            {
                return 16;
            }

            return 17;
        }

        private static string ContentTypes(
            int sheetCount
        )
        {
            var builder = new StringBuilder();

            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                """
            );

            for (var index = 1; index <= sheetCount; index++)
            {
                builder.Append(
                    $"""
                      <Override PartName="/xl/worksheets/sheet{index}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                    """
                );
            }

            builder.Append("</Types>");

            return builder.ToString();
        }

        private static string Workbook(
            IReadOnlyList<SheetDefinition> sheets
        )
        {
            var builder = new StringBuilder();

            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook
                  xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                  xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <bookViews>
                    <workbookView xWindow="0" yWindow="0" windowWidth="22000" windowHeight="12000"/>
                  </bookViews>
                  <sheets>
                """
            );

            for (var index = 0; index < sheets.Count; index++)
            {
                builder.Append(
                    $"""
                    <sheet
                      name="{Xml(sheets[index].Name)}"
                      sheetId="{index + 1}"
                      r:id="rId{index + 1}"/>
                    """
                );
            }

            builder.Append(
                """
                  </sheets>
                  <calcPr calcId="191029" fullCalcOnLoad="1" forceFullCalc="1"/>
                </workbook>
                """
            );

            return builder.ToString();
        }

        private static string WorkbookRelationships(
            int sheetCount
        )
        {
            var builder = new StringBuilder();

            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                """
            );

            for (var index = 1; index <= sheetCount; index++)
            {
                builder.Append(
                    $"""
                    <Relationship
                      Id="rId{index}"
                      Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"
                      Target="worksheets/sheet{index}.xml"/>
                    """
                );
            }

            builder.Append(
                $"""
                <Relationship
                  Id="rId{sheetCount + 1}"
                  Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"
                  Target="styles.xml"/>
                </Relationships>
                """
            );

            return builder.ToString();
        }

        private static string Styles()
        {
            return
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="9">
                    <font><sz val="10"/><color rgb="FF334155"/><name val="Calibri"/></font>
                    <font><b/><sz val="20"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
                    <font><b/><sz val="10"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
                    <font><b/><sz val="10"/><color rgb="FF0F172A"/><name val="Calibri"/></font>
                    <font><sz val="10"/><color rgb="FF475569"/><name val="Calibri"/></font>
                    <font><b/><sz val="22"/><color rgb="FF0F172A"/><name val="Calibri"/></font>
                    <font><b/><sz val="10"/><color rgb="FF92400E"/><name val="Calibri"/></font>
                    <font><b/><sz val="10"/><color rgb="FF166534"/><name val="Calibri"/></font>
                    <font><i/><sz val="9"/><color rgb="FF64748B"/><name val="Calibri"/></font>
                  </fonts>
                  <fills count="11">
                    <fill><patternFill patternType="none"/></fill>
                    <fill><patternFill patternType="gray125"/></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF0F766E"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFCCFBF1"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFFFFFFF"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFF0FDFA"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFFEF3C7"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFDCFCE7"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFE2E8F0"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FF0F172A"/><bgColor indexed="64"/></patternFill></fill>
                    <fill><patternFill patternType="solid"><fgColor rgb="FFF8FAFC"/><bgColor indexed="64"/></patternFill></fill>
                  </fills>
                  <borders count="2">
                    <border/>
                    <border>
                      <left style="thin"><color rgb="FFE2E8F0"/></left>
                      <right style="thin"><color rgb="FFE2E8F0"/></right>
                      <top style="thin"><color rgb="FFE2E8F0"/></top>
                      <bottom style="thin"><color rgb="FFE2E8F0"/></bottom>
                      <diagonal/>
                    </border>
                  </borders>
                  <cellStyleXfs count="1">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
                  </cellStyleXfs>
                  <cellXfs count="21">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                    <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="3" fillId="3" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center"/></xf>
                    <xf numFmtId="0" fontId="4" fillId="4" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="9" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>
                    <xf numFmtId="0" fontId="3" fillId="3" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="5" fillId="3" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="4" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="5" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="4" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="5" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
                    <xf numFmtId="0" fontId="6" fillId="6" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="7" fillId="7" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="9" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
                    <xf numFmtId="0" fontId="3" fillId="8" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="4" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="5" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="8" fillId="10" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="5" fillId="6" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
                  </cellXfs>
                  <cellStyles count="1">
                    <cellStyle name="Normal" xfId="0" builtinId="0"/>
                  </cellStyles>
                </styleSheet>
                """;
        }

        private static string Worksheet(
            SheetDefinition sheet
        )
        {
            var builder = new StringBuilder();
            var maxRow = Math.Max(sheet.Rows.Keys.DefaultIfEmpty(1).Max(), 1);
            var maxCol = Math.Max(
                sheet.Rows.Values
                    .SelectMany(row => row.Cells)
                    .Select(cell => cell.Column)
                    .DefaultIfEmpty(sheet.ColumnWidths.Count)
                    .Max(),
                sheet.ColumnWidths.Count
            );

            builder.Append(
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                """
            );

            builder.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
            builder.Append(
                $"<dimension ref=\"A1:{Column(maxCol)}{maxRow}\"/>"
            );

            builder.Append("<sheetViews><sheetView workbookViewId=\"0\">");

            if (sheet.FreezeRows > 0)
            {
                builder.Append(
                    $"<pane ySplit=\"{sheet.FreezeRows}\" topLeftCell=\"A{sheet.FreezeRows + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/>"
                );
            }

            builder.Append("</sheetView></sheetViews>");
            builder.Append("<sheetFormatPr defaultRowHeight=\"18\"/>");

            builder.Append("<cols>");

            for (var index = 0; index < sheet.ColumnWidths.Count; index++)
            {
                builder.Append(
                    $"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{sheet.ColumnWidths[index].ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>"
                );
            }

            builder.Append("</cols>");
            builder.Append("<sheetData>");

            foreach (var row in sheet.Rows.OrderBy(item => item.Key))
            {
                builder.Append(
                    $"<row r=\"{row.Key}\""
                );

                if (row.Value.Height.HasValue)
                {
                    builder.Append(
                        $" ht=\"{row.Value.Height.Value.ToString(CultureInfo.InvariantCulture)}\" customHeight=\"1\""
                    );
                }

                builder.Append(">");

                foreach (var cell in row.Value.Cells.OrderBy(item => item.Column))
                {
                    Cell(
                        builder,
                        $"{Column(cell.Column)}{row.Key}",
                        cell
                    );
                }

                builder.Append("</row>");
            }

            builder.Append("</sheetData>");

            if (!string.IsNullOrWhiteSpace(sheet.AutoFilterRef))
            {
                builder.Append(
                    $"<autoFilter ref=\"{Xml(sheet.AutoFilterRef!)}\"/>"
                );
            }

            if (sheet.MergeCells.Count > 0)
            {
                builder.Append(
                    $"<mergeCells count=\"{sheet.MergeCells.Count}\">"
                );

                foreach (var merge in sheet.MergeCells)
                {
                    builder.Append(
                        $"<mergeCell ref=\"{Xml(merge)}\"/>"
                    );
                }

                builder.Append("</mergeCells>");
            }

            var priority = 1;

            foreach (var dataBar in sheet.DataBars)
            {
                builder.Append(
                    $"""
                    <conditionalFormatting sqref="{Xml(dataBar.Range)}">
                      <cfRule type="dataBar" priority="{priority++}">
                        <dataBar showValue="1">
                          <cfvo type="min"/>
                          <cfvo type="max"/>
                          <color rgb="FF{dataBar.Color}"/>
                        </dataBar>
                      </cfRule>
                    </conditionalFormatting>
                    """
                );
            }

            builder.Append(
                """
                <pageMargins left="0.25" right="0.25" top="0.45" bottom="0.45" header="0.2" footer="0.2"/>
                """
            );

            builder.Append(
                sheet.Landscape
                    ? """
                      <pageSetup paperSize="9" orientation="landscape" fitToWidth="1" fitToHeight="0"/>
                      """
                    : """
                      <pageSetup paperSize="9" orientation="portrait" fitToWidth="1" fitToHeight="0"/>
                      """
            );

            builder.Append("</worksheet>");

            return builder.ToString();
        }

        private static void Cell(
            StringBuilder builder,
            string reference,
            CellDefinition cell
        )
        {
            builder.Append(
                $"<c r=\"{reference}\" s=\"{cell.Style}\""
            );

            if (
                cell.Value is string ||
                cell.Value is DateTime ||
                cell.Value is bool
            )
            {
                builder.Append(" t=\"inlineStr\"");
            }

            builder.Append(">");

            if (!string.IsNullOrWhiteSpace(cell.Formula))
            {
                builder.Append(
                    $"<f>{Xml(cell.Formula!)}</f>"
                );

                builder.Append(
                    $"<v>{Convert.ToString(cell.CachedValue ?? 0, CultureInfo.InvariantCulture)}</v>"
                );

                builder.Append("</c>");

                return;
            }

            if (cell.Value == null)
            {
                builder.Append("</c>");
                return;
            }

            if (IsNumber(cell.Value))
            {
                builder.Append(
                    $"<v>{Convert.ToString(cell.Value, CultureInfo.InvariantCulture)}</v>"
                );

                builder.Append("</c>");

                return;
            }

            builder.Append(
                $"<is><t xml:space=\"preserve\">{Xml(ValueText(cell.Value))}</t></is></c>"
            );
        }

        // =====================================================
        // PDF
        // =====================================================

        public static byte[] BuildPdf(
            ClinicAdminReportDataDto report
        )
        {
            var pages = BuildPdfPages(report);

            for (var index = 0; index < pages.Count; index++)
            {
                pages[index].Footer(
                    report,
                    index + 1,
                    pages.Count
                );
            }

            return PdfDocument(pages);
        }

        private static List<PdfCanvas> BuildPdfPages(
            ClinicAdminReportDataDto report
        )
        {
            var pages = new List<PdfCanvas>
            {
                DashboardPage(report)
            };

            AddTablePages(
                pages,
                report,
                "Appointments",
                new[]
                {
                    "Scheduled",
                    "Patient",
                    "Type",
                    "Mode",
                    "Status",
                    "Provider"
                },
                new[]
                {
                    92d, 135d, 135d, 72d, 85d, 145d
                },
                report.Appointments.Select(
                    item => new[]
                    {
                        DateTimeText(item.ScheduledAt),
                        item.PatientName,
                        item.Type,
                        item.Mode,
                        item.Status,
                        item.NurseName
                    }
                ).ToList()
            );

            AddTablePages(
                pages,
                report,
                "Medication Collections",
                new[]
                {
                    "Scheduled",
                    "Collected",
                    "Patient",
                    "Status",
                    "Proxy",
                    "Processed by",
                    "Qty"
                },
                new[]
                {
                    86d, 86d, 125d, 80d, 105d, 125d, 55d
                },
                report.Collections.Select(
                    item => new[]
                    {
                        DateTimeText(item.ScheduledCollectionDate),
                        item.CollectedAt.HasValue
                            ? DateTimeText(item.CollectedAt.Value)
                            : "-",
                        item.PatientName,
                        item.Status,
                        item.ProxyName,
                        item.ProcessedByNurseName,
                        item.TotalQuantity.ToString(CultureInfo.InvariantCulture)
                    }
                ).ToList(),
                $"Total quantity: {report.Collections.Sum(item => item.TotalQuantity)}"
            );

            AddTablePages(
                pages,
                report,
                "Medication Inventory",
                new[]
                {
                    "Medication",
                    "Strength",
                    "Form",
                    "Unit",
                    "Qty",
                    "Reorder",
                    "Status",
                    "Active"
                },
                new[]
                {
                    165d, 82d, 72d, 70d, 58d, 65d, 95d, 55d
                },
                report.Inventory.Select(
                    item => new[]
                    {
                        item.MedicationName,
                        item.Strength,
                        item.Form,
                        item.Unit,
                        item.QuantityOnHand.ToString(CultureInfo.InvariantCulture),
                        item.ReorderLevel.ToString(CultureInfo.InvariantCulture),
                        item.Status,
                        item.IsActive ? "Yes" : "No"
                    }
                ).ToList(),
                $"Total stock units: {report.Inventory.Sum(item => item.QuantityOnHand)}"
            );

            AddTablePages(
                pages,
                report,
                "Clinic Staff",
                new[]
                {
                    "Full name",
                    "Role",
                    "ID number",
                    "Phone",
                    "Email",
                    "Active",
                    "Created"
                },
                new[]
                {
                    140d, 80d, 105d, 100d, 155d, 60d, 90d
                },
                report.Staff.Select(
                    item => new[]
                    {
                        item.FullName,
                        item.Role,
                        item.IdNumber,
                        item.PhoneNumber,
                        item.Email,
                        item.IsActive ? "Yes" : "No",
                        DateText(item.CreatedAt)
                    }
                ).ToList(),
                $"Total staff: {report.Staff.Count}"
            );

            AddTablePages(
                pages,
                report,
                "New Patients",
                new[]
                {
                    "Full name",
                    "Patient number",
                    "Phone",
                    "Registered"
                },
                new[]
                {
                    220d, 170d, 160d, 150d
                },
                report.NewPatients.Select(
                    item => new[]
                    {
                        item.FullName,
                        item.PatientNumber,
                        item.PhoneNumber,
                        DateTimeText(item.CreatedAt)
                    }
                ).ToList(),
                $"New patients in range: {report.NewPatients.Count}"
            );

            return pages;
        }

        private static PdfCanvas DashboardPage(
            ClinicAdminReportDataDto report
        )
        {
            var page = new PdfCanvas();
            var kpis = report.Analytics.Kpis;

            page.FillRect(0, 0, 842, 76, DarkTeal);
            page.Text(
                28,
                22,
                20,
                "PHILALINK CLINIC OPERATIONS REPORT",
                true,
                White
            );
            page.Text(
                28,
                48,
                9,
                $"{report.ClinicName} | Official management report",
                false,
                "D1FAE5"
            );

            page.Text(
                595,
                21,
                8,
                $"Requested by: {report.AdminName}",
                true,
                White
            );
            page.Text(
                595,
                38,
                8,
                $"Period: {DateText(report.RangeStartUtc)} - {DateText(report.RangeEndUtc)}",
                false,
                White
            );
            page.Text(
                595,
                54,
                8,
                $"Generated: {DateTimeText(report.GeneratedAtUtc)} UTC",
                false,
                White
            );

            var cards = new[]
            {
                new PdfKpi("Active patients", kpis.ActivePatients, PaleTeal, DarkTeal),
                new PdfKpi("Appointments today", kpis.AppointmentsToday, "DBEAFE", "1D4ED8"),
                new PdfKpi("Collections due", kpis.CollectionsDueToday, "E0F2FE", "0369A1"),
                new PdfKpi("Low stock", kpis.LowStockItems, PaleAmber, "92400E"),
                new PdfKpi("New patients", kpis.NewPatientsThisMonth, PaleGreen, "166534")
            };

            var cardX = 28d;
            const double cardY = 92;
            const double cardWidth = 146;
            const double cardHeight = 66;
            const double gap = 12;

            foreach (var card in cards)
            {
                page.RoundRect(
                    cardX,
                    cardY,
                    cardWidth,
                    cardHeight,
                    8,
                    card.Fill,
                    LightSlate
                );
                page.Text(cardX + 12, cardY + 12, 8, card.Label.ToUpperInvariant(), true, MidSlate);
                page.Text(cardX + 12, cardY + 31, 22, card.Value.ToString(CultureInfo.InvariantCulture), true, Slate);
                page.FillRect(cardX + 12, cardY + 54, 34, 3, card.Accent);

                cardX += cardWidth + gap;
            }

            page.Panel(28, 176, 500, 190, "Monthly activity");
            DrawMonthlyActivity(page, report, 44, 210, 468, 132);

            page.Panel(544, 176, 270, 190, "Status distribution");
            DrawStatusDistribution(page, report, 560, 210, 238, 132);

            page.Panel(28, 382, 500, 150, "Low stock / inventory attention");
            DrawLowStock(page, report, 44, 413, 468, 102);

            page.Panel(544, 382, 270, 150, "Report scope");
            page.Text(560, 414, 9, "Active nurses", false, MidSlate);
            page.Text(777, 414, 9, kpis.ActiveNurses.ToString(CultureInfo.InvariantCulture), true, Slate, rightAlign: true);
            page.Text(560, 436, 9, "Active proxies", false, MidSlate);
            page.Text(777, 436, 9, kpis.ActiveProxies.ToString(CultureInfo.InvariantCulture), true, Slate, rightAlign: true);
            page.Text(560, 458, 9, "Pending appointments", false, MidSlate);
            page.Text(777, 458, 9, kpis.PendingAppointments.ToString(CultureInfo.InvariantCulture), true, Slate, rightAlign: true);
            page.Text(560, 480, 9, "Overdue collections", false, MidSlate);
            page.Text(777, 480, 9, kpis.OverdueCollections.ToString(CultureInfo.InvariantCulture), true, kpis.OverdueCollections > 0 ? Red : Slate, rightAlign: true);
            page.Text(560, 502, 9, "Collected this month", false, MidSlate);
            page.Text(777, 502, 9, kpis.CollectedThisMonth.ToString(CultureInfo.InvariantCulture), true, Slate, rightAlign: true);

            return page;
        }

        private static void DrawMonthlyActivity(
            PdfCanvas page,
            ClinicAdminReportDataDto report,
            double x,
            double y,
            double width,
            double height
        )
        {
            var data = report.Analytics.MonthlyActivity.TakeLast(8).ToList();

            if (data.Count == 0)
            {
                page.Text(x, y + 44, 9, "No monthly activity data in this reporting period.", false, MidSlate);
                return;
            }

            var maxValue = Math.Max(
                1,
                data.Max(
                    item => Math.Max(
                        Math.Max(item.NewPatients, item.Appointments),
                        item.Collections
                    )
                )
            );

            var chartTop = y + 8;
            var chartBottom = y + height - 22;
            var chartHeight = chartBottom - chartTop;

            page.Line(x + 26, chartTop, x + 26, chartBottom, LightSlate, 0.8);
            page.Line(x + 26, chartBottom, x + width, chartBottom, LightSlate, 0.8);

            var slot = (width - 36) / data.Count;
            var barWidth = Math.Min(9d, slot / 4.5);

            for (var index = 0; index < data.Count; index++)
            {
                var item = data[index];
                var center = x + 30 + slot * index + slot / 2;

                DrawBar(page, center - barWidth * 1.6, chartBottom, barWidth, chartHeight, item.Appointments, maxValue, DarkTeal);
                DrawBar(page, center - barWidth * 0.45, chartBottom, barWidth, chartHeight, item.Collections, maxValue, Teal);
                DrawBar(page, center + barWidth * 0.7, chartBottom, barWidth, chartHeight, item.NewPatients, maxValue, "2563EB");

                page.Text(center - slot / 2 + 2, chartBottom + 5, 6.5, Truncate(item.Period, 10), false, MidSlate);
            }

            page.FillRect(x + 290, y - 2, 7, 7, DarkTeal);
            page.Text(x + 301, y - 3, 7, "Appointments", false, MidSlate);
            page.FillRect(x + 357, y - 2, 7, 7, Teal);
            page.Text(x + 368, y - 3, 7, "Collections", false, MidSlate);
            page.FillRect(x + 419, y - 2, 7, 7, "2563EB");
            page.Text(x + 430, y - 3, 7, "New patients", false, MidSlate);
        }

        private static void DrawBar(
            PdfCanvas page,
            double x,
            double bottom,
            double width,
            double chartHeight,
            int value,
            int maxValue,
            string color
        )
        {
            var height = chartHeight * value / Math.Max(maxValue, 1);

            page.FillRect(
                x,
                bottom - height,
                width,
                Math.Max(height, 1),
                color
            );
        }

        private static void DrawStatusDistribution(
            PdfCanvas page,
            ClinicAdminReportDataDto report,
            double x,
            double y,
            double width,
            double height
        )
        {
            var appointment = report.Analytics.AppointmentStatuses.Take(4).ToList();
            var collection = report.Analytics.CollectionStatuses.Take(4).ToList();

            page.Text(x, y, 8, "Appointments", true, Slate);
            DrawHorizontalStatusBars(page, appointment, x, y + 17, width, 48, DarkTeal);

            page.Text(x, y + 73, 8, "Collections", true, Slate);
            DrawHorizontalStatusBars(page, collection, x, y + 90, width, 48, Teal);
        }

        private static void DrawHorizontalStatusBars(
            PdfCanvas page,
            IReadOnlyList<ClinicAdminStatusCountDto> rows,
            double x,
            double y,
            double width,
            double height,
            string color
        )
        {
            if (rows.Count == 0)
            {
                page.Text(x, y + 14, 8, "No data", false, MidSlate);
                return;
            }

            var max = Math.Max(1, rows.Max(item => item.Count));
            var rowHeight = height / rows.Count;

            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var top = y + index * rowHeight;

                page.Text(x, top + 1, 7, Truncate(item.Status, 18), false, MidSlate);

                var barX = x + 91;
                var barWidth = width - 118;
                var valueWidth = barWidth * item.Count / max;

                page.FillRect(barX, top + 2, barWidth, 6, VeryLightSlate);
                page.FillRect(barX, top + 2, Math.Max(valueWidth, 2), 6, color);
                page.Text(x + width - 2, top + 1, 7, item.Count.ToString(CultureInfo.InvariantCulture), true, Slate, rightAlign: true);
            }
        }

        private static void DrawLowStock(
            PdfCanvas page,
            ClinicAdminReportDataDto report,
            double x,
            double y,
            double width,
            double height
        )
        {
            var rows = report.Analytics.LowStockItems.Take(5).ToList();

            if (rows.Count == 0)
            {
                page.RoundRect(x, y + 8, width, 48, 6, PaleGreen, "BBF7D0");
                page.Text(x + 14, y + 22, 10, "Inventory healthy - no low-stock alerts.", true, "166534");
                return;
            }

            page.Text(x, y, 7.5, "Medication", true, MidSlate);
            page.Text(x + 260, y, 7.5, "On hand", true, MidSlate);
            page.Text(x + 342, y, 7.5, "Reorder", true, MidSlate);
            page.Text(x + width, y, 7.5, "Status", true, MidSlate, rightAlign: true);

            var rowY = y + 18;

            foreach (var item in rows)
            {
                page.Line(x, rowY - 5, x + width, rowY - 5, LightSlate, 0.5);
                page.Text(x, rowY, 8, Truncate($"{item.MedicationName} {item.Strength}", 40), false, Slate);
                page.Text(x + 260, rowY, 8, $"{item.QuantityOnHand} {item.Unit}", true, Red);
                page.Text(x + 342, rowY, 8, item.ReorderLevel.ToString(CultureInfo.InvariantCulture), false, MidSlate);
                page.Text(x + width, rowY, 7.5, "LOW STOCK", true, "92400E", rightAlign: true);
                rowY += 18;
            }
        }

        private static void AddTablePages(
            ICollection<PdfCanvas> pages,
            ClinicAdminReportDataDto report,
            string title,
            IReadOnlyList<string> headers,
            IReadOnlyList<double> widths,
            IReadOnlyList<string[]> rows,
            string? summary = null
        )
        {
            const int rowsPerPage = 20;
            var chunks = rows
                .Chunk(rowsPerPage)
                .Select(chunk => chunk.ToList())
                .ToList();

            if (chunks.Count == 0)
            {
                chunks.Add(new List<string[]>());
            }

            for (var pageIndex = 0; pageIndex < chunks.Count; pageIndex++)
            {
                var page = new PdfCanvas();

                page.FillRect(0, 0, 842, 54, DarkTeal);
                page.Text(28, 17, 16, $"PHILALINK | {title.ToUpperInvariant()}", true, White);
                page.Text(
                    28,
                    36,
                    7.5,
                    $"{report.ClinicName} | {DateText(report.RangeStartUtc)} - {DateText(report.RangeEndUtc)}",
                    false,
                    "D1FAE5"
                );
                page.Text(
                    810,
                    35,
                    7.5,
                    $"Requested by: {report.AdminName}",
                    false,
                    White,
                    rightAlign: true
                );

                var tableX = 28d;
                var tableY = 80d;
                const double headerHeight = 24;
                const double rowHeight = 20;

                var totalWidth = widths.Sum();
                var available = 786d;
                var scale = available / totalWidth;
                var actualWidths = widths.Select(value => value * scale).ToArray();

                var x = tableX;

                for (var column = 0; column < headers.Count; column++)
                {
                    page.FillRect(x, tableY, actualWidths[column], headerHeight, DarkTeal);
                    page.Text(x + 5, tableY + 7, 7.2, Truncate(headers[column], 24), true, White);
                    x += actualWidths[column];
                }

                var y = tableY + headerHeight;

                foreach (var row in chunks[pageIndex])
                {
                    x = tableX;
                    var alt = ((int)((y - tableY - headerHeight) / rowHeight)) % 2 == 1;
                    var fill = alt ? LightTeal : White;

                    for (var column = 0; column < headers.Count; column++)
                    {
                        page.FillRect(x, y, actualWidths[column], rowHeight, fill);
                        page.StrokeRect(x, y, actualWidths[column], rowHeight, LightSlate, 0.45);

                        var value = column < row.Length ? row[column] : string.Empty;
                        page.Text(
                            x + 5,
                            y + 6,
                            7.1,
                            Truncate(
                                value,
                                Math.Max(8, (int)(actualWidths[column] / 5.5))
                            ),
                            false,
                            Slate
                        );

                        x += actualWidths[column];
                    }

                    y += rowHeight;
                }

                if (chunks[pageIndex].Count == 0)
                {
                    page.Text(
                        tableX,
                        tableY + 48,
                        10,
                        "No records in this reporting period.",
                        false,
                        MidSlate
                    );
                }

                if (
                    pageIndex == chunks.Count - 1 &&
                    !string.IsNullOrWhiteSpace(summary)
                )
                {
                    page.RoundRect(28, 522, 260, 28, 6, PaleTeal, "99F6E4");
                    page.Text(40, 531, 8, summary!, true, DarkTeal);
                }

                pages.Add(page);
            }
        }

        private static byte[] PdfDocument(
            IReadOnlyList<PdfCanvas> pages
        )
        {
            using var stream = new MemoryStream();

            var offsets = new List<long>
            {
                0
            };

            void WriteAscii(string value)
            {
                var bytes = Encoding.ASCII.GetBytes(value);
                stream.Write(bytes, 0, bytes.Length);
            }

            WriteAscii("%PDF-1.4\n");

            var objectCount = 4 + pages.Count * 2;
            var pageIds = Enumerable
                .Range(0, pages.Count)
                .Select(index => 5 + index * 2)
                .ToList();

            var objects = new Dictionary<int, string>
            {
                [1] = "<< /Type /Catalog /Pages 2 0 R >>",
                [2] = $"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pages.Count} >>",
                [3] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                [4] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
            };

            for (var index = 0; index < pages.Count; index++)
            {
                var pageId = 5 + index * 2;
                var contentId = pageId + 1;
                var content = pages[index].Content;

                objects[pageId] =
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>";

                objects[contentId] =
                    $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream";
            }

            for (var id = 1; id <= objectCount; id++)
            {
                offsets.Add(stream.Position);
                WriteAscii($"{id} 0 obj\n{objects[id]}\nendobj\n");
            }

            var xref = stream.Position;

            WriteAscii($"xref\n0 {objectCount + 1}\n");
            WriteAscii("0000000000 65535 f \n");

            for (var id = 1; id <= objectCount; id++)
            {
                WriteAscii($"{offsets[id]:D10} 00000 n \n");
            }

            WriteAscii(
                $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"
            );

            return stream.ToArray();
        }

        // =====================================================
        // COMMON
        // =====================================================

        private static void Csv(
            StringBuilder builder,
            params object?[] values
        )
        {
            builder.AppendLine(
                string.Join(
                    ",",
                    values.Select(CsvValue)
                )
            );
        }

        private static string CsvValue(
            object? value
        )
        {
            var text = ValueText(value)
                .Replace("\"", "\"\"");

            return $"\"{text}\"";
        }

        private static string ValueText(
            object? value
        )
        {
            return value switch
            {
                null => "",
                DateTime date => DateTimeText(date),
                bool boolean => boolean ? "Yes" : "No",
                IFormattable formatter =>
                    formatter.ToString(
                        null,
                        CultureInfo.InvariantCulture
                    ) ?? "",
                _ => value.ToString() ?? ""
            };
        }

        private static bool IsNumber(
            object value
        )
        {
            return value is byte ||
                   value is short ||
                   value is int ||
                   value is long ||
                   value is float ||
                   value is double ||
                   value is decimal;
        }

        private static string Xml(
            string value
        )
        {
            return SecurityElement.Escape(value) ?? "";
        }

        private static string DateText(
            DateTime value
        )
        {
            return value.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            );
        }

        private static string DateTimeText(
            DateTime value
        )
        {
            return value.ToString(
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture
            );
        }

        private static string Truncate(
            string? value,
            int maximum
        )
        {
            var text = value ?? string.Empty;

            if (text.Length <= maximum)
            {
                return text;
            }

            if (maximum <= 3)
            {
                return text[..maximum];
            }

            return $"{text[..(maximum - 3)]}...";
        }

        private static string Column(
            int index
        )
        {
            var value = string.Empty;

            while (index > 0)
            {
                index--;

                value =
                    (char)('A' + index % 26) +
                    value;

                index /= 26;
            }

            return value;
        }

        private static int ColumnIndex(
            string reference
        )
        {
            var letters = new string(
                reference
                    .TakeWhile(char.IsLetter)
                    .ToArray()
            );

            var value = 0;

            foreach (var letter in letters)
            {
                value *= 26;
                value += char.ToUpperInvariant(letter) - 'A' + 1;
            }

            return value;
        }

        private static void WriteEntry(
            ZipArchive archive,
            string name,
            string content
        )
        {
            var entry = archive.CreateEntry(
                name,
                CompressionLevel.Fastest
            );

            using var stream = entry.Open();
            using var writer = new StreamWriter(
                stream,
                new UTF8Encoding(false)
            );

            writer.Write(content);
        }

        private sealed record CellDefinition(
            int Column,
            object? Value,
            int Style,
            string? Formula = null,
            object? CachedValue = null
        );

        private sealed class RowDefinition
        {
            public double? Height { get; set; }

            public List<CellDefinition> Cells { get; } =
                new();
        }

        private sealed record ConditionalDataBar(
            string Range,
            string Color
        );

        private sealed class SheetDefinition
        {
            public SheetDefinition(
                string name,
                IEnumerable<double> widths
            )
            {
                Name = name;
                ColumnWidths = widths.ToList();
            }

            public string Name { get; }

            public List<double> ColumnWidths { get; }

            public Dictionary<int, RowDefinition> Rows { get; } =
                new();

            public List<string> MergeCells { get; } =
                new();

            public List<ConditionalDataBar> DataBars { get; } =
                new();

            public int FreezeRows { get; set; }

            public string? AutoFilterRef { get; set; }

            public bool Landscape { get; set; }

            public void Merge(string range)
            {
                var parts = range.Split(':');

                if (
                    parts.Length == 2 &&
                    string.Equals(
                        parts[0],
                        parts[1],
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return;
                }

                if (!MergeCells.Contains(range))
                {
                    MergeCells.Add(range);
                }
            }

            public void DataBar(
                string range,
                string color
            )
            {
                DataBars.Add(
                    new ConditionalDataBar(
                        range,
                        color
                    )
                );
            }

            public void Cell(
                int row,
                int column,
                object? value,
                int style = 0,
                string? formula = null,
                object? cachedValue = null
            )
            {
                if (!Rows.TryGetValue(row, out var rowDefinition))
                {
                    rowDefinition = new RowDefinition();
                    Rows[row] = rowDefinition;
                }

                rowDefinition.Cells.RemoveAll(
                    item => item.Column == column
                );

                rowDefinition.Cells.Add(
                    new CellDefinition(
                        column,
                        value,
                        style,
                        formula,
                        cachedValue
                    )
                );
            }
        }

        private sealed record PdfKpi(
            string Label,
            int Value,
            string Fill,
            string Accent
        );

        private sealed class PdfCanvas
        {
            private const double Width = 842;
            private const double Height = 595;

            private readonly StringBuilder _builder =
                new();

            public string Content => _builder.ToString();

            public void Panel(
                double x,
                double y,
                double width,
                double height,
                string title
            )
            {
                RoundRect(
                    x,
                    y,
                    width,
                    height,
                    8,
                    White,
                    LightSlate
                );

                Text(
                    x + 16,
                    y + 14,
                    10,
                    title,
                    true,
                    Slate
                );

                Line(
                    x + 16,
                    y + 35,
                    x + width - 16,
                    y + 35,
                    LightSlate,
                    0.6
                );
            }

            public void Footer(
                ClinicAdminReportDataDto report,
                int pageNumber,
                int pageCount
            )
            {
                Line(
                    28,
                    564,
                    814,
                    564,
                    LightSlate,
                    0.5
                );

                Text(
                    28,
                    571,
                    6.8,
                    $"PhilaLink | Confidential clinic operations report | {report.ClinicName}",
                    false,
                    MidSlate
                );

                Text(
                    814,
                    571,
                    6.8,
                    $"Page {pageNumber} of {pageCount}",
                    true,
                    MidSlate,
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
                var rgb = Rgb(color);
                var pdfY = Height - y - height;

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
                var rgb = Rgb(color);
                var pdfY = Height - y - height;

                _builder.AppendLine(
                    $"q {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} RG {N(lineWidth)} w {N(x)} {N(pdfY)} {N(width)} {N(height)} re S Q"
                );
            }

            public void RoundRect(
                double x,
                double y,
                double width,
                double height,
                double radius,
                string fill,
                string stroke
            )
            {
                // PDF has no native rounded rectangle operator.
                // A standard rectangle with soft brand fills keeps the report dependency-free.
                FillRect(x, y, width, height, fill);
                StrokeRect(x, y, width, height, stroke, 0.65);
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
                var rgb = Rgb(color);
                var py1 = Height - y1;
                var py2 = Height - y2;

                _builder.AppendLine(
                    $"q {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} RG {N(lineWidth)} w {N(x1)} {N(py1)} m {N(x2)} {N(py2)} l S Q"
                );
            }

            public void Text(
                double x,
                double y,
                double size,
                string? text,
                bool bold,
                string color,
                bool rightAlign = false
            )
            {
                var safe = PdfText(text ?? string.Empty);
                var rgb = Rgb(color);
                var width = EstimateTextWidth(safe, size, bold);
                var drawX = rightAlign
                    ? x - width
                    : x;
                var baseline = Height - y - size;

                _builder.AppendLine(
                    $"BT /{(bold ? "F2" : "F1")} {N(size)} Tf {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} rg {N(drawX)} {N(baseline)} Td ({safe}) Tj ET"
                );
            }

            private static string PdfText(
                string value
            )
            {
                var builder = new StringBuilder(value.Length);

                foreach (var character in value)
                {
                    var safe = character <= 127
                        ? character
                        : '-';

                    if (
                        safe == '\\' ||
                        safe == '(' ||
                        safe == ')'
                    )
                    {
                        builder.Append('\\');
                    }

                    builder.Append(safe);
                }

                return builder.ToString();
            }

            private static double EstimateTextWidth(
                string text,
                double size,
                bool bold
            )
            {
                return text.Length *
                       size *
                       (bold ? 0.56 : 0.51);
            }

            private static (double R, double G, double B) Rgb(
                string hex
            )
            {
                var normalized = hex.Trim().TrimStart('#');

                if (normalized.Length != 6)
                {
                    normalized = "000000";
                }

                return (
                    Convert.ToInt32(normalized[..2], 16) / 255d,
                    Convert.ToInt32(normalized.Substring(2, 2), 16) / 255d,
                    Convert.ToInt32(normalized.Substring(4, 2), 16) / 255d
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
    }
}
