using AngleSharp.Css.Dom;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Viper.Models.VIPER;
public partial class Keys
{
    public int Id { get; set; }
    public int KeyId { get; set; }
    public string CutNumber { get; set; } = null;
    public string RequestedBy { get; set; } = null;
    public string AssignedTo { get; set; } = null;
    public string AdHocName { get; set; } = null;
    public DateTime? RequestDate { get; set; } = null;
    public DateTime? IssuedDate { get; set; } = null;
    public int Disposition { get; set; } = 0;
    public DateTime? DispositionDate { get; set; } = null;
    public string DispositionBy { get; set; } = null;
    public DateTime? DueDate { get; set; } = null;
    public int Deleted { get; set; } = 0;
    public int Expr1 { get; set; } = 0;
    public string KeyNumber { get; set; } = null;
    public string AccessDescription { get; set; } = null;
    public DateTime? CreatedDate { get; set; } = null;
    public string CreatedBy { get; set; } = null;
    public DateTime? UpdatedDate { get; set; } = null;
    public string UpdatedBy { get; set; } = null;
    public string ManagedBy { get; set; } = null;
    public bool BuildingMaster { get; set; } = false;
    public bool Submaster { get; set; } = false;
    public bool Grandmaster { get; set; } = false;
    public bool Restricted { get; set; } = false;
    public string RestrictedContact { get; set; } = null;
    public bool Expr2 { get; set; } = false;
    public string Notes { get; set; } = null;
    public string Issued_by { get; set; } = null;
}
