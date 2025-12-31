using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UserRepository.Repositories;
using UserRepository.Model.Request;
using UserRepository.Models;
using Share.VerifyEmail;

namespace UserService.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IEmailVerificationRepository _emailVerificationRepository;
        public EmailVerificationService(EmailSettings emailSettings , IEmailVerificationRepository emailVerificationRepository)
        {
            _emailSettings = emailSettings;
            _emailVerificationRepository = emailVerificationRepository;
        }
        public async Task SendVerificationCodeAsync(string email)
        {
            // ❗ XÓA OTP CŨ
            await _emailVerificationRepository.DeleteAllByEmailAsync(email);

            var code = new Random().Next(100000, 999999).ToString();

            var verificationEmail = new EmailVerification
            {
                Email = email,
                Code = code,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IsVerified = false
            };

            await _emailVerificationRepository.AddAsync(verificationEmail);
            await _emailVerificationRepository.SaveChangesAsync();

            await SendEmailAsync(email, "Email Verification Code",
                $"Your verification code is: <b>{code}</b>. It will expire in 5 minutes.");
        }

        public async Task SendPasswordResetCodeAsync(string email, string otp)
        {
            await SendEmailAsync(email, "Password Reset OTP",
                $"Your OTP is: <b>{otp}</b>. It will expire in 10 minutes.");
        }

        public async Task<bool> VerifyCodeAsync(VerifyOTPRequest request)
        {
            var verification = await _emailVerificationRepository.GetByEmailAndCodeAsync(request.Email, request.Code);
            if (verification == null) return false;

            if (verification.IsVerified) return true; // Đã verify trước đó
            if (verification.ExpiredAt < DateTime.UtcNow) return false; // Hết hạn

            await _emailVerificationRepository.MarkAsVerifiedAsync(verification);
            await _emailVerificationRepository.SaveChangesAsync();
            return true;
        }
        public async Task<bool> IsEmailVerifiedAsync(string email)
        {
            var verification = await _emailVerificationRepository.GetByEmailAsync(email);
            return verification != null && verification.IsVerified;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
        public async Task SendPasswordChangedNotificationAsync(string email)
        {
            await SendEmailAsync(email, "Password Changed Successfully",
                "Your password has been successfully changed. If you did not make this change, please contact our support team immediately.");
        }

    }
}
