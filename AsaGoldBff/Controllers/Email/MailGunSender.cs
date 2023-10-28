﻿using AsaGoldBff.Model.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsaGoldBff.Controllers.Email
{
    /// <summary>
    /// Sendgrid email sender
    /// </summary>
    public class MailGunSender : IEmailSender
    {
        private readonly ILogger<MailGunSender> logger;
        private readonly IOptions<Model.Settings.MailGunConfiguration> settings;
        /// <summary>
        /// Constructor
        /// </summary>
        public MailGunSender(
            ILogger<MailGunSender> logger,
            IOptions<Model.Settings.MailGunConfiguration> settings
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
            try
            {
                this.logger = logger;
                this.settings = settings;
                if (string.IsNullOrEmpty(settings.Value.ApiKey))
                {
                    throw new Exception("Invalid MailGun configuration");
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc.Message, exc);
            }
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

                if (string.IsNullOrEmpty(toEmail))
                {
                    logger.LogDebug($"Message {data.TemplateId} not delivered because email is not defined");
                    return false;
                }
                //logger.LogInformation($"Sending {data.TemplateId} email to {toEmail}");



                var client = new RestClient
                {
                    BaseUrl = new Uri(settings.Value.Endpoint),
                    Authenticator = new HttpBasicAuthenticator("api", settings.Value.ApiKey)
                };

                var request = new RestRequest();
                request.AddParameter("domain", settings.Value.Domain, ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", $"{settings.Value.MailerFromName} <{settings.Value.MailerFromEmail}>");
                request.AddParameter("to", $"{toName} <{toEmail}>");
                request.AddParameter("subject", subject);
                request.AddParameter("template", data.TemplateId);

                if (!string.IsNullOrEmpty(settings.Value.ReplyToEmail))
                {
                    var name = settings.Value.ReplyToEmail;
                    if (!string.IsNullOrEmpty(settings.Value.ReplyToName))
                    {
                        name = $"{settings.Value.ReplyToName} <{settings.Value.ReplyToEmail}>";
                    }
                    request.AddParameter("h:Reply-To", name);
                }

                request.AddParameter("v:IsCS", data.IsCS);
                request.AddParameter("v:IsSK", data.IsSK);
                request.AddParameter("v:IsDE", data.IsDE);
                request.AddParameter("v:IsEN", data.IsEN);
                request.AddParameter("v:IsHU", data.IsHU);

                request.AddParameter("v:isCS", data.IsCS);
                request.AddParameter("v:isSK", data.IsSK);
                request.AddParameter("v:isDE", data.IsDE);
                request.AddParameter("v:isEN", data.IsEN);
                request.AddParameter("v:isHU", data.IsHU);

                request.AddParameter("v:Website", data.Website);
                request.AddParameter("v:SupportEmail", data.SupportEmail);
                request.AddParameter("v:SupportPhone", data.SupportPhone);

                if (data is PersonalDataRemovedEmail)
                {
                    var emailData = data as PersonalDataRemovedEmail;
                    request.AddParameter("v:Name", emailData.Name);
                }

                if (data is RolesUpdatedEmail)
                {
                    var emailData = data as RolesUpdatedEmail;
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Password", emailData.Password);
                    request.AddParameter("v:Roles", emailData.Roles);
                }
                if (data is VisitorChangeRegistrationEmail)
                {
                    var emailData = data as VisitorChangeRegistrationEmail;
                    request.AddParameter("v:BarCode", emailData.BarCode);
                    request.AddParameter("v:Code", emailData.Code);
                    request.AddParameter("v:Date", emailData.Date);
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Place", emailData.Place);
                    request.AddParameter("v:PlaceDescription", emailData.PlaceDescription);
                }
                if (data is VisitorRegistrationEmail)
                {
                    var emailData = data as VisitorRegistrationEmail;
                    request.AddParameter("v:BarCode", emailData.BarCode);
                    request.AddParameter("v:Code", emailData.Code);
                    request.AddParameter("v:Date", emailData.Date);
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Place", emailData.Place);
                    request.AddParameter("v:PlaceDescription", emailData.PlaceDescription);
                }
                if (data is VisitorTestingInProcessEmail)
                {
                    var emailData = data as VisitorTestingInProcessEmail;
                    request.AddParameter("v:Name", emailData.Name);
                }
                if (data is VisitorTestingResultEmail)
                {
                    var emailData = data as VisitorTestingResultEmail;
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:IsSick", emailData.IsSick);
                }
                if (data is VisitorTestingToBeRepeatedEmail)
                {
                    var emailData = data as VisitorTestingToBeRepeatedEmail;
                    request.AddParameter("v:Name", emailData.Name);
                }

                foreach (var attachment in attachments)
                {
                    request.AddFile("attachment", Convert.FromBase64String(attachment.Content), attachment.Filename, attachment.Type);
                }
                request.Method = Method.POST;
                logger.LogInformation($"Sending {data.TemplateId} email to {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toEmail)}");
                var response = client.Execute(request);
                if (response.IsSuccessful)
                {
                    logger.LogInformation($"Sent {data.TemplateId} email to {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toEmail)}");
                }
                return response.IsSuccessful;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error while sending email through mailgun");
                return false;
            }
        }
    }

}
