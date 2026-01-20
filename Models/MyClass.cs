using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElDesignApp.Models;

using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ElDesignApp.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization; // For CultureInfo.InvariantCulture
using Microsoft.AspNetCore.Identity;

public class MyClass
{
    
}

public class ConnectionStrings
{
    public string? DefaultConnection { get; set; }
    public string? MacMini { get; set; }
    public string? MacBook { get; set; }
    public string? OracleVM1VCU { get; set; }
    public string? TestFeb24Context { get; set; }
    public string? MacMiniDocker { get; set; }
    public string? Redis { get; set; }
}



/// <summary>
/// Updated RoleMapping model - now project-specific
/// </summary>
public class RoleMapping
{
    [Key]
    public Guid UID { get; set; }
    
    /// <summary>
    /// Links role to specific project. Required for project-specific roles.
    /// </summary>
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string CustomRoleName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON array of hard-coded role names: ["Admin", "User"]
    /// </summary>
    [Required]
    public string MappedHardRoles { get; set; } = "[]";
    
    /// <summary>
    /// If false, role cannot be edited or deleted (e.g., "Admin" role)
    /// </summary>
    public bool IsEditable { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation properties
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }
}


[Table("UserRoleAssignment")]
public class UserRoleAssignment
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string ProjectId { get; set; } = string.Empty;
    
    public string UserId { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string RoleId { get; set; } = string.Empty;
    
    public string RoleName { get; set; } = string.Empty;
    
    public DateTime? CreatedDate { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// NEW: Maps users to custom roles within a project
/// Admin assigns these after SuperAdmin does soft assignment
/// </summary>
public class ProjectUserRole
{
    [Key]
    public Guid UID { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string CustomRoleName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(450)]
    public string AssignedBy { get; set; } = string.Empty;
    
    public DateTime AssignedOn { get; set; } = DateTime.Now;
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(450)]
    public string? RemovedBy { get; set; }
    
    public DateTime? RemovedOn { get; set; }
    
    // Navigation properties
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    [ForeignKey("CustomRoleName,ProjectId")]
    public virtual RoleMapping? RoleMapping { get; set; }
}


public class DBMaster: BaseInfo
{
    public DBMaster()
    {
    }

    public string DBName { get; set; }
    public string FieldName { get; set; }
    public string FieldProperty { get; set; }
    public string FieldNull { get; set; }
    public string DisplayFieldName { get; set; }
    public string ShortFieldName { get; set; }
    public string SampleValue { get; set; }
    public int FieldOrder { get; set; }
    public bool Display { get; set; }
}

public class ProjectUserAssignment
{
    [Key]
    public Guid UID { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this user is an Admin for this project
    /// SuperAdmin assigns this. If true, user can manage roles in this project.
    /// </summary>
    public bool IsProjectAdmin { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    [StringLength(450)]
    public string AssignedBy { get; set; } = string.Empty;
    
    public DateTime AssignedOn { get; set; } = DateTime.Now;
    
    [StringLength(450)]
    public string? RemovedBy { get; set; }
    
    public DateTime? RemovedOn { get; set; }
    
    // Navigation properties
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
}

/// <summary>
/// Updated Project model - removed redundant JSON columns
/// </summary>
public class Project
{
    public Project()
    {
        UID = Guid.NewGuid();
        Tag = "";
        TagDescription = "";
        ProjAlt = "base";
        ProjClient = "";
        ProjPartners = "[\"self\"]";
        ProjLocation = "";
        Order = 0;
        Display = true;
        XEW = true;
        GlobalE = 0f;
        GlobalN = 0f;
    }

    [Key]
    public Guid UID { get; set; }

    [Required(ErrorMessage = "Project Code is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Project Code must be 3–20 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s._-]+$", ErrorMessage = "Only letters, numbers, space, dot, underscore, hyphen allowed")]
    [Display(Name = "Project Code", Order = 2)]
    public string Tag { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Project Description", Order = 3)]
    public string TagDescription { get; set; }

    [Display(Name = "Alternative Name", Order = 4)] 
    public string ProjAlt { get; set; } = "base";

    [Display(Name = "Client", Order = 5)] 
    public string ProjClient { get; set; }

    [Display(Name = "Partners (JSON)", Order = 6)]
    public string ProjPartners { get; set; }
    
    [Display(Name = "Location", Order = 7)]
    public string ProjLocation { get; set; }

    [Display(Name = "Display in List", Order = 9)]
    public bool Display { get; set; } = true;

    [Display(Name = "X-Axis = East-West", Order = 10)]
    public bool XEW { get; set; } = true;

    [Display(Name = "Local Easting (X=0)", Order = 11)]
    public float LocalE { get; set; } = 0f; // Local E coordinate at Origin of 3D(X=0, Y=0)

    [Display(Name = "Local Northing (Y=0)", Order = 12)]
    public float LocalN { get; set; } = 0f; // Local N coordinate at Origin of 3D(X=0, Y=0)
    
    [Display(Name = "Global Easting (X=0)", Order = 13)]
    public float GlobalE { get; set; } = 0f; // Global E coordinate at Origin of 3D(X=0, Y=0)

    [Display(Name = "Global Northing (Y=0)", Order = 14)]
    public float GlobalN { get; set; } = 0f; // Local N coordinate at Origin of 3D(X=0, Y=0)
    
    [Display(Name = "Angle True North)", Order = 15)]
    public float AngleTrueNorth { get; set; } = 0f; // Angle (degree) of True North with respect to Plant North (Clock wise is +ve)
    
    [Display(Name = "X2>X1", Order = 16)]
    public bool PositiveScaleX { get; set; } = true; // While scaling the Key Plot Plan X2>X1?
    
    [Display(Name = "Y2>Y1", Order = 17)]
    public bool PositiveScaleY { get; set; } = true; // While scaling the Key Plot Plan Y2>Y1?
    
    public string CoordinateSystemJson { get; set; } //JSON of list of CoordinateSystems (base and any other like GLOBAL, etc.), 
    public List<CoordinateSystem> CoordinateSystems { get; set; }
    public int Order { get; set; }
    
    // Navigation properties
    public virtual ICollection<ProjectUserAssignment> UserAssignments { get; set; } = new List<ProjectUserAssignment>();
    public virtual ICollection<RoleMapping> RoleMappings { get; set; } = new List<RoleMapping>();
    public virtual ICollection<ProjectUserRole> UserRoles { get; set; } = new List<ProjectUserRole>();
}



// Base class with the Basic Info to be inherited by ALL classes
public class BaseInfo
{
    [ExcludeFromExcelImport] 
    [ExcludeFromExcelExport]
    [Display(Name = "UID", Order = 1)] public Guid UID { get; set; }
    [ExcludeFromExcelExport]
    
    [ExcludeFromExcelImport] 
    [Display(Name = "Project", Order = 2)] public string? ProjectId { get; set; }
    
    
    [ExcludeFromExcelExport]
    [Display(Name = "Option", Order = 3)] public string? OptionId { get; set; } = "base";
    
    [ExcludeFromExcelImport] 
    [Display(Name = "Sequence", Order = 0)]
    public int Seq { get; set; }
    
    [Display(Name = "Record", Order = 5)] public string? RecordId { get; set; } // Users own identification record # for reference
    
    [Required] [Display(Name = "Tag", Order = 6)]
    [RegularExpression(@"^([ .\/\a-zA-Z0-9]){3,100}$",
        ErrorMessage = "Tag should be 3 to 100 characters and can not contain any special characters")]
    public string? Tag { get; set; } //  Tag

    [Display(Name = "Tag Description", Order = 7)]
    [RegularExpression(@"^([ .\/\a-zA-Z0-9]){0,255}$",
        ErrorMessage = "Tag Description should be 3 to 255 characters and can not contain any special characters")]
    public string? TagDesc { get; set; } // Tag Description
    [Display(Name = "Remark", Order = 90)] public string? Remark { get; set; } = "";
    
    [ExcludeFromExcelImport] 
    [Display(Name = "Updated By", Order = 91)] public string UpdatedBy { get; set; } = "KP";
    
    [ExcludeFromExcelImport] 
    [Display(Name = "Update Date Time", Order = 92)] public DateTime UpdatedOn { get; set; }

    [ExcludeFromExcelImport] 
    [ExcludeFromExcelExport]
    public List<string> CellCSS { get; set; } // dynamically assign CSS for Table 
    
    [ExcludeFromExcelImport] 
    [ExcludeFromExcelExport]
    public bool Save2DB { get; set; } // if this item are programatically generated or to be saved to DB
}

// Base class with the method to be inherited
public interface IBaseMethod
{
    void Update();
}

public class Bus : BaseInfo, IBaseMethod
{
    public Bus()
    {
    }
    //Swing Bus or Slack Bus (aka reference bus): the voltage magnitude is known and voltage angle=0, real and reactive power not known
    //PV Bus or Generation bus: Real power and voltage magnitude known, reactive power and voltage angle unknown
    //PQ Bus or Load Bus: Real and reactive power known, voltage angle and magnitude unknown (motor)
    // other bus (switchboards) are "" unknown bus as none of the parameters voltage, angle, Real or Imaginary Power not known
    public string Category { get; set; } // Type of the bus (Swing/PV/PQ/"")
    [Display(Name = "Rated Voltage (V)")] public float VR { get; set; } // Bus Nominal/Rated Voltage in V

    [RegularExpression(@"^[0-9]{1,5}?[\.]{0,1}[0-9]{0,3}[ ]{0,2}[k]?[M]?[V]?[A]?$",
        ErrorMessage = "Short Circuit value with unit kA or MVA, value should be max 5 digit. Default unit 'kA'")]
    [Display(Name = "Rated Current (A)")] public float IR { get; set; } // Rated FLC (A)
    [IncludeExcelExport]
    [Display(Name = "Short Circuit")] public string SC { get; set; } // Bus Short Circuit (e.g., 25kA, 325 MVA, 200MVA, 25) default unit kA
    [Display(Name = "SC Current (kA)")] public float ISC { get; set; } // Bus Short Circuit Current in kA
    [Display(Name = "X/R Ratio")] public float XR { get; set; } = 10; // Bus X/R ratio (applicable for source
    [Display(Name = "Switchboard Tag")] public string SwbdTag { get; set; } // corresponding Switchboard Tag
    [Display(Name = "Switchboard Section")] public string Sec { get; set; } // Switchboard Section corresponding bus section A/B/.. or blank
    public float Vb { get; set; } // Base Voltage for this Bus in kV
    public Complex Vo { get; set; } // Bus Operating Voltage in PU
    public Complex Ybb { get; set; } // Self Admittance (sum of admittances of all connected branches)
    public Complex Sit { get; set; } = new(0, 0); // Sum of all connected loads at each Iteration
    public List<string> Cn { get; set; } = []; // Connected Bus's List of Tags
    public float SCkAaMax { get; set; } // Bus Max Short Circuit Current in kA Actual for SLD Display
    public float SCkAaMin { get; set; } // Bus MinShort Circuit Current in kA Actual for SLD Display
    public Complex Vit { get; set; } = new(1, 0); // Initialised Bus pu Iteration Voltage
    public int CordX { get; set; } // X-Coordinate of the Bus centre location in SLD in pixel
    public int CordY { get; set; } // Y-Coordinate of the Bus centre location in SLD in pixel

    public int Length { get; set; } // KeySLD drawing x-coordinate length of the bus, same as BusLF SLDL parameter in pixel

    public int SLDX { get; set; } // KeySLD order left to right, same as BusLF X parameter. Bus Section A is 0, B is 1, next board secA is 2..

    public int SLDY { get; set; } // KeySLD order top to bottom, same as BusLF Y parameter. Source is 0 and downstream boards are 1,2,3..

    public int SLDL { get; set; } // KeySLD drawing length of the bus, same as BusLF L parameter 1.2.3.4 depending on no of connections

    public int Network { get; set; } // Sequence no. of the corresponding independent network

    public List<string> SwingSources { get; set; } // list of swing source bus to which this bus is connected 

    // to store the last LF and SC result in the database to shorten no.of iterations
    public string VoJSON { get; set; } // final LF voltage Vo for saving to DB

    public List<SCBusVal> SCResult { get; set; } // string: all bus tag, Complex: corresponding final LF voltage for this bus as faulty bus

    //public List<Dictionary<string, Complex>> SCResult { get; set; } // string: all bus tag, Complex: corresponding final LF voltage for this bus as faulty bus
    public string SCResultJSON { get; set; } // for saving to DB

    public void Update()
    {
        ISC = SCFloat(SC, VR);
        Vit = new Complex(1, 0); // initialised
        Sit = new Complex(0, 0); // initialised
        // nothing further to update
    }
    public async Task UpdateAsync()
    {
        // Perform async operations if needed
        await Task.Run(() => Update());
        
        Debug.WriteLine($"UpdateAsync() called for {Tag}");
    }
    
    /// <summary>
    /// Converts short circuit string to ISC float value
    /// </summary>
    /// <param name="sc">Short circuit string (e.g., "100 kA", "50 MVA")</param>
    /// <param name="vr">Rated voltage in V</param>
    /// <returns>Short circuit current in kA</returns>
    float SCFloat(string sc, float vr)
    {
        const float defaultISC = 25f;
    
        if (string.IsNullOrWhiteSpace(sc))
        {
            return defaultISC;
        }

        try
        {
            // Remove whitespace and convert to uppercase
            sc = Regex.Replace(sc, @"\s+", "").ToUpper();

            // Match kA format
            var matchkA = Regex.Match(sc, @"^([0-9]{1,5}\.?[0-9]{0,3})KA$");
            if (matchkA.Success)
            {
                return float.Parse(matchkA.Groups[1].Value);
            }

            // Match MVA format
            var matchMVA = Regex.Match(sc, @"^([0-9]{1,5}\.?[0-9]{0,3})MVA$");
            if (matchMVA.Success)
            {
                var mva = float.Parse(matchMVA.Groups[1].Value);
            
                if (vr <= 0)
                {
                    Debug.WriteLine($"Invalid voltage {vr}V. Using default ISC {defaultISC}kA.");
                    return defaultISC;
                }
            
                // ISC = MVA * 1000000 / (√3 * VR_V)
                return mva * 1000000f / ((float)Math.Sqrt(3) * vr);
            }

            // No unit - assume kA
            if (float.TryParse(sc, out var value))
            {
                return value;
            }
        
            Debug.WriteLine($"Unable to parse '{sc}'. Using default {defaultISC}kA.");
            return defaultISC;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing '{sc}': {ex.Message}. Using default {defaultISC}kA.");
            return defaultISC;
        }
    }
}

public class SLDComponent : BaseInfo
{
    public string Type { get; set; } // cable, transformer, bus, etc.
    public string SLD { get; set; } // "Key" or any Switchboard/Bus Name
    public string PropertyJSON { get; set; } // JSON string of all the properties from teh Javascript
}

public class SLDXY : BaseInfo
{
    public SLDXY()
    {
    }

    public SLDXY(string projectId, string optionId, string sLD, string tag, string type, int cordX, int cordY,
        int length, string updatedBy, DateTime updatedOn)
    {
        UID = Guid.NewGuid();
        ProjectId = projectId;
        OptionId = optionId;
        SLD = sLD;
        Tag = tag;
        Type = type;
        CordX = cordX;
        CordY = cordY;
        Length = length;
        UpdatedBy = updatedBy;
        UpdatedOn = updatedOn;
    }

    public string SLD { get; set; } // "Key" or any Switchboard/Bus Name
    public string Type { get; set; } // cable, transformer, bus, etc.
    public int CordX { get; set; } // X-Coordinate of the Bus centre location in SLDs
    public int CordY { get; set; } // X-Coordinate of the Bus centre location in SLDs
    public int Length { get; set; } // Bus drawing length (Size) in Key SLD for Bus Bar
}

public class BusStudyResult : BaseInfo
{
    //public string StudyType { get; set; } // to store Study result in DB
    //public string StudyID { get; set; }
    //public string StudyDescription { get; set; } // to store Study result in DB

    // Tag is Bus Tag

    //to store last LF and SC result in the database to shorten no.of iterations
    // as per class Bus
    public Complex Vo { get; set; } // Bus Operating Voltage in PU in LF Study
    public string VoJSON { get; set; } // final LF voltage Vo for saving to DB

    public List<SCBusVal>
        SCResult
    {
        get;
        set;
    } // string : all bus tag, Complex : corresponding final LF voltage for this bus as faulty bus                                                         

    public string SCResultJSON { get; set; } // for saving to DB
}

public class SCBusVal
{
    public SCBusVal()
    {
    }

    public SCBusVal(string tag, Complex vo)
    {
        Tag = tag;
        Vo = vo;
    }

    public string Tag { get; set; } // Bus Tag
    public Complex Vo { get; set; } // final LF voltage of this Bus
}

/// <summary>
/// 
/// </summary>
public class Branch : BaseInfo
{

    [Display(Name = "Category", Order = 11)]
    public string Category { get; set; } // Category: Transformer, Cable, BusDuct, Reactor, OHL

    [Display(Name = "Bus From Tag", Order = 12)]
    public string BfT { get; set; } // Bus From Tag

    [Display(Name = "Bus To Tag", Order = 13)]
    public string BtT { get; set; } // Bus To Tag
    
    
    // derived parameters and hence should not be included in the .xls output file by default

    [ExcludeFromExcelExport] 
    public float Rl { get; set; } // Branch Resistance per km

    [ExcludeFromExcelExport]
    public float Xl { get; set; } // Branch Reactance (+ve for Inductive) per km

    [ExcludeFromExcelExport]
    public float R { get; set; } // Branch Resistance 

    [ExcludeFromExcelExport]
    public float? VR { get; set; } // Rated Voltage (V)
    [ExcludeFromExcelExport]
    public float? IR { get; set; } // Rated FLC (A)
    [ExcludeFromExcelExport]
    public float X { get; set; } // Branch Reactance (+ve for Inductive)
    [ExcludeFromExcelExport]
    public float Vb { get; set; } // Applicable Base Voltage for this Cable Branch, Volt
    [ExcludeFromExcelExport]
    public float Zb { get; set; } // Applicable Base Impedenace for this Cable Branch, Ohm
    [ExcludeFromExcelExport]
    public float Ib { get; set; } // Applicable Base Current for this Cable Branch, Ampere
    [ExcludeFromExcelExport]
    public Complex Ypu { get; set; } // Branch Admittance in PU
    [ExcludeFromExcelExport]
    public Complex Io { get; set; } // Branch Operating Current in PU BfT to BtT
    [ExcludeFromExcelExport]
    public float KW { get; set; } // Branch Operating KW
    [ExcludeFromExcelExport]
    public float KVAR { get; set; } // Branch Operating KVAR
    [ExcludeFromExcelExport]
    public float VRatio { get; set; } = 1; // Ratio of BfT rated voltage to BtT rated voltage (applicable for transformer) 
    //public float SCkAaMax { get; set; }   // Max SC in kA Actual for SLD Display, Contributing to BfT. Contribution to BtT = -1 * VRatio 
    //public float SCkAaMin { get; set; }   // Max SC in kA Actual for SLD Display, Contributing to BfT. Contribution to BtT = -1 * VRatio 
    public Branch()
    {
    }

    public Branch(string category, string tag, string bft, string btt, Complex ypu)
    {
        UID = Guid.NewGuid();
        Category = category;
        Tag = tag;
        BfT = bft;
        BtT = btt;
        Ypu = ypu;
        // BranchBusUpdate from the Razor Page where Bus List is already available
    }


}

public class BusDuctValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var item = (BusDuct)validationContext.ObjectInstance;

        // Example condition: 
        if (item.L < 5) return new ValidationResult("Error: Provide busduct length >= 5.");

        return ValidationResult.Success;
    }
}

[BusDuctValidation]
public class BusDuct : Branch
{


    [Display(Name = "Length (m)", Order = 14)]
    public float L { get; set; } // Branch Length, m
    
    [Display(Name = "Rated Current (A)", Order = 15)]
    public float IR { get; set; }
    [Display(Name = "Cross-section (sq.mm)", Order = 16)]
    public float Size { get; set; }
    
    public BusDuct()
    {
    }


}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExcludeFromExcelExportAttribute : Attribute
{
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IncludeExcelExportAttribute : Attribute
{
}

/// <summary>
/// Mark properties to exclude from Excel import
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExcludeFromExcelImportAttribute : Attribute { }


public class CableBranchValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var item = (CableBranch)validationContext.ObjectInstance;
        // Example condition: 
        if (item.L < 5) return new ValidationResult("Error: Provide cable branch length >= 5.");
        return ValidationResult.Success;
    }
}

[CableBranchValidation]
public class CableBranch : Branch
{
    public CableBranch()
    {
    }

    public CableBranch(string tag, string bft, string btt, string cblDesc, float l)
    {
        if (string.IsNullOrEmpty(CblDesc)) CblDesc = "4Cx16";
        Category = "Cable";
        //N = SystemStudyPage.Cables.Count; 
        UID = Guid.NewGuid();
        Tag = tag;
        L = l;
        BfT = bft;
        BtT = btt;
        CblDesc = cblDesc;

        // BranchBusUpdate from the Razor Page where Bus List is already available
    }
    [IncludeExcelExport]
    [Display(Name = "Length (m)", Order = 14)]
    public float L { get; set; } // Branch Length, m
    
    [Display(Name = "Cable Description", Order = 15)]
    [RegularExpression(
        @"^([1-9][R][x])?[0-9][C][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)[ ]{0,2}[A-Z]{0,4}[a-z]{0,4}$",
        ErrorMessage = "[0-9]Rx[0-9]Cx??? format only acceptable (e.g., 2Rx3Cx185 Al or 7Cx1.5")]
    public string CblDesc { get; set; } // Cable Size Description (Similar to Excel Input)
    
    // below are the generated field and therefore not to be included in downloaded .xls file
    [ExcludeFromExcelExport]
    public int Run { get; set; } = 1; // Branch Run (no. of conductor per phase) it may be >1 for cable, for BusDuct = 1
    [ExcludeFromExcelExport]
    public float VdR { get; set; } // Running VoltageDrop
    [ExcludeFromExcelExport]
    public float AlVdR { get; set; } = 4f; // Running VoltageDrop  - Allowable (in %)
    [ExcludeFromExcelExport]
    public Complex VdRC { get; set; } // Running VoltageDrop Copmplex
    [ExcludeFromExcelExport]
    public float VdS { get; set; } // Starting VoltageDrop
    [ExcludeFromExcelExport]
    public float AlVdS { get; set; } = 15f; // Starting VoltageDrop - - Allowable (in %)
    [ExcludeFromExcelExport]
    public Complex VdSC { get; set; } // Starting VoltageDrop Complex
    [ExcludeFromExcelExport]
    public float Sp { get; set; } //  Power Cable size of phase conductor in sq.mm
    [ExcludeFromExcelExport]
    public float Sn { get; set; } //  Power Cable size of Neutral conductor in sq.mm
    [ExcludeFromExcelExport]
    public float Spe { get; set; } //  Power Cable size of PE conductor in sq.mm
    [ExcludeFromExcelExport]
    public int Core { get; set; } = 3; // Cable core nos: 3C (3P+N+E), 3C (3C+PE/N) 3.5C (3C+1/2N), 3C (3P or 2P+N/PE), 2C (P+N) or 1C cable

 

    //public float Vb { get; set; } // Applicable Base Voltage for this Cable Branch, Volt
    //public float Zb { get; set; } // Applicable Base Impedenace for this Cable Branch, Ohm
    //public float Ib { get; set; } // Applicable Base Current for this Cable Branch, Ampere


}

public class TransformerValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var transformer = (Transformer)validationContext.ObjectInstance;

        // Example condition: If V1 is less than V2, alert
        if (transformer.V1 < transformer.V2)
            return new ValidationResult("Error: V1 must be greater than or equal to V2.");

        return ValidationResult.Success;
    }
}

[TransformerValidation]
public class Transformer : Branch
{
    

    [Required] [Display(Name = "% Impedance", Order = 16)]
    [RegularExpression(@"^[+]?((\d{1,2}(\.\d{1,3})?)|(0\.\d{1,3})|(\.\d{1,3}))$",
        ErrorMessage = "Invalid format. Value must be a number with up to 2 digits before and 3 digits after the decimal point (e.g., 12.345, 0.123, .123).")]
    [Range(1.0, 30.0, ErrorMessage = "% Impedance must be between 1% and 30%.")]
    public float Z { get; set; } // Impedance in percentage
    [Display(Name = "Primary Voltage", Order = 17)]
    public float V1 { get; set; } // From (1) side (BfT) Open Circuit Voltage 
    [Display(Name = "Secondary Voltage", Order = 18)]
    public float V2 { get; set; } // To (2) (BtT) side Open Circuit Voltage

    // BfT and BtT already defined
    //public string BfT { get; set; }     // Bus V1 (From side) Tag, it would be node
    //public string BtT { get; set; }     // Bus V2 (To side) Tag, it would be node
    [Display(Name = "X/R Ratio", Order = 17)]
    public float XR { get; set; } // X/R Ratio

    [Display(Name = "Rated kVA", Order = 18)]
    public float KVA { get; set; } // kVA

    [Display(Name = "Force kVA", Order = 19)]
    public float KVAF { get; set; } // KVA Forced Cooling Rating
    
}

public class Switch : BaseInfo
{
    [Display(Name = "Rated Voltage (V)")]
    public float VR { get; set; } // Rated Voltage (V)
    [Display(Name = "Rated Current (A)")]
    public float IR { get; set; } // Rated FLC (A)
    [Display(Name = "Short Circuit Rating (X/XkA/XMVA)")]
    public string SC { get; set; } // Bus Short Circuit (e.g., 25kA, 325 MVA, 200MVA, 25) default unit kA
    public float ISC { get; set; } // Bus Short Circuit Current in kA (calculated)

    public string T1 { get; set; } // Bus Tag at Connection 1
    public string T2 { get; set; } // Bus Tag at Connection 2
    [Display(Name = "T1-T2 Closed")]
    public bool Conn12 { get; set; } = true; // true -> Switch is Closed

    public string T3 { get; set; } = ""; // Bus Tag at Connection 3 (for 3-Way Switch)
    [Display(Name = "T1-T3 Closed")]
    public bool Conn13 { get; set; } // Connection between T1 & T3: true -> Switch is Closed
    [Display(Name = "T2-T3 Closed")]
    public bool Conn23 { get; set; } // Connection between T2 & T3: true -> Switch is Closed

    public void Update()
    {
        if (T3 != "")
        {
            // any one connection can be true
            if (Conn12)
            {
                Conn23 = false;
                Conn13 = false;
            }

            if (Conn23)
            {
                Conn12 = false;
                Conn13 = false;
            }

            if (Conn13)
            {
                Conn23 = false;
                Conn12 = false;
            }
        }
        // nothing further to update
    }
}

public class Load : BaseInfo
{
    public Load()
    {
    }

    public Load(string category, string tag, string bft, Complex scpu, float vr, float dr)
    {
        UID = Guid.NewGuid();
        Category = category;
        Tag = tag;
        Scpu = scpu;
        BfT = bft;
        VR = vr;
        DR = dr;
        P = (float)Scpu.Real;
        Q = (float)Scpu.Imaginary;
        PfO = P / (float)Scpu.Magnitude;
        //N = SystemStudyPage.Loads.Count;
    }

    public string WBS { get; set; }
    public string Discipline { get; set; }
    public string Package { get; set; } // Description of Corresponding Package
    public string Category { get; set; } = "Load"; // Motor, Heater, Capacitor, LumpLoad, etc.
    public string Maturity { get; set; } // Data Maturity Status : Estimate/Vendor/As-built/..

    [Display(Name = "Location & Geometry", Order = 20)]
    [RegularExpression(
        @"^E:[-]?[0-9]{1,4}(\.[0-9]{0,3})?([ ,]+)N:[-]?[0-9]{1,4}(\.[0-9]{0,3})?([ ,]+)U:[-]?[0-9]{1,4}(\.[0-9]{0,3})?(([ ,]+)LE:[0-9]{1,4}(\.[0-9]{0,3})?([ ,]+)LN:[0-9]{1,4}(\.[0-9]{0,3})?([ ,]+)LU:[0-9]{1,4}(\.[0-9]{0,3})?)?$",
        ErrorMessage =
            "E:?,N:?,U?, LE:?,LN:?,LU:?,  format only acceptable (e.g., 'E:1075.985,N:1276.31 U:0.785,LE:1.57 LN:2.84 LU:1.57')]")]
    public string LocationSize { get; set; } // Location in E: N: U: (centre point) and Size LE LN LU (B: D: H: T:), Cord = "Global" (Global coordinate system or local01/local02)
    [Display(Name = "Supply Class (Normal/Emergency/UPS)", Order = 21)]
    public string Supply { get; set; } // Supply Class "Normal"/ "Emergency", "UPS"

    [Display(Name = "Fed From (Bus Section Tag)", Order = 22)]
    public string BfT { get; set; } // Source (From) Bus - Assigned Power Source Unique Switchboard Bus Tag e.g., SS08-PMCC-04-BusA
    [Display(Name = "Feeder Type (Block Diagram)", Order = 23)]
    public string FeederType { get; set; } // Feeder Type / BlockDiagram
    [Display(Name = "Duty Type (C/I/S/Sp)", Order = 24)]
    public string Duty { get; set; } = "C"; // "Contineous (C)" / "Standby (S)" / Spare (Sp) / Intermittent (I)/ Custom Duty / etc.
    [Display(Name = "Rating", Order = 25)]
    public float R { get; set; } = 1f;  //  Rating (Output)
    [Display(Name = "Unit", Order = 26)]
    public string Unit { get; set; } = "kW"; // Rating Unit (A, VA, kVAR, MW)
    [Display(Name = "Voltage", Order = 27)]
    public float VR { get; set; } = 400; // Rated Voltage (V)
    [Display(Name = "Absorbed Power", Order = 28)]
    public float PA { get; set; } // Absorbed Power (kW/kVA/.. = (load factor * rated power)
    [Display(Name = "PF", Order = 29)]
    public float Pf { get; set; } = 0.85f; // Rated Power Factor (-1<Pf<1)
    [Display(Name = "PF Table", Order = 30)]
    public string PfJSON { get; set; } =
        "[]"; // JSON String in for full load, 3/4 load and 1/2 load in decimal To calculate power factor at operating load
    [Display(Name = "Eff", Order = 31)]
    public float Eff { get; set; } = 100; // Efficiency in %(.001<Eff<100)
    [Display(Name = "Eff. Table", Order = 32)]
    public string EffJSON { get; set; } =
        "[]"; // JSON String in for full load, 3/4 load and 1/2 load in % To calculate efficiency at operating load
    [Display(Name = "Phase", Order = 33)]
    public byte Ph { get; set; } = 3; // Phase
    [Display(Name = "Ist/Iflc", Order = 34)]
    public float Ist { get; set; } = 6; // Ist/Iflc in case of motor load etc.
    [Display(Name = "Pf(starting)", Order = 35)]
    public float Pfst { get; set; } = 0.25f; // Starting Power Factor (0.01 to 1) in case of motor load etc.
    [Display(Name = "Dynamic Ratio", Order = 36)]
    public float DR { get; set; } =
        1; // Dynamic Load Ratio for Lump Loads: 1 in case of full dynamic (motor) , 0.5 in case of 1/2 dynamic(const kW) and 1/2 static(const R))

    public float AM { get; set; } =
        1f; // Ampacity Margin for Cable Sizing - default 0, for 10% margin, apply factor 1.1f

    public float L { get; set; } // Route Length m for Cable Sizing Calculation (consider margin in Cable Schedule module)

    public float Isc { get; set; } // SC Current for Cable Sizing
    
    [Display(Name = "Cable Size (Manual)", Order = 37)]
    [RegularExpression(
        @"^([1-9][R][x])?[0-9][C][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)[ ]{0,2}[A-Z]{0,4}[a-z]{0,4}$",
        ErrorMessage = "[0-9]Rx[0-9]Cx??? format only acceptable (e.g., 2Rx3Cx185 or 7Cx1.5")]
    public string CblDescM { get; set; } // Cable Size Description (Similar to Excel Input) - Manual
    // For Cable Schedule purpose True if Cable sizing is Auto, False if Cable size as as per manually entered
    [Display(Name = "Auto Sizing)", Order = 38)]
    public bool CblAuto { get; set; } = true; 
    public int Pole { get; set; } = 4; // No of Poles for Motors


    // derived properties (not in SQL Server)

    public float LF { get; set; } = 80f; // Load Factor in %
    public float PR { get; set; } = 1; // Rated Power kW (e.g., Output of motor)

    public float DF { get; set; } = 100; // Diversity Factor (DF) in percentage Default is 100%

    //public string LocationSize { get; set; } // Location in E: N: U: (centre point) and Size B: D: H: T:, Cord = "Global" (Global coordinate system or local01/local02)
    public Vector3 Location { get; set; } // Location Vector (centre point)
    public Vector3 Size { get; set; } // Shape (assumed recangle) Breadth x, Depth y, Height z 
    public float T { get; set; } = 0; // Rotaion in deg 
    public float V { get; set; } // Operating Voltage (V)
    public Complex Vc { get; set; } // Operating Voltage Complex
    public float PfO { get; set; } = 1; // Operating Power Factor (-1<Pf<1) @ Operating condition
    public float EffO { get; set; } = 100; // Operating Efficiency (.001<Eff<100) @ Operating condition
    public float P { get; set; } = 1; // Drawn/Operating Power kW
    public float Ppu { get; set; } = 1; // Drawn/Operating Power (pu)
    public float Q { get; set; } // Drawn/Operating Reactive Power kVAR
    public float Qpu { get; set; } = 0; // Drawn/Operating kVAR (pu)
    public float SR { get; set; } = 1; // Rated Aparent Power (kVA)
    public float S { get; set; } = 1; // Drawn/Operating Aparent Power (kVA)
    public float Spu { get; set; } = 1; // Drawn/Operating Aparent Power (pu)
    public Complex Sc { get; set; } // Drawn/Operating Aperent Power Complex (kVA)
    public Complex Scpu { get; set; } // Drawn/Operating Aperent Power Complex (pu)
    public float IR { get; set; } // Rated FLC (A)
    public float I { get; set; } // Drawn/Operating Current (A)
    public Complex Ic { get; set; } // Drawn/Operating Current Complex
    public string CblDesc { get; set; } // Cable Size Description (Similar to Excel Input) - Selected
    public float VdR { get; set; } // Running VoltageDrop  - Selected
    public float AlVdR { get; set; } = 3f; // Running VoltageDrop  - Allowable (in %)
    public Complex VdRC { get; set; } // Running VoltageDrop Copmplex - Selected
    public float VdS { get; set; } // Starting VoltageDrop - Selected
    public float AlVdS { get; set; } = 15f; // Starting VoltageDrop - - Allowable (in %)
    public Complex VdSC { get; set; } // Starting VoltageDrop Complex - Selected
    public int Rn { get; set; } = 1; // Power Cable Run (no. of cores per phase) - Selected
    public double Sp { get; set; } //  Power Cable size of phase conductor - Selected
    public double Sn { get; set; } //  Power Cable size of Neutral conductor - Selected
    public double Spe { get; set; } //  Power Cable size of PE conductor - Selected
    // Cable core nos: 5C (3P+N+E), 4C (3C+PE/N) 3.5C (3C+1/2N), 3C (3P or 2P+N/PE), 2C (P+N) or 1C cable - Selected
    public float C { get; set; } = 3; 
    public string Mat { get; set; } // Cable Material "Cu" or "Al" - Selected
    public float VdRM { get; set; } // Running VoltageDrop - Manual
    public Complex VdRCM { get; set; } // Running VoltageDrop Copmplex - Manual
    public float VdSM { get; set; } // Starting VoltageDrop - Manual
    public Complex VdSCM { get; set; } // Starting VoltageDrop Complex - Manual
    public int RnM { get; set; } = 1; // Power Cable Run (no. of cores per phase) - Manual
    public double SpM { get; set; } //  Power Cable size of phase conductor - Manual
    public double SnM { get; set; } //  Power Cable size of Neutral conductor - Manual
    public double SpeM { get; set; } //  Power Cable size of PE conductor - Manual
    public float CM { get; set; } =
        3; // Cable core nos: 5C (3P+N+E), 4C (3C+PE/N) 3.5C (3C+1/2N), 3C (3P or 2P+N/PE), 2C (P+N) or 1C cable - Manual
    public string MatM { get; set; } // Cable Material "Cu" or "Al" - Manual
    public float VdRA { get; set; } // Running VoltageDrop - Auto
    public Complex VdRCA { get; set; } // Running VoltageDrop Copmplex - Auto
    public float VdSA { get; set; } // Starting VoltageDrop - Auto
    public Complex VdSCA { get; set; } // Starting VoltageDrop Complex - Auto
    public int RnA { get; set; } = 1; // Power Cable Run (no. of cores per phase) - Auto
    public float SpA { get; set; } //  Power Cable size of phase conductor - Auto
    public float SnA { get; set; } //  Power Cable size of Neutral conductor - Auto
    public float SpeA { get; set; } //  Power Cable size of PE conductor - Auto
    public float CA { get; set; } =
        3; // Cable core nos: 5C (3P+N+E), 4C (3C+PE/N) 3.5C (3C+1/2N), 3C (3P or 2P+N/PE), 2C (P+N) or 1C cable - Auto
    public string CblDescA { get; set; } // Cable Size Description (Similar to Excel Input) Selected - Auto
    public string MatA { get; set; } // Cable Material "Cu" or "Al" - Auto

    //public BlockDiagramS BlockDiagram;  // applicable Block Diagram
    // list of connected IDs (source, end equipment, aux equipment.etc.) inline with the applicable BlockDiagram
    public List<Guid> ConnectedEquipmentId { get; set; }
    //public List<EL> LoadScenario { get; set; }  // [0] is the base scenario Electrical Parameters (power ratings), other for different loading

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which Location (LocationAndSizeString) is stored and accordingly CentrePointV is calculated
    //public string CoordSystem { get; set; }
    //public string LocationAndSizeString { get; set; } = "";
    public float Autonomy { get; set; } = 0; // Autonomy Time for UPS fed feeders (minute)
    public bool ReAcce { get; set; } // Re-acceleration Required : yes/no
    public bool ReStart { get; set; } // ReStart Required : yes/no

    //  LoadHead Tag for this Load e.g., Regen Gas Compressor - PDO for future search/refrence
    public string LoadHead { get; set; } 
    public string PhaseIdentifier { get; set; }
    public string RunSequenceIdentifier { get; set; }
    public string LoadFilterCategory1 { get; set; } // Reserved Category - Area
    public string LoadFilterCategory2 { get; set; } // Reserved Category - Discipline
    public string LoadFilterCategory3 { get; set; } // Reserved Category

    // LoadUpdate is implemented in LayoutFunction.cs
    
}

public class CableData
{
    [Display(Name = "ID", Order = 1)]
    public Guid UID { get; set; }
    [Display(Name = "Tag", Order = 2)]
    public string Tag { get; set; }
    [Display(Name = "Make", Order = 3)]
    public string Make { get; set; }
    [Display(Name = "Type", Order = 4)]
    public string Type { get; set; }
    [Display(Name = "PhaseNo", Order = 5)]
    [RegularExpression(@"[+]?((\d{0,2})|(\d{0,2}[.]\d{0,2})|([.]\d{0,3}))$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    //[Display(Name = "Efficiency at absorbed power")]
    public int PhaseNo { get; set; }
    [Display(Name = "PhaseSize", Order = 6)]
    [RegularExpression(
        @"^([1-9][R][x])?[0-9][C][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10]|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)$",
        ErrorMessage = "[0-9]Rx[0-9]Cx??? format only acceptable (e.g., 2Rx3Cx185 or 7Cx1.5")]
    public float PhaseSize { get; set; }
    [Display(Name = "NeutralNo", Order = 7)]
    public int NeutralNo { get; set; }
    [Display(Name = "NeutralSize", Order = 8)]
    public float NeutralSize { get; set; }
    [Display(Name = "PENo", Order = 9)]
    public int PENo { get; set; }
    [Display(Name = "PESize", Order = 10)]
    public float PESize { get; set; }
    [Display(Name = "Uv", Order = 11)]
    public float Uv { get; set; }
    [Display(Name = "Uvo", Order = 12)]
    public float Uvo { get; set; }
    [Display(Name = "Uvm", Order = 13)]
    public float Uvm { get; set; }
    [Display(Name = "SpecVoltage", Order = 14)]
    [Required] [RegularExpression(
        @"^[0-9]{1,4}?([\.]{0,1}[0-9]{0,4})\/[0-9]{1,4}?([\.]{0,1}[0-9]{0,4})[(][0-9]{1,4}?([\.]{0,1}[0-9]{0,4})[)]$",
        ErrorMessage = "Enter in only acceptable format(e.g., (3.6/6(7.2)")]
    public string SpecVoltage { get; set; }
    [Display(Name = "Insulated", Order = 15)]
    public bool Insulated { get; set; }
    [Display(Name = "InsulationMaterial", Order = 16)]
    public string InsulationMaterial { get; set; }
    [Display(Name = "ConductorMaterial", Order = 17)]
    public string ConductorMaterial { get; set; } // Cu/Al
    [Display(Name = "Armoured", Order = 18)]
    public bool Armoured { get; set; }
    [Display(Name = "ArmourMaterial", Order = 19)]
    public string ArmourMaterial { get; set; }
    [Display(Name = "FlameRetardent", Order = 20)]
    public bool FlameRetardent { get; set; }
    [Display(Name = "FireResistant", Order = 21)]
    public bool FireResistant { get; set; }
    [Display(Name = "LSZH", Order = 22)]
    public bool LSZH { get; set; }
    [Display(Name = "LeadSheathed", Order = 23)]
    public bool LeadSheathed { get; set; }
    [Display(Name = "Spec Description", Order = 24)]
    [Required]
    [RegularExpression(
        @"^([0-9]{1,2}Cx(0\.5|1|1\.5|2\.5|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000))(\+[0-9]Nx(0\.5|1|1\.5|2\.5|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000))?(\+[0-9](PE|E)x(0\.5|1|1\.5|2\.5|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000))?$",
        ErrorMessage = "Enter in only acceptable format (e.g., 3Cx185+1Nx185+1Ex95, 3Cx185+1Nx95, 3Cx185+3PEx185, or 7Cx1.5)")]
    public string SpecDescription { get; set; }
    [Display(Name = "Outer Diameter", Order = 25)]
    public float OuterDiameter { get; set; }
    [Display(Name = "Diameter below Armour", Order = 26)]
    public float DiameterbelowArmour { get; set; }
    [Display(Name = "RDC", Order = 27)]
    public float RDC { get; set; } // Conductor DC Resistance/km at 20 deg C
    [Display(Name = "RAC", Order = 28)]
    public float RAC { get; set; } // Conductor AC Resistance/km at 75 deg C
    [Display(Name = "XAC", Order = 29)]
    public float XAC { get; set; } // Conductor Reactance/km at 75 deg C
    [Display(Name = "UnitRate", Order = 30)]
    public float UnitRate { get; set; } // Unit rate/m in USD
    [Display(Name = "LMECuFactor", Order = 31)]
    public float LMECuFactor { get; set; } // Cu weight factor MT/m
    [Display(Name = "LMECuRate", Order = 32)]
    public float LMECuRate { get; set; } // Base LME Cu rate USD/MT
    [Display(Name = "LMEAlFactor", Order = 33)]
    public float LMEAlFactor { get; set; } //  Al weight factor MT/m
    [Display(Name = "LMEAlRate", Order = 34)]
    public float LMEAlRate { get; set; } // Base LME Al rate USD/MT
    [Display(Name = "LMEPbFactor", Order = 35)]
    public float LMEPbFactor { get; set; } // Lead  weight factor MT/m
    [Display(Name = "LMEPBRate", Order = 36)]
    public float LMEPBRate { get; set; } // Base LME Pb rate USD/MT
    [Display(Name = "Weight", Order = 37)]
    public float Weight { get; set; } // Cable Weight kg/m
    [Display(Name = "DrumSizeCatalogue", Order = 38)]
    public string DrumSizeCatalogue { get; set; }
    [Display(Name = "StdDrumLength", Order = 39)]
    public int StdDrumLength { get; set; } // standard drum length in m
    [Display(Name = "StdDrumLengthMin", Order = 40)]
    public int StdDrumLengthMin { get; set; } // standard drum length in m
    [Display(Name = "StdDrumLengthMax", Order = 41)]
    public int StdDrumLengthMax { get; set; } // standard drum length in m
    [Display(Name = "GlandSize", Order = 42)]
    public string GlandSize { get; set; } // 
    [Display(Name = "AmpicityGround", Order = 43)]
    public float AmpicityGround { get; set; }
    [Display(Name = "AmpicityDuct", Order = 44)]
    public float AmpicityDuct { get; set; }
    [Display(Name = "AmpicityAir", Order = 45)]
    public float AmpicityAir { get; set; }
}

public class MotorData
{
    public MotorData()
    {
    }

    public MotorData(
        string make
        , string frameSize
        , double ratedVoltage
        , int frequency
        , int pole
        , string efficiencyClass
        , double ratedkW
        , string powerFactorJSON
        , string efficiencyJSON
        , double lRC
        , double lRPF
    )
    {
        UID = Guid.NewGuid();
        Make = make;
        FrameSize = frameSize;
        RatedVoltage = ratedVoltage;
        Frequency = frequency;
        Pole = pole;
        EfficiencyClass = efficiencyClass;
        RatedkW = ratedkW;
        PowerFactorJSON = powerFactorJSON;
        EfficiencyJSON = efficiencyJSON;
        LRC = lRC;
        LRPF = lRPF;
    }

    public Guid UID { get; set; }
    public string Make { get; set; }
    public string FrameSize { get; set; }
    public double RatedVoltage { get; set; } // Voltage
    public int Frequency { get; set; } // Frequency
    public int Pole { get; set; } // Pole
    public string EfficiencyClass { get; set; }
    public double RatedkW { get; set; } // kW
    public string PowerFactorJSON { get; set; }
    public double[][] PowerFactorTable { get; set; }
    public string EfficiencyJSON { get; set; }
    public double[][] EfficiencyTable { get; set; }
    public double LRC { get; set; }
    public double LRPF { get; set; }
}


// Custom converter for floats/doubles to round to 4 decimal places
public class RoundingFloatConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Simply read the float as usual during deserialization
        return reader.GetSingle();
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        // Round to 4 decimal places during serialization
        writer.WriteNumberValue(MathF.Round(value, 4)); // Use Math.Round for double
    }
}