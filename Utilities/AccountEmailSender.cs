using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalProject.Models.Constants;
using System.Net;
using System.Net.Mail;

namespace PersonalProject.Utilities
{
    public static class AccountEmailSender
    {
        public static async Task
            SendAccountInvitationAsync(
                IConfiguration configuration,
                ILogger logger,
                string toEmail,
                string recipientName,
                string role,
                string? clinicName,
                string temporaryPassword
            )
        {
            var frontendBaseUrl =
                GetFrontendBaseUrl(
                    configuration
                );

            var loginUrl =
                frontendBaseUrl +
                "/login";

            var logoUrl =
                frontendBaseUrl +
                "/logo2.png";

            var safeName =
                WebUtility.HtmlEncode(
                    recipientName
                );

            var safeRole =
                WebUtility.HtmlEncode(
                    RoleLabel(
                        role
                    )
                );

            var safeClinic =
                WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(
                        clinicName
                    )
                        ? "Not currently assigned"
                        : clinicName
                );

            var safePassword =
                WebUtility.HtmlEncode(
                    temporaryPassword
                );

            var safeLoginUrl =
                WebUtility.HtmlEncode(
                    loginUrl
                );

            var safeLogoUrl =
                WebUtility.HtmlEncode(
                    logoUrl
                );

            var subject =
                $"Your PhilaLink {RoleLabel(role)} account";

            var html =
                $@"
<!doctype html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta
        name=""viewport""
        content=""width=device-width, initial-scale=1""
    >
    <title>{subject}</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background:#f4f6f5;
    font-family:Arial,Helvetica,sans-serif;
    color:#0f172a;
"">

<table
    role=""presentation""
    width=""100%""
    cellspacing=""0""
    cellpadding=""0""
    border=""0""
    style=""
        width:100%;
        background:#f4f6f5;
        padding:32px 12px;
    ""
>
    <tr>
        <td align=""center"">

            <table
                role=""presentation""
                width=""100%""
                cellspacing=""0""
                cellpadding=""0""
                border=""0""
                style=""
                    width:100%;
                    max-width:620px;
                    background:#ffffff;
                    border:1px solid #dbe4e2;
                ""
            >

                <tr>
                    <td
                        style=""
                            height:6px;
                            background:#0f766e;
                            font-size:0;
                        ""
                    >
                        &nbsp;
                    </td>
                </tr>

                <tr>
                    <td
                        style=""
                            padding:28px 32px 20px;
                            border-bottom:1px solid #e2e8f0;
                        ""
                    >

                        <table
                            role=""presentation""
                            cellspacing=""0""
                            cellpadding=""0""
                            border=""0""
                        >
                            <tr>
                                <td
                                    style=""
                                        padding-right:14px;
                                    ""
                                >
                                    <img
                                        src=""{safeLogoUrl}""
                                        alt=""PhilaLink""
                                        width=""50""
                                        style=""
                                            display:block;
                                            width:50px;
                                            height:auto;
                                        ""
                                    >
                                </td>

                                <td>
                                    <div
                                        style=""
                                            font-size:21px;
                                            line-height:26px;
                                            font-weight:700;
                                            color:#0f172a;
                                        ""
                                    >
                                        PhilaLink
                                    </div>

                                    <div
                                        style=""
                                            margin-top:2px;
                                            font-size:12px;
                                            line-height:18px;
                                            color:#0f766e;
                                        ""
                                    >
                                        Healthcare coordination platform
                                    </div>
                                </td>
                            </tr>
                        </table>

                    </td>
                </tr>

                <tr>
                    <td
                        style=""
                            padding:30px 32px;
                        ""
                    >

                        <p
                            style=""
                                margin:0 0 18px;
                                font-size:15px;
                                line-height:24px;
                                color:#475569;
                            ""
                        >
                            Hello {safeName},
                        </p>

                        <h1
                            style=""
                                margin:0 0 14px;
                                font-size:24px;
                                line-height:31px;
                                color:#0f172a;
                            ""
                        >
                            Your PhilaLink account has been created
                        </h1>

                        <p
                            style=""
                                margin:0 0 24px;
                                font-size:14px;
                                line-height:23px;
                                color:#475569;
                            ""
                        >
                            An authorised PhilaLink administrator has created
                            an account for you. Your initial access details are
                            shown below.
                        </p>

                        <table
                            role=""presentation""
                            width=""100%""
                            cellspacing=""0""
                            cellpadding=""0""
                            border=""0""
                            style=""
                                width:100%;
                                margin-bottom:22px;
                                border:1px solid #e2e8f0;
                                background:#f8fafc;
                            ""
                        >

                            <tr>
                                <td
                                    style=""
                                        padding:13px 16px;
                                        width:150px;
                                        border-bottom:1px solid #e2e8f0;
                                        font-size:12px;
                                        color:#64748b;
                                    ""
                                >
                                    Role
                                </td>

                                <td
                                    style=""
                                        padding:13px 16px;
                                        border-bottom:1px solid #e2e8f0;
                                        font-size:13px;
                                        font-weight:600;
                                        color:#0f172a;
                                    ""
                                >
                                    {safeRole}
                                </td>
                            </tr>

                            <tr>
                                <td
                                    style=""
                                        padding:13px 16px;
                                        width:150px;
                                        border-bottom:1px solid #e2e8f0;
                                        font-size:12px;
                                        color:#64748b;
                                    ""
                                >
                                    Clinic
                                </td>

                                <td
                                    style=""
                                        padding:13px 16px;
                                        border-bottom:1px solid #e2e8f0;
                                        font-size:13px;
                                        font-weight:600;
                                        color:#0f172a;
                                    ""
                                >
                                    {safeClinic}
                                </td>
                            </tr>

                            <tr>
                                <td
                                    style=""
                                        padding:13px 16px;
                                        width:150px;
                                        font-size:12px;
                                        color:#64748b;
                                    ""
                                >
                                    Sign-in ID
                                </td>

                                <td
                                    style=""
                                        padding:13px 16px;
                                        font-size:13px;
                                        color:#334155;
                                    ""
                                >
                                    Use the SA ID number supplied when your
                                    account was registered.
                                </td>
                            </tr>

                        </table>

                        <div
                            style=""
                                margin:0 0 24px;
                                padding:18px;
                                border:1px solid #f3d38a;
                                background:#fffbeb;
                            ""
                        >

                            <div
                                style=""
                                    margin-bottom:7px;
                                    font-size:11px;
                                    font-weight:700;
                                    text-transform:uppercase;
                                    letter-spacing:.08em;
                                    color:#92400e;
                                ""
                            >
                                Temporary password
                            </div>

                            <div
                                style=""
                                    font-family:Consolas,Monaco,monospace;
                                    font-size:21px;
                                    line-height:28px;
                                    font-weight:700;
                                    color:#0f172a;
                                    word-break:break-all;
                                ""
                            >
                                {safePassword}
                            </div>

                        </div>

                        <div
                            style=""
                                margin:0 0 24px;
                                padding:16px;
                                border-left:4px solid #0f766e;
                                background:#f0fdfa;
                                font-size:13px;
                                line-height:21px;
                                color:#134e4a;
                            ""
                        >
                            <strong>
                                You must change this temporary password
                                immediately after your first login.
                            </strong>
                            PhilaLink will restrict normal application access
                            until you choose your own password.
                        </div>

                        <table
                            role=""presentation""
                            cellspacing=""0""
                            cellpadding=""0""
                            border=""0""
                            style=""
                                margin-bottom:24px;
                            ""
                        >
                            <tr>
                                <td
                                    style=""
                                        background:#0f766e;
                                    ""
                                >
                                    <a
                                        href=""{safeLoginUrl}""
                                        style=""
                                            display:inline-block;
                                            padding:12px 20px;
                                            color:#ffffff;
                                            text-decoration:none;
                                            font-size:14px;
                                            font-weight:600;
                                        ""
                                    >
                                        Sign in to PhilaLink
                                    </a>
                                </td>
                            </tr>
                        </table>

                        <p
                            style=""
                                margin:0 0 10px;
                                font-size:12px;
                                line-height:20px;
                                color:#64748b;
                            ""
                        >
                            Do not share this temporary password with anyone.
                            PhilaLink administrators do not need to know your
                            password.
                        </p>

                        <p
                            style=""
                                margin:0;
                                font-size:12px;
                                line-height:20px;
                                color:#64748b;
                            ""
                        >
                            If you were not expecting this account, contact
                            your clinic or PhilaLink administrator before
                            signing in.
                        </p>

                    </td>
                </tr>

                <tr>
                    <td
                        style=""
                            padding:18px 32px;
                            border-top:1px solid #e2e8f0;
                            background:#f8fafc;
                            font-size:11px;
                            line-height:18px;
                            color:#94a3b8;
                        ""
                    >
                        This is an automated PhilaLink account notification.
                    </td>
                </tr>

            </table>

        </td>
    </tr>
</table>

</body>
</html>
";

            await SendAsync(
                configuration,
                logger,
                toEmail,
                subject,
                html
            );
        }

        public static async Task
            SendClinicAdminAssignmentAsync(
                IConfiguration configuration,
                ILogger logger,
                string toEmail,
                string recipientName,
                string eventType,
                string? clinicName,
                string? previousClinicName = null
            )
        {
            var frontendBaseUrl =
                GetFrontendBaseUrl(
                    configuration
                );

            var loginUrl =
                frontendBaseUrl +
                "/login";

            var logoUrl =
                frontendBaseUrl +
                "/logo2.png";

            var safeName =
                WebUtility.HtmlEncode(
                    recipientName
                );

            var safeClinic =
                WebUtility.HtmlEncode(
                    clinicName ??
                    "No clinic assigned"
                );

            var safePreviousClinic =
                WebUtility.HtmlEncode(
                    previousClinicName ??
                    "your previous clinic"
                );

            var safeLoginUrl =
                WebUtility.HtmlEncode(
                    loginUrl
                );

            var safeLogoUrl =
                WebUtility.HtmlEncode(
                    logoUrl
                );

            string subject;
            string heading;
            string message;

            switch (
                eventType
                    .Trim()
                    .ToLowerInvariant()
            )
            {
                case "reassigned":
                    subject =
                        "Your PhilaLink clinic assignment has changed";

                    heading =
                        "Clinic assignment updated";

                    message =
                        $"Your Clinic Administrator assignment has changed from <strong>{safePreviousClinic}</strong> to <strong>{safeClinic}</strong>.";

                    break;

                case "deassigned":
                    subject =
                        "Your PhilaLink clinic assignment was removed";

                    heading =
                        "Clinic assignment removed";

                    message =
                        $"Your Clinic Administrator assignment to <strong>{safePreviousClinic}</strong> has been removed. Your account remains in PhilaLink, but clinic-scoped administrative access is unavailable until a Super Administrator assigns you to a clinic again.";

                    break;

                case "confirmed":
                    subject =
                        "Your PhilaLink clinic assignment";

                    heading =
                        "Clinic assignment confirmed";

                    message =
                        $"Your Clinic Administrator assignment to <strong>{safeClinic}</strong> has been confirmed.";

                    break;

                default:
                    subject =
                        "You have been assigned to a PhilaLink clinic";

                    heading =
                        "Clinic assignment";

                    message =
                        $"Your Clinic Administrator account has been assigned to <strong>{safeClinic}</strong>.";

                    break;
            }

            var html =
                $@"
<!doctype html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta
        name=""viewport""
        content=""width=device-width, initial-scale=1""
    >
    <title>{subject}</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background:#f4f6f5;
    font-family:Arial,Helvetica,sans-serif;
    color:#0f172a;
"">

<table
    role=""presentation""
    width=""100%""
    cellspacing=""0""
    cellpadding=""0""
    border=""0""
    style=""
        width:100%;
        background:#f4f6f5;
        padding:32px 12px;
    ""
>
    <tr>
        <td align=""center"">

            <table
                role=""presentation""
                width=""100%""
                cellspacing=""0""
                cellpadding=""0""
                border=""0""
                style=""
                    width:100%;
                    max-width:600px;
                    background:#ffffff;
                    border:1px solid #dbe4e2;
                ""
            >

                <tr>
                    <td
                        style=""
                            height:6px;
                            background:#0f766e;
                            font-size:0;
                        ""
                    >
                        &nbsp;
                    </td>
                </tr>

                <tr>
                    <td
                        style=""
                            padding:26px 30px;
                            border-bottom:1px solid #e2e8f0;
                        ""
                    >
                        <img
                            src=""{safeLogoUrl}""
                            alt=""PhilaLink""
                            width=""44""
                            style=""
                                display:inline-block;
                                width:44px;
                                height:auto;
                                vertical-align:middle;
                                margin-right:12px;
                            ""
                        >

                        <span
                            style=""
                                display:inline-block;
                                vertical-align:middle;
                                font-size:20px;
                                font-weight:700;
                                color:#0f172a;
                            ""
                        >
                            PhilaLink
                        </span>
                    </td>
                </tr>

                <tr>
                    <td
                        style=""
                            padding:30px;
                        ""
                    >

                        <p
                            style=""
                                margin:0 0 18px;
                                font-size:15px;
                                line-height:23px;
                                color:#475569;
                            ""
                        >
                            Hello {safeName},
                        </p>

                        <h1
                            style=""
                                margin:0 0 16px;
                                font-size:23px;
                                line-height:30px;
                                color:#0f172a;
                            ""
                        >
                            {heading}
                        </h1>

                        <p
                            style=""
                                margin:0 0 24px;
                                font-size:14px;
                                line-height:23px;
                                color:#475569;
                            ""
                        >
                            {message}
                        </p>

                        <div
                            style=""
                                margin:0 0 24px;
                                padding:15px 17px;
                                border-left:4px solid #0f766e;
                                background:#f0fdfa;
                                font-size:13px;
                                line-height:21px;
                                color:#134e4a;
                            ""
                        >
                            Clinic assignments control which clinic's
                            administrative data and functions your Clinic
                            Administrator account can access.
                        </div>

                        <table
                            role=""presentation""
                            cellspacing=""0""
                            cellpadding=""0""
                            border=""0""
                        >
                            <tr>
                                <td
                                    style=""
                                        background:#0f766e;
                                    ""
                                >
                                    <a
                                        href=""{safeLoginUrl}""
                                        style=""
                                            display:inline-block;
                                            padding:12px 20px;
                                            color:#ffffff;
                                            text-decoration:none;
                                            font-size:14px;
                                            font-weight:600;
                                        ""
                                    >
                                        Open PhilaLink
                                    </a>
                                </td>
                            </tr>
                        </table>

                        <p
                            style=""
                                margin:24px 0 0;
                                font-size:12px;
                                line-height:20px;
                                color:#64748b;
                            ""
                        >
                            If you did not expect this change, contact a
                            PhilaLink Super Administrator.
                        </p>

                    </td>
                </tr>

            </table>

        </td>
    </tr>
</table>

</body>
</html>
";

            await SendAsync(
                configuration,
                logger,
                toEmail,
                subject,
                html
            );
        }

        private static async Task
            SendAsync(
                IConfiguration configuration,
                ILogger logger,
                string toEmail,
                string subject,
                string htmlBody
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    toEmail
                )
            )
            {
                throw new InvalidOperationException(
                    "The account does not have an email address."
                );
            }

            var host =
                configuration[
                    "Smtp:Host"
                ];

            var username =
                configuration[
                    "Smtp:Username"
                ];

            var password =
                configuration[
                    "Smtp:Password"
                ];

            var fromAddress =
                configuration[
                    "Smtp:From"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    host
                ) ||
                string.IsNullOrWhiteSpace(
                    username
                ) ||
                string.IsNullOrWhiteSpace(
                    password
                )
            )
            {
                logger.LogError(
                    "SMTP configuration is incomplete for account email delivery."
                );

                throw new InvalidOperationException(
                    "Account email delivery is temporarily unavailable."
                );
            }

            if (
                !int.TryParse(
                    configuration[
                        "Smtp:Port"
                    ],
                    out var port
                )
            )
            {
                port =
                    587;
            }

            if (
                string.IsNullOrWhiteSpace(
                    fromAddress
                )
            )
            {
                fromAddress =
                    username;
            }

            var enableSsl =
                !bool.TryParse(
                    configuration[
                        "Smtp:EnableSsl"
                    ],
                    out var configuredSsl
                ) ||
                configuredSsl;

            using var client =
                new SmtpClient(
                    host,
                    port
                )
                {
                    EnableSsl =
                        enableSsl,

                    UseDefaultCredentials =
                        false,

                    DeliveryMethod =
                        SmtpDeliveryMethod
                            .Network,

                    Credentials =
                        new NetworkCredential(
                            username,
                            password
                        )
                };

            using var message =
                new MailMessage
                {
                    From =
                        new MailAddress(
                            fromAddress,
                            "PhilaLink"
                        ),

                    Subject =
                        subject,

                    Body =
                        htmlBody,

                    IsBodyHtml =
                        true
                };

            message.To.Add(
                toEmail
            );

            try
            {
                await client
                    .SendMailAsync(
                        message
                    );
            }
            catch (
                Exception ex
            )
            {
                logger.LogError(
                    ex,
                    "Failed to send a PhilaLink account email to {Email}.",
                    toEmail
                );

                throw new InvalidOperationException(
                    "The account email could not be delivered."
                );
            }
        }

        private static string
            GetFrontendBaseUrl(
                IConfiguration configuration
            )
        {
            var value =
                configuration[
                    "Frontend:BaseUrl"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                value =
                    "https://philalinkmed.vercel.app";
            }

            return value
                .Trim()
                .TrimEnd('/');
        }

        private static string RoleLabel(
            string role
        )
        {
            return role switch
            {
                RoleNames.ClinicAdmin =>
                    "Clinic Administrator",

                RoleNames.Nurse =>
                    "Nurse",

                RoleNames.Proxy =>
                    "Proxy",

                _ =>
                    role
            };
        }
    }
}
