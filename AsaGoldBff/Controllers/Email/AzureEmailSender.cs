using AsaGoldBff.Model.Email;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsaGoldBff.Controllers.Email
{
    /// <summary>
    /// Sendgrid email sender
    /// </summary>
    public class AzureEmailSender : IEmailSender
    {
        private readonly string fromName;
        private readonly string fromEmail;
        private readonly Azure.Communication.Email.EmailClient emailClient;
        private readonly ILogger<SendGridController> logger;
        /// <summary>
        /// Constructor
        /// </summary>
        public AzureEmailSender(
            ILogger<SendGridController> logger,
            IOptions<Model.Settings.AzureConfiguration> settings,
            IOptions<Model.Config.BFFOptions> bff
            )
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            emailClient = new Azure.Communication.Email.EmailClient(settings.Value.EmailConnectionString);
            fromEmail = bff.Value.SupportEmail;
            fromName = bff.Value.SupportName ?? "Support";

        }
        /// <summary>
        /// Semd email
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(
            string subject,
            string toEmail,
            string toName,
            IEmail data,
            IEnumerable<Attachment> attachments
            )
        {
            try
            {
                if (data == null)
                {
                    throw new Exception("Please define data for email");
                }
                var recepients = new Azure.Communication.Email.EmailRecipients(new List<Azure.Communication.Email.EmailAddress>() { new Azure.Communication.Email.EmailAddress(toEmail, toName) });
                var content = new Azure.Communication.Email.EmailContent("Subject");

                var source = File.ReadAllText($"EmailTemplates/{data.TemplateId}.html");
                var template = Handlebars.Compile(source);
                content.Html = template(data);
                var emailMessage = new Azure.Communication.Email.EmailMessage(fromEmail, recepients, content);
                await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error while sending email through sendgrid");
            }
            return false;
        }
    }

}
