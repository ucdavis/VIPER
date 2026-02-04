using System.Xml.Serialization;

namespace Viper.Areas.Directory.Models
{
    [XmlRoot("item")]
    public class VMACSItem
    {
        [XmlAttribute("dbfile")]
        public int dbfile { get; set; }

        [XmlElement("Name")]
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
        [XmlElement("LastEdit")]
        public string[]? LastEdit { get; set; }
    }

    [XmlRoot("query")]
    public class VMACSQuery
    {
        [XmlElement("item")]
        public VMACSItem? item { get; set; }

        [XmlElement("dbfile")]
        public string[]? dbfile { get; set; }

        [XmlElement("dbfileNm")]
        public string[]? dbfileNm { get; set; }

        [XmlElement("uri")]
        public string[]? uri { get; set; }
    }
}
