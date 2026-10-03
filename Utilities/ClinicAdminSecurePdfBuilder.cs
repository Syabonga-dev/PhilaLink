using PersonalProject.Models.DTOs;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PersonalProject.Utilities
{
    /// <summary>
    /// Builds the official Clinic Administrator PDF report and
    /// applies PDF 1.4 Standard Security Handler encryption.
    /// The user-supplied password exists only for the current
    /// export request and is never persisted by this builder.
    /// </summary>
    public static class ClinicAdminSecurePdfBuilder
    {
        private const double PageWidth =
            842;

        private const double PageHeight =
            595;

        public static byte[] Build(
            ClinicAdminDynamicReportPreviewDto report,
            string password,
            byte[]? logoJpeg = null
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    password
                ) ||
                password.Length <
                8
            )
            {
                throw new ArgumentException(
                    "A PDF password of at least 8 characters is required.",
                    nameof(password)
                );
            }

            var pages =
                BuildPages(
                    report,
                    logoJpeg is
                    {
                        Length:
                            > 0
                    }
                );

            return EncryptedPdfDocument
                .Build(
                    pages,
                    password,
                    logoJpeg
                );
        }

        private static List<string>
            BuildPages(
                ClinicAdminDynamicReportPreviewDto report,
                bool hasLogo
            )
        {
            var pages =
                new List<string>();

            var first =
                new PdfCanvas();

            first.BrandHeader(
                report.Title,
                report.ClinicName,
                hasLogo
            );

            first.Text(
                32,
                86,
                8,
                "CONFIDENTIAL CLINIC REPORT",
                true,
                "#0F766E"
            );

            first.Text(
                32,
                104,
                8,
                RangeText(
                    report
                ),
                false,
                "#475569"
            );

            first.Text(
                32,
                118,
                8,
                $"Requested by: {report.RequestedBy}",
                false,
                "#475569"
            );

            first.Text(
                810,
                118,
                8,
                $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC",
                false,
                "#475569",
                rightAlign:
                    true
            );

            var summary =
                report.Summary
                    .Take(
                        4
                    )
                    .ToList();

            const double gap =
                10;

            var cardWidth =
                (
                    778d -
                    gap *
                    3
                ) /
                4d;

            for (
                var index = 0;
                index < 4;
                index++
            )
            {
                var x =
                    32 +
                    index *
                    (
                        cardWidth +
                        gap
                    );

                var item =
                    index <
                    summary.Count
                        ? summary[
                            index
                        ]
                        : null;

                first.Metric(
                    x,
                    142,
                    cardWidth,
                    58,
                    item?.Label ??
                    "—",
                    item?.Value ??
                    "—"
                );
            }

            first.Text(
                32,
                226,
                10,
                "Filtered report records",
                true,
                "#0F172A"
            );

            first.Text(
                810,
                226,
                7,
                $"{report.Rows.Count.ToString("N0", CultureInfo.InvariantCulture)} row(s)",
                false,
                "#64748B",
                rightAlign:
                    true
            );

            var previewColumns =
                report.Columns
                    .Take(
                        Math.Min(
                            7,
                            report.Columns.Count
                        )
                    )
                    .ToList();

            DrawTable(
                first,
                previewColumns,
                report.Rows
                    .Take(
                        14
                    )
                    .ToList(),
                startY:
                    242,
                footerText:
                    report.Rows.Count >
                    14
                        ? $"Showing 14 of {report.Rows.Count} rows. Remaining records continue on following pages."
                        : $"{report.Rows.Count} row(s) in the filtered dataset."
            );

            pages.Add(
                first.Content
            );

            var detailColumns =
                report.Columns
                    .Take(
                        Math.Min(
                            8,
                            report.Columns.Count
                        )
                    )
                    .ToList();

            const int rowsPerPage =
                22;

            for (
                var offset = 0;
                offset <
                report.Rows.Count;
                offset +=
                    rowsPerPage
            )
            {
                var page =
                    new PdfCanvas();

                page.BrandHeader(
                    report.Title,
                    report.ClinicName,
                    hasLogo
                );

                page.Text(
                    32,
                    86,
                    8,
                    $"Detailed records | {RangeText(report)}",
                    false,
                    "#475569"
                );

                DrawTable(
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
                    startY:
                        108,
                    footerText:
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
                var empty =
                    new PdfCanvas();

                empty.BrandHeader(
                    report.Title,
                    report.ClinicName,
                    hasLogo
                );

                empty.Text(
                    32,
                    118,
                    12,
                    "No records matched the selected report parameters.",
                    true,
                    "#0F172A"
                );

                empty.Text(
                    32,
                    140,
                    8,
                    "The report was generated successfully, but the filtered dataset was empty.",
                    false,
                    "#64748B"
                );

                pages.Add(
                    empty.Content
                );
            }

            for (
                var index = 0;
                index <
                pages.Count;
                index++
            )
            {
                var canvas =
                    new PdfCanvas(
                        pages[
                            index
                        ]
                    );

                canvas.Footer(
                    index +
                    1,
                    pages.Count,
                    report.ClinicName
                );

                pages[
                    index
                ] =
                    canvas.Content;
            }

            return pages;
        }

        private static void
            DrawTable(
                PdfCanvas page,
                List<ClinicAdminDynamicReportColumnDto> columns,
                List<Dictionary<string, object?>> rows,
                double startY,
                string footerText
            )
        {
            if (
                columns.Count ==
                0
            )
            {
                page.Text(
                    32,
                    startY,
                    8,
                    "No report columns are available.",
                    false,
                    "#64748B"
                );

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
                "#0F172A"
            );

            for (
                var index = 0;
                index <
                columns.Count;
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
                    6.6,
                    Truncate(
                        columns[
                            index
                        ].Label,
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
                rowIndex <
                rows.Count;
                rowIndex++
            )
            {
                var row =
                    rows[
                        rowIndex
                    ];

                page.FillRect(
                    left,
                    y,
                    tableWidth,
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
                    tableWidth,
                    y +
                    rowHeight,
                    "#E2E8F0",
                    0.45
                );

                for (
                    var columnIndex = 0;
                    columnIndex <
                    columns.Count;
                    columnIndex++
                )
                {
                    var column =
                        columns[
                            columnIndex
                        ];

                    row.TryGetValue(
                        column.Key,
                        out var value
                    );

                    var display =
                        DisplayValue(
                            value,
                            column.DataType
                        );

                    var color =
                        column.DataType
                            .Equals(
                                "status",
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                            ? StatusColor(
                                display
                            )
                            : "#334155";

                    page.Text(
                        left +
                        columnIndex *
                        columnWidth +
                        5,
                        y +
                        6,
                        6.3,
                        Truncate(
                            display,
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
                y +
                12,
                6.8,
                footerText,
                false,
                "#64748B"
            );
        }

        private static string
            DisplayValue(
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
                    StringComparison
                        .OrdinalIgnoreCase
                ) ||
                dataType.Equals(
                    "datetime",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                if (
                    value is
                    DateTime date
                )
                {
                    return dataType
                        .Equals(
                            "date",
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                        ? date.ToString(
                            "yyyy-MM-dd",
                            CultureInfo
                                .InvariantCulture
                        )
                        : date.ToString(
                            "yyyy-MM-dd HH:mm",
                            CultureInfo
                                .InvariantCulture
                        );
                }

                if (
                    DateTime.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo
                                .InvariantCulture
                        ),
                        CultureInfo
                            .InvariantCulture,
                        DateTimeStyles
                            .AssumeUniversal |
                        DateTimeStyles
                            .AdjustToUniversal,
                        out var parsed
                    )
                )
                {
                    return dataType
                        .Equals(
                            "date",
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                        ? parsed.ToString(
                            "yyyy-MM-dd",
                            CultureInfo
                                .InvariantCulture
                        )
                        : parsed.ToString(
                            "yyyy-MM-dd HH:mm",
                            CultureInfo
                                .InvariantCulture
                        );
                }
            }

            if (
                dataType.Equals(
                    "number",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                if (
                    decimal.TryParse(
                        Convert.ToString(
                            value,
                            CultureInfo
                                .InvariantCulture
                        ),
                        NumberStyles.Any,
                        CultureInfo
                            .InvariantCulture,
                        out var number
                    )
                )
                {
                    return number
                        .ToString(
                            "N0",
                            CultureInfo
                                .InvariantCulture
                        );
                }
            }

            return Convert.ToString(
                value,
                CultureInfo
                    .InvariantCulture
            ) ??
            "—";
        }

        private static string
            RangeText(
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
                return "Current-state clinic report";
            }

            var from =
                report.DateFrom?
                    .ToString(
                        "yyyy-MM-dd",
                        CultureInfo
                            .InvariantCulture
                    ) ??
                "Start";

            var to =
                report.DateTo?
                    .ToString(
                        "yyyy-MM-dd",
                        CultureInfo
                            .InvariantCulture
                    ) ??
                "Present";

            return $"Reporting period: {from} to {to}";
        }

        private static string
            Truncate(
                string value,
                int max
            )
        {
            value ??=
                string.Empty;

            if (
                value.Length <=
                max
            )
            {
                return value;
            }

            return max <=
                3
                ? value[..max]
                : value[
                    ..(
                        max -
                        3
                    )
                  ] +
                  "...";
        }

        private static string
            StatusColor(
                string value
            )
        {
            var status =
                value
                    .ToLowerInvariant();

            if (
                status.Contains(
                    "active"
                ) ||
                status.Contains(
                    "completed"
                ) ||
                status.Contains(
                    "collected"
                ) ||
                status.Contains(
                    "taken"
                ) ||
                status.Contains(
                    "healthy"
                ) ||
                status.Contains(
                    "confirmed"
                )
            )
            {
                return "#166534";
            }

            if (
                status.Contains(
                    "pending"
                ) ||
                status.Contains(
                    "scheduled"
                ) ||
                status.Contains(
                    "low stock"
                ) ||
                status.Contains(
                    "due"
                )
            )
            {
                return "#92400E";
            }

            if (
                status.Contains(
                    "missed"
                ) ||
                status.Contains(
                    "overdue"
                ) ||
                status.Contains(
                    "cancel"
                ) ||
                status.Contains(
                    "inactive"
                )
            )
            {
                return "#991B1B";
            }

            return "#475569";
        }

        private sealed class PdfCanvas
        {
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
                _builder
                    .ToString();

            public void BrandHeader(
                string title,
                string clinicName,
                bool hasLogo
            )
            {
                FillRect(
                    0,
                    0,
                    PageWidth,
                    72,
                    "#FFFFFF"
                );

                if (
                    hasLogo
                )
                {
                    Image(
                        "Im1",
                        32,
                        14,
                        42,
                        42
                    );
                }
                else
                {
                    FillRect(
                        32,
                        14,
                        42,
                        42,
                        "#0F766E"
                    );

                    Text(
                        53,
                        25,
                        14,
                        "P",
                        true,
                        "#FFFFFF",
                        centerAlign:
                            true
                    );
                }

                Text(
                    86,
                    17,
                    7,
                    "PHILALINK",
                    true,
                    "#0F766E"
                );

                Text(
                    86,
                    31,
                    14,
                    title,
                    true,
                    "#0F172A"
                );

                Text(
                    86,
                    51,
                    7.5,
                    clinicName,
                    false,
                    "#64748B"
                );

                Text(
                    810,
                    19,
                    6.7,
                    "OFFICIAL CLINIC REPORT",
                    true,
                    "#64748B",
                    rightAlign:
                        true
                );

                Text(
                    810,
                    34,
                    6.7,
                    "PASSWORD PROTECTED",
                    true,
                    "#0F766E",
                    rightAlign:
                        true
                );

                Line(
                    32,
                    68,
                    810,
                    68,
                    "#0F766E",
                    1.4
                );
            }

            public void Metric(
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
                    "#FFFFFF"
                );

                StrokeRect(
                    x,
                    y,
                    width,
                    height,
                    "#CBD5E1",
                    0.6
                );

                FillRect(
                    x,
                    y,
                    3,
                    height,
                    "#0F766E"
                );

                Text(
                    x +
                    12,
                    y +
                    13,
                    6.8,
                    Truncate(
                        label,
                        30
                    ),
                    true,
                    "#64748B"
                );

                Text(
                    x +
                    12,
                    y +
                    30,
                    14,
                    Truncate(
                        value,
                        25
                    ),
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
                    6.4,
                    $"PhilaLink | {clinic} | Confidential - authorised clinical administration use only",
                    false,
                    "#64748B"
                );

                Text(
                    810,
                    570,
                    6.4,
                    $"Page {page} of {total}",
                    true,
                    "#64748B",
                    rightAlign:
                        true
                );
            }

            public void Image(
                string name,
                double x,
                double y,
                double width,
                double height
            )
            {
                var pdfY =
                    PageHeight -
                    y -
                    height;

                _builder.AppendLine(
                    $"q {N(width)} 0 0 {N(height)} {N(x)} {N(pdfY)} cm /{name} Do Q"
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
                bool rightAlign = false,
                bool centerAlign = false
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
                        : centerAlign
                            ? x -
                              width /
                              2
                            : x;

                _builder.AppendLine(
                    $"BT /{(bold ? "F2" : "F1")} {N(size)} Tf {N(rgb.R)} {N(rgb.G)} {N(rgb.B)} rg {N(drawX)} {N(PageHeight - y - size)} Td ({safe}) Tj ET"
                );
            }

            private static string
                PdfText(
                    string value
                )
            {
                var builder =
                    new StringBuilder();

                foreach (
                    var character in
                    value ??
                    string.Empty
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

                return builder
                    .ToString();
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

            private static string
                N(
                    double value
                )
            {
                return value
                    .ToString(
                        "0.###",
                        CultureInfo
                            .InvariantCulture
                    );
            }
        }

        private static class EncryptedPdfDocument
        {
            private static readonly byte[]
                PasswordPadding =
            {
                0x28,
                0xBF,
                0x4E,
                0x5E,
                0x4E,
                0x75,
                0x8A,
                0x41,
                0x64,
                0x00,
                0x4E,
                0x56,
                0xFF,
                0xFA,
                0x01,
                0x08,
                0x2E,
                0x2E,
                0x00,
                0xB6,
                0xD0,
                0x68,
                0x3E,
                0x80,
                0x2F,
                0x0C,
                0xA9,
                0xFE,
                0x64,
                0x53,
                0x69,
                0x7A
            };

            /*
             * Allow printing while denying
             * modification, extraction/copying,
             * annotations, form filling and
             * document assembly.
             */
            private const int Permissions =
                -1852;

            public static byte[] Build(
                List<string> pageContents,
                string userPassword,
                byte[]? logoJpeg
            )
            {
                var fileId =
                    RandomNumberGenerator
                        .GetBytes(
                            16
                        );

                var ownerPassword =
                    Convert
                        .ToBase64String(
                            RandomNumberGenerator
                                .GetBytes(
                                    24
                                )
                        );

                var ownerEntry =
                    BuildOwnerEntry(
                        ownerPassword,
                        userPassword
                    );

                var encryptionKey =
                    BuildEncryptionKey(
                        userPassword,
                        ownerEntry,
                        Permissions,
                        fileId
                    );

                var userEntry =
                    BuildUserEntry(
                        encryptionKey,
                        fileId
                    );

                var objects =
                    new List<PdfObject>();

                objects.Add(
                    PdfObject
                        .PlainAscii(
                            "<< /Type /Catalog /Pages 2 0 R >>"
                        )
                );

                objects.Add(
                    PdfObject
                        .PlainAscii(
                            string.Empty
                        )
                );

                objects.Add(
                    PdfObject
                        .PlainAscii(
                            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
                        )
                );

                objects.Add(
                    PdfObject
                        .PlainAscii(
                            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
                        )
                );

                int?
                    imageObjectNumber =
                        null;

                if (
                    logoJpeg is
                    {
                        Length:
                            > 0
                    }
                )
                {
                    imageObjectNumber =
                        objects.Count +
                        1;

                    objects.Add(
                        PdfObject
                            .Stream(
                                $"<< /Type /XObject /Subtype /Image /Width 220 /Height 220 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {logoJpeg.Length} >>",
                                logoJpeg,
                                encryptStream:
                                    true
                            )
                    );
                }

                var pageObjectNumbers =
                    new List<int>();

                foreach (
                    var content in
                    pageContents
                )
                {
                    var contentBytes =
                        Encoding.ASCII
                            .GetBytes(
                                content
                            );

                    var contentObjectNumber =
                        objects.Count +
                        1;

                    objects.Add(
                        PdfObject
                            .Stream(
                                $"<< /Length {contentBytes.Length} >>",
                                contentBytes,
                                encryptStream:
                                    true
                            )
                    );

                    var pageObjectNumber =
                        objects.Count +
                        1;

                    pageObjectNumbers
                        .Add(
                            pageObjectNumber
                        );

                    var xObjects =
                        imageObjectNumber
                            .HasValue
                            ? $" /XObject << /Im1 {imageObjectNumber.Value} 0 R >>"
                            : string.Empty;

                    objects.Add(
                        PdfObject
                            .PlainAscii(
                                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >>{xObjects} >> /Contents {contentObjectNumber} 0 R >>"
                            )
                    );
                }

                objects[1] =
                    PdfObject
                        .PlainAscii(
                            $"<< /Type /Pages /Count {pageObjectNumbers.Count} /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] >>"
                        );

                var encryptObjectNumber =
                    objects.Count +
                    1;

                objects.Add(
                    PdfObject
                        .PlainAscii(
                            $"<< /Filter /Standard /V 2 /R 3 /Length 128 /O <{Hex(ownerEntry)}> /U <{Hex(userEntry)}> /P {Permissions} >>"
                        )
                );

                using var stream =
                    new MemoryStream();

                WriteAscii(
                    stream,
                    "%PDF-1.4\n"
                );

                stream.Write(
                    new byte[]
                    {
                        (byte)'%',
                        0xE2,
                        0xE3,
                        0xCF,
                        0xD3,
                        (byte)'\n'
                    }
                );

                var offsets =
                    new List<long>
                    {
                        0
                    };

                for (
                    var index = 0;
                    index <
                    objects.Count;
                    index++
                )
                {
                    var objectNumber =
                        index +
                        1;

                    offsets.Add(
                        stream.Position
                    );

                    WriteAscii(
                        stream,
                        $"{objectNumber} 0 obj\n"
                    );

                    var obj =
                        objects[
                            index
                        ];

                    if (
                        obj.StreamData ==
                        null
                    )
                    {
                        stream.Write(
                            obj.PrefixBytes
                        );
                    }
                    else
                    {
                        var streamBytes =
                            obj.EncryptStream
                                ? Rc4(
                                    ObjectEncryptionKey(
                                        encryptionKey,
                                        objectNumber,
                                        0
                                    ),
                                    obj.StreamData
                                  )
                                : obj.StreamData;

                        var dictionary =
                            ReplaceLength(
                                Encoding.ASCII
                                    .GetString(
                                        obj.PrefixBytes
                                    ),
                                streamBytes.Length
                            );

                        WriteAscii(
                            stream,
                            dictionary
                        );

                        WriteAscii(
                            stream,
                            "\nstream\n"
                        );

                        stream.Write(
                            streamBytes
                        );

                        WriteAscii(
                            stream,
                            "\nendstream"
                        );
                    }

                    WriteAscii(
                        stream,
                        "\nendobj\n"
                    );
                }

                var xref =
                    stream.Position;

                WriteAscii(
                    stream,
                    $"xref\n0 {objects.Count + 1}\n"
                );

                WriteAscii(
                    stream,
                    "0000000000 65535 f \n"
                );

                for (
                    var index = 1;
                    index <
                    offsets.Count;
                    index++
                )
                {
                    WriteAscii(
                        stream,
                        $"{offsets[index]:0000000000} 00000 n \n"
                    );
                }

                var idHex =
                    Hex(
                        fileId
                    );

                WriteAscii(
                    stream,
                    $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Encrypt {encryptObjectNumber} 0 R /ID [<{idHex}><{idHex}>] >>\nstartxref\n{xref}\n%%EOF"
                );

                return stream
                    .ToArray();
            }

            private static string
                ReplaceLength(
                    string dictionary,
                    int actualLength
                )
            {
                const string marker =
                    "/Length ";

                var index =
                    dictionary
                        .IndexOf(
                            marker,
                            StringComparison
                                .Ordinal
                        );

                if (
                    index <
                    0
                )
                {
                    var close =
                        dictionary
                            .LastIndexOf(
                                ">>",
                                StringComparison
                                    .Ordinal
                            );

                    if (
                        close >=
                        0
                    )
                    {
                        return dictionary
                            .Insert(
                                close,
                                $" /Length {actualLength}"
                            );
                    }

                    return dictionary;
                }

                var numberStart =
                    index +
                    marker.Length;

                var numberEnd =
                    numberStart;

                while (
                    numberEnd <
                        dictionary.Length &&
                    char.IsDigit(
                        dictionary[
                            numberEnd
                        ]
                    )
                )
                {
                    numberEnd++;
                }

                return dictionary[
                    ..numberStart
                ] +
                actualLength
                    .ToString(
                        CultureInfo
                            .InvariantCulture
                    ) +
                dictionary[
                    numberEnd..
                ];
            }

            private static byte[]
                BuildOwnerEntry(
                    string ownerPassword,
                    string userPassword
                )
            {
                var ownerPadded =
                    PadPassword(
                        ownerPassword
                    );

                var digest =
                    MD5.HashData(
                        ownerPadded
                    );

                for (
                    var i = 0;
                    i <
                    50;
                    i++
                )
                {
                    digest =
                        MD5.HashData(
                            digest.AsSpan(
                                0,
                                16
                            )
                        );
                }

                var key =
                    digest
                        .AsSpan(
                            0,
                            16
                        )
                        .ToArray();

                var result =
                    Rc4(
                        key,
                        PadPassword(
                            userPassword
                        )
                    );

                for (
                    var i = 1;
                    i <=
                    19;
                    i++
                )
                {
                    result =
                        Rc4(
                            XorKey(
                                key,
                                (byte)i
                            ),
                            result
                        );
                }

                return result;
            }

            private static byte[]
                BuildEncryptionKey(
                    string userPassword,
                    byte[] ownerEntry,
                    int permissions,
                    byte[] fileId
                )
            {
                using var buffer =
                    new MemoryStream();

                buffer.Write(
                    PadPassword(
                        userPassword
                    )
                );

                buffer.Write(
                    ownerEntry
                );

                Span<byte>
                    permissionBytes =
                        stackalloc byte[4];

                BinaryPrimitives
                    .WriteInt32LittleEndian(
                        permissionBytes,
                        permissions
                    );

                buffer.Write(
                    permissionBytes
                );

                buffer.Write(
                    fileId
                );

                var digest =
                    MD5.HashData(
                        buffer
                            .ToArray()
                    );

                for (
                    var i = 0;
                    i <
                    50;
                    i++
                )
                {
                    digest =
                        MD5.HashData(
                            digest.AsSpan(
                                0,
                                16
                            )
                        );
                }

                return digest
                    .AsSpan(
                        0,
                        16
                    )
                    .ToArray();
            }

            private static byte[]
                BuildUserEntry(
                    byte[] encryptionKey,
                    byte[] fileId
                )
            {
                var seed =
                    new byte[
                        PasswordPadding.Length +
                        fileId.Length
                    ];

                Buffer.BlockCopy(
                    PasswordPadding,
                    0,
                    seed,
                    0,
                    PasswordPadding.Length
                );

                Buffer.BlockCopy(
                    fileId,
                    0,
                    seed,
                    PasswordPadding.Length,
                    fileId.Length
                );

                var digest =
                    MD5.HashData(
                        seed
                    );

                var result =
                    Rc4(
                        encryptionKey,
                        digest
                            .AsSpan(
                                0,
                                16
                            )
                            .ToArray()
                    );

                for (
                    var i = 1;
                    i <=
                    19;
                    i++
                )
                {
                    result =
                        Rc4(
                            XorKey(
                                encryptionKey,
                                (byte)i
                            ),
                            result
                        );
                }

                var userEntry =
                    new byte[32];

                Buffer.BlockCopy(
                    result,
                    0,
                    userEntry,
                    0,
                    16
                );

                Buffer.BlockCopy(
                    PasswordPadding,
                    0,
                    userEntry,
                    16,
                    16
                );

                return userEntry;
            }

            private static byte[]
                ObjectEncryptionKey(
                    byte[] documentKey,
                    int objectNumber,
                    int generationNumber
                )
            {
                var input =
                    new byte[
                        documentKey.Length +
                        5
                    ];

                Buffer.BlockCopy(
                    documentKey,
                    0,
                    input,
                    0,
                    documentKey.Length
                );

                input[
                    documentKey.Length
                ] =
                    (byte)(
                        objectNumber &
                        0xFF
                    );

                input[
                    documentKey.Length +
                    1
                ] =
                    (byte)(
                        (
                            objectNumber >>
                            8
                        ) &
                        0xFF
                    );

                input[
                    documentKey.Length +
                    2
                ] =
                    (byte)(
                        (
                            objectNumber >>
                            16
                        ) &
                        0xFF
                    );

                input[
                    documentKey.Length +
                    3
                ] =
                    (byte)(
                        generationNumber &
                        0xFF
                    );

                input[
                    documentKey.Length +
                    4
                ] =
                    (byte)(
                        (
                            generationNumber >>
                            8
                        ) &
                        0xFF
                    );

                var digest =
                    MD5.HashData(
                        input
                    );

                var keyLength =
                    Math.Min(
                        documentKey.Length +
                        5,
                        16
                    );

                return digest
                    .AsSpan(
                        0,
                        keyLength
                    )
                    .ToArray();
            }

            private static byte[]
                PadPassword(
                    string password
                )
            {
                var raw =
                    Encoding.Latin1
                        .GetBytes(
                            password ??
                            string.Empty
                        );

                var result =
                    new byte[32];

                var take =
                    Math.Min(
                        raw.Length,
                        32
                    );

                if (
                    take >
                    0
                )
                {
                    Buffer.BlockCopy(
                        raw,
                        0,
                        result,
                        0,
                        take
                    );
                }

                if (
                    take <
                    32
                )
                {
                    Buffer.BlockCopy(
                        PasswordPadding,
                        0,
                        result,
                        take,
                        32 -
                        take
                    );
                }

                return result;
            }

            private static byte[]
                XorKey(
                    byte[] key,
                    byte value
                )
            {
                var result =
                    new byte[
                        key.Length
                    ];

                for (
                    var i = 0;
                    i <
                    key.Length;
                    i++
                )
                {
                    result[
                        i
                    ] =
                        (byte)(
                            key[
                                i
                            ] ^
                            value
                        );
                }

                return result;
            }

            private static byte[]
                Rc4(
                    byte[] key,
                    byte[] data
                )
            {
                var state =
                    new byte[256];

                for (
                    var i = 0;
                    i <
                    256;
                    i++
                )
                {
                    state[
                        i
                    ] =
                        (byte)i;
                }

                var j =
                    0;

                for (
                    var i = 0;
                    i <
                    256;
                    i++
                )
                {
                    j =
                        (
                            j +
                            state[
                                i
                            ] +
                            key[
                                i %
                                key.Length
                            ]
                        ) &
                        0xFF;

                    (
                        state[
                            i
                        ],
                        state[
                            j
                        ]
                    ) =
                    (
                        state[
                            j
                        ],
                        state[
                            i
                        ]
                    );
                }

                var output =
                    new byte[
                        data.Length
                    ];

                var x =
                    0;

                j =
                    0;

                for (
                    var index = 0;
                    index <
                    data.Length;
                    index++
                )
                {
                    x =
                        (
                            x +
                            1
                        ) &
                        0xFF;

                    j =
                        (
                            j +
                            state[
                                x
                            ]
                        ) &
                        0xFF;

                    (
                        state[
                            x
                        ],
                        state[
                            j
                        ]
                    ) =
                    (
                        state[
                            j
                        ],
                        state[
                            x
                        ]
                    );

                    var k =
                        state[
                            (
                                state[
                                    x
                                ] +
                                state[
                                    j
                                ]
                            ) &
                            0xFF
                        ];

                    output[
                        index
                    ] =
                        (byte)(
                            data[
                                index
                            ] ^
                            k
                        );
                }

                return output;
            }

            private static string
                Hex(
                    byte[] bytes
                )
            {
                return Convert
                    .ToHexString(
                        bytes
                    );
            }

            private static void
                WriteAscii(
                    Stream stream,
                    string value
                )
            {
                var bytes =
                    Encoding.ASCII
                        .GetBytes(
                            value
                        );

                stream.Write(
                    bytes
                );
            }

            private sealed class PdfObject
            {
                public byte[] PrefixBytes
                { get; private init; } =
                    Array.Empty<byte>();

                public byte[]? StreamData
                { get; private init; }

                public bool EncryptStream
                { get; private init; }

                public static PdfObject
                    PlainAscii(
                        string value
                    )
                {
                    return new PdfObject
                    {
                        PrefixBytes =
                            Encoding.ASCII
                                .GetBytes(
                                    value
                                )
                    };
                }

                public static PdfObject
                    Stream(
                        string dictionary,
                        byte[] data,
                        bool encryptStream
                    )
                {
                    return new PdfObject
                    {
                        PrefixBytes =
                            Encoding.ASCII
                                .GetBytes(
                                    dictionary
                                ),

                        StreamData =
                            data,

                        EncryptStream =
                            encryptStream
                    };
                }
            }
        }
    }
}
