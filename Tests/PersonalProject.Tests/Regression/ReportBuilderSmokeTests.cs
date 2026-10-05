using PersonalProject.Models.DTOs;
using PersonalProject.Utilities;
using System.Text;
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
                Report();

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
            ExcelReportProducesValidZipContainer()
        {
            var bytes =
                ClinicAdminReportBuilder
                    .BuildXlsx(
                        Report()
                    );

            Assert.True(
                bytes.Length >
                4
            );

            /*
             * XLSX files are ZIP containers.
             * ZIP files begin with PK.
             */
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
                        Report()
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

        private static ClinicAdminReportDataDto
            Report()
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
