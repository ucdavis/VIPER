using Microsoft.AspNetCore.Mvc;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using Viper.Areas.RAPS.Models;
using System.Net.Http;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Viper.Areas.Directory.Services
{
    public class VMACSService
    {
        //private const string _ldapUsername = "uid=vetmed,ou=Special Users,dc=ucdavis,dc=edu";
        private static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://vmacs-vmth.vetmed.ucdavis.edu"),
        };
        /*

         * 
         */
        [XmlRoot("query/item")]
        public class VMACS_API
        {
            [XmlElement("name")]
            public string[]? Name { get; set; }
            [XmlElement("UserID")]
            public string[]? VMACSID { get; set; }
            [XmlElement("Unit")]
            public string[]? Unit { get; set; }
            [XmlElement("Home")]
            public string[]? Home { get; set; }
            [XmlElement("Nextel")]
            public string[]? Nextel { get; set; }
            [XmlElement("LDPager")]
            public string[]? LDPager { get; set; }
            [XmlElement("Status")]
            public string[]? Status { get; set; }
            [XmlElement("Campus_LoginID")]
            public string[]? Campus_LoginID { get; set; }
            [XmlElement("Email_Forward")]
            public string[]? Email_Forward { get; set; }
        }

        public VMACSService() { }

        /// <summary>
        /// </summary>
        /// <param name="loginID"></param>
        /// <returns></returns>
        public static async Task<String?> Search (String? loginID)
        {
			string request = $"/trust/query.xml?dbfile=3&index=CampusLoginId&find={loginID}&format=CHRIS4&AUTH=06232005".ToString();
            using HttpResponseMessage response = await sharedClient.GetAsync(request);
            return response.ToString();
            if (response.IsSuccessStatusCode == false){
                return null;
            }
            var s = await response.Content.ReadAsStringAsync();
            var buffer = Encoding.UTF8.GetBytes(s);
            using (var stream = new MemoryStream(buffer))
            {
                return stream.ToString();
                //var serializer = new XmlSerializer(typeof(VMACS_API));
                //var vmacs_api = (VMACS_API?)serializer.Deserialize(stream);
                //if (vmacs_api != null && vmacs_api.Nextel != null){
                //    return vmacs_api.Nextel[0];
                //}
            }
            return null;
        }


        /*
         *         var result = "";
        var contacts = {LDPager="", Nextel="", LastEdit=""};
        cfhttp(url="https://vmacs-vmth.vetmed.ucdavis.edu/trust/query.xml?dbfile=3&index=CampusLoginId&find=#loginID#&format=CHRIS4&AUTH=06232005", 
				method="get", timeout="2", result="result") {}
				
		//the number of try/catch block here could be reduced by using more xml functions to check if something exists?
        try {
			var data = xmlSearch(xmlParse(result.FileContent),"/query/item");
			var exp = "";
			var lastEdit = "";
			//most users only have one, but in some cases, multiple accounts exist.
			//grab the latest edited, non-expired record.
			for(var d in data) {
				if(d?.Campus_LoginID?.XmlText == loginID) { //mvt (Megan Rott) matches mvthomps (not Megan Rott)
					//try to get dates - if this fails, assume the record is active and was lastedited today
					try {
						exp = getDateFromVmacsString(d.Deactive.XmlText);
						lastEdit = getDateFromVmacsString(d.LastEdit.XmlText);
					} 
					catch(Any e) {
						exp = dateAdd("yyyy", 1, now());
						lastEdit = now();
					}

					if(exp > now() && (!isValid("date", contacts.LastEdit) || lastEdit > contacts.LastEdit)) {
						contacts.lastEdit = lastEdit;

						//individual tries in case these fields don't exist
						try {
							contacts.LDPager = d.LDPager.XmlText;
							if(contacts.LDPager != "") {
								contacts.LDPager = "1-530-" & contacts.LDPager;
							}
						}
						catch(Any e) {}
						
						try {
							contacts.Nextel = d.Nextel.XmlText;
						}
						catch(Any e) {}
					}
				}
			} 
        }
        catch(Any e) {}
        return contacts;

         */

        /*
        /// <summary>
        /// </summary>
        /// <param name="name">Partial name of group to search for</param>
        /// <returns>List of groups</returns>
        public List<LdapGroup> GetGroups(string? name = null)
        {
        }
        */

    }
}
