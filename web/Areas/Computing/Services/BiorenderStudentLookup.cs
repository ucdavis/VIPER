using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Computing.Model;
using Viper.Classes.Utilities;

namespace Viper.Areas.Computing.Services
{
    public class BiorenderStudentLookup
    {
        private readonly IamApi iam;
        public BiorenderStudentLookup(IamApi iamapi)
        {
            iam = iamapi;
        }

        /// <summary>
        /// Get student info from IAM for all emails in a list
        /// </summary>
        /// <param name="emails"></param>
        /// <returns></returns>
        public async Task<List<BiorenderStudent>> GetBiorenderStudentInfo(List<string> emails)
        {
            List<Task<BiorenderStudent>> resultList = new();
            var throttler = new SemaphoreSlim(initialCount: 20);
            foreach (var email in emails)
            {
                await throttler.WaitAsync();

                var emailTrimmed = email.Trim();
                if (!emailTrimmed.Contains('@'))
                {
                    emailTrimmed += "@ucdavis.edu";
                }
                if (IsValidEmail(emailTrimmed))
                {
                    resultList.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                return await GetSingleStudent(emailTrimmed);
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        })
                    );
                }
            }

            var taskResults = await Task.WhenAll(resultList);
            List<BiorenderStudent> students = new();
            foreach (var t in taskResults)
            {
                if (t != null)
                {
                    students.Add(t);
                }
            }

            return students;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<BiorenderStudent> GetSingleStudent(string email)
        {
            var std = new BiorenderStudent() { Email = email };

            //get iam info by email
            var iamContact = await iam.GetContactInfoByEmail(email);
            if (iamContact.Data == null || !iamContact.Data.Any())
            {
                return std;
            }
            std.IamId = iamContact.Data.First().IamId;

            //get student associations
            var stdAssoc = await iam.GetSisAssociations(std.IamId);
            if (stdAssoc.Data != null && stdAssoc.Data.Any())
            {
                std.StudentAssociations = stdAssoc.Data.ToList();
            }

            var iamPerson = await iam.GetByIamIds(new List<string>() { std.IamId });
            std.FirstName = iamPerson?.Data?.FirstOrDefault()?.DFirstName;
            std.LastName = iamPerson?.Data?.FirstOrDefault()?.DLastName;

            return std;
        }

        static private bool IsValidEmail(string email)
        {
            var trimmed = email.Trim();
            if (trimmed.Length == 0 || trimmed.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmed;
            }
            catch
            {
                return false;
            }
        }
    }
}
