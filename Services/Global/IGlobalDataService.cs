using System.Collections.Generic;
using System.Diagnostics;

namespace ElDesignApp.Services.Global;


using System.Numerics;
using ElDesignApp.Models;


public interface IGlobalDataService
{
    string? LoginUser { get; set; }
    
    Project? SelectedProject { get; set; } // Or whatever properties GlobalData holds
    
    event Action? OnProjectChanged;
    
    // For Segments Rendering
    /// <summary>
    /// Event fired when the segment page tab changes
    /// Used by SharedSceneHost to draw/hide segments based on active tab
    /// </summary>
    event Action<int>? OnSegmentTabChanged;
    
    /// <summary>
    /// Notify listeners that the segment tab has changed
    /// </summary>
    void NotifySegmentTabChanged(int tabIndex);
    
        
    void SetSelectedProject(Project? project);
        
    int ClickCount { get; set; }

    bool dbConnected { get; set; }

    // segment page
    int SegmentPageTab { get; set; }
    double SegRendererWidth { get; set; }
    double SegRendererHeight { get; set; }

    /// <summary>Gets or sets the counter value for the LF component.</summary>
    int CounterLF { get; set; }

        /// <summary>Gets or sets the  global data.</summary>
    int IterationLF { get; set; }

    /// <summary>Gets or sets the  global data.</summary>
    float PrecisionLF { get; set; }

    /// <summary>Gets or sets the  global data.</summary>
    int IterationSC { get; set; }

    /// <summary>Gets or sets the  global data.</summary>
    float PrecisionSC { get; set; }

    /// <summary>Gets or sets the  global data.</summary>
    bool LoadContribution { get; set; }

    // for Drawing SLD in canvas
    /// <summary>Gets or sets the global data.</summary>
     int xgridSize { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int ygridSize { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int leftSpacing { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int topSpacing { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int xGridSpacing { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int yGridSpacing { get; set; }

    //
    /// <summary>Gets or sets the global data.</summary>
    string UpdatedBy { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    string selectedPage { get; set; }
    // Add this event
    public event Action OnHeaderChanged;
    
    public bool Show3D { get; set; }

    // Call this whenever you update the header data
    public void NotifyHeaderChanged();

    /// <summary>Gets or sets the global data.</summary>
    string sceneCurrent { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    SceneInfo sceneDataCurrent { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    SceneInfo sceneDataCable { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    SceneInfo sceneDataLoad { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    SceneInfo sceneDataSegment { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<DBMaster>? DBMasters { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<PlotPlan>? PlotPlans { get; set; }
    
    List<Vector3>? RefPoints { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    string XYEN { get; set; } 

    /// <summary>Gets or sets the global data.</summary>
    List<SegmentResult>? SegmentResults { get; set; }   

    /// <summary>Gets or sets the global data.</summary>
    List<Segment>? RawSegments { get; set; }
    /// <summary>Gets or sets the global data.</summary>
    List<Segment>? Segments { get; set; }
    List<Trench>? Trenches { get; set; }
    List<Structure>? Structures { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Segment>? Trays { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Bend>? Bends { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Tee>? Tees { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Cross>? Crosses { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Sleeve>? Sleeves { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Node>? Nodes { get; set; } 

    /// <summary>Gets or sets the global data.</summary>
    List<Spacing>? Spacings { get; set; } 

    /// <summary>Gets or sets the global data.</summary>

    List<Load>? Loads { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Cable>? Cables { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Equipment>? Equipments { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Board>? Boards { get; set; } 

    // system study page
    /// <summary>Gets or sets the global data.</summary>
    float Sb { get; set; }

    /// <summary>Gets or sets the global data.</summary>
     List<Bus>? Buses { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Transformer>? Transformers { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<Switch>? Switches { get; set; } 
    
    List<BusBarLink>? BusBarLinks { get; set; } 
    List<Fuse>? Fuses { get; set; } 

    /// <summary>Gets or sets the global data.</summary>
    List<CableBranch>? CableBranches { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<BusDuct>? BusDucts { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<SLDXY>? SLDXYs { get; set; } 

    /// <summary>Gets or sets the global data.</summary>
    List<BusStudyResult>? BusStudyResults { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<SLDComponent>? SLDComponents { get; set; }


    //public static List<LoadMaster> ImportedLoads { get; set; }

    //Catalogue Data
    /// <summary>Gets or sets the global data.</summary>
    List<MotorData> MotorData { get; set; }

    /// <summary>Gets or sets the a global data.</summary>
    List<CableData> CableData { get; set; } 
    // cable schedule page
    /// <summary>Gets or sets the global data.</summary>
    string DefaultPhaseIdentifier { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    string DefaultRunSequenceIdentifier { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<float> DefaultAvailableCableSizes1C { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    List<float> DefaultAvailableCableSizesMC { get; set; }


    // segment page

    /// <summary>Gets or sets the global data.</summary>
    List<string> displayItemsLayoutComponentRawSegment { get; set; } 

    /// <summary>Gets or sets the global data.</summary>
    List<string> displayItemsLayoutComponentGeneratedSegment { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    string SegmentPage_locationString { get; set; } 


    // layout page
    /// <summary>Gets or sets the global data.</summary>
    List<string> displayItemsLayoutComponent { get; set; }
    //public static List<string> displayItemsLayoutComponent = new() { "Plot", "Segment", "Tee", "Cross", "Node", "Load", "Cable" };
    //["Plot", "IsolatedSegment", "Segment", "Bend", "Tee", "Cross", "Node", "Sleeve", "Load", "Cable", "Board"];
    /// <summary>Gets or sets the aglobal data.</summary>
    double segmentOpacity { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    int[] isolatedSegmentColor { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] segmentColorNeutral { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] segmentColorLV { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] segmentColorHV { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] bendColor { get; set; }
    
    int[] equipmentColor { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] teeColor { get; set; } 

    /// <summary>Gets or sets the global data.</summary>
    int[] crossColor { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] nodeColor { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] segmentColor { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] sleeveColor { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    int[] cableColor { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    int CurveStep { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    int BendStep { get; set; }


    /// <summary>Gets or sets the aglobal data.</summary>
    List<Segment> IsolatedSegments { get; set; } 

    //public static List<SegmentResult> SegmentResults { get; set; } 
    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> LadderCategories { get; set; }

    //public static List<Cable> Cables { get; set; } 
    //
    /// <summary>Gets or sets the aglobal data.</summary>
    bool CablesReadInProgress { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    bool LoadMastersReadInProgress { get; set; }

    //
    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> RawSegmentJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> IsolatedSegmentJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> SegmentJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> BendJSON { get; set; } 

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> TeeJSON { get; set; } 

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> CrossJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> SleeveJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> NodeJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> LoadJSON { get; set; } 

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> CableJSON { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>

    int tubeTubularSegments { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    int tubeRadialSegments { get; set; } 

    /// <summary>Gets or sets the aglobal data.</summary>
    double distanceMargin { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    double nodeGapLimit { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    double MarginSide1 { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    double MarginSide2 { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    double MarginSpare { get; set; }

    //
    /// <summary>Gets or sets the aglobal data.</summary>
    string LoadListComponent_locationString { get; set; }

    /// <summary>Gets or sets the aglobal data.</summary>
    List<string> CableCategoryList { get; set; }


    // load list page

    /// <summary>Gets or sets the aglobal data.</summary>
    string LoadListPage_displayMsgAction { get; set; } 

    /// <summary>Gets or sets the aglobal data.</summary>
    bool LoadListPage_showInputFile { get; set; }

    //public static List<LoadMaster> LoadMasters { get; set; } 
    //public static List<Board> Boards { get; set; }
    //public static List<Equipment> Equipments { get; set; } 
    //public static List<LoadHeadS> LoadHeadSs { get; set; }

    //public static List<BlockDiagram> BlockDiagrams { get; set; }
    /// <summary>Gets or sets the aglobal data.</summary>
    string exampleLoadJSON { get; set; }
        
        
    // SLD Page
    int SLDPageTab { get; set; }  
        
}


public enum ModalDialogType // <-- Defined directly in the namespace
{
    Ok,
    OkCancel,
    DeleteCancel,
    SaveDeleteCancelOk
}



public class GlobalDataService : IGlobalDataService
    {
        public string LoginUser { get; set; }
        
        private Project? _selectedProject;
        /// <summary>Gets or sets the global data.</summary>
        public Project? SelectedProject
        {
            get => _selectedProject;
            set => _selectedProject = value;
        }
        public event Action? OnProjectChanged;
        public void SetSelectedProject(Project? project)
        {
            if (_selectedProject?.UID != project?.UID)
            {
                _selectedProject = project;
                OnProjectChanged?.Invoke();  // Fire the event!
                Debug.WriteLine($"GlobalDataService: OnProjectChanged fired for {project?.Tag}");
            }
        }
        
        public event Action<int>? OnSegmentTabChanged;
        
        public void NotifySegmentTabChanged(int tabIndex)
        {
            OnSegmentTabChanged?.Invoke(tabIndex);
        }


        public int ClickCount { get; set; }

        public int SegmentPageTab { get; set; } = 1;
        public double SegRendererWidth { get; set; }
        public double SegRendererHeight { get; set; }
        
        public  bool dbConnected { get; set; }

    /// <summary>Gets or sets the  global data.</summary>
    public int CounterLF { get; set; } = 10; //10

    /// <summary>Gets or sets the  global data.</summary>
    public int IterationLF { get; set; }= 500; // 500

    /// <summary>Gets or sets the  global data.</summary>
    public float PrecisionLF { get; set; }= 0.000001f; // 0.00001;

    /// <summary>Gets or sets the  global data.</summary>
    public int IterationSC { get; set; } = 10000; // 1000

    /// <summary>Gets or sets the  global data.</summary>
    public float PrecisionSC { get; set; }= 0.00001f; // 0.001

    /// <summary>Gets or sets the  global data.</summary>
    public bool LoadContribution { get; set; }= false; // load contributioin for SC study

    // for Drawing SLD in canvas
    /// <summary>Gets or sets the global data.</summary>
    public int xgridSize { get; set; } = 12000; // default

    /// <summary>Gets or sets the global data.</summary>
    public int ygridSize { get; set; }= 10000; // default

    /// <summary>Gets or sets the global data.</summary>
    public int leftSpacing { get; set; }= 100;

    /// <summary>Gets or sets the global data.</summary>
    public int topSpacing { get; set; } = 150;

    /// <summary>Gets or sets the global data.</summary>
    public int xGridSpacing { get; set; }= 150;

    /// <summary>Gets or sets the global data.</summary>
    public int yGridSpacing { get; set; }= 200;

    //
    /// <summary>Gets or sets the global data.</summary>
    public string UpdatedBy { get; set; }

    /// <summary>Gets or sets the global data.</summary>
    public string selectedPage { get; set; } = "Encompass: The Fusion of 2D and 3D Engineering ";
    
    // Add this event
    public event Action OnHeaderChanged;

    // Call this whenever you update the header data
    public void NotifyHeaderChanged() => OnHeaderChanged?.Invoke();

    public bool Show3D { get; set; } = true;

    /// <summary>Gets or sets the global data.</summary>
    public string sceneCurrent { get; set; } = "";

    /// <summary>Gets or sets the global data.</summary>
    public SceneInfo sceneDataCurrent { get; set; } = new();

    /// <summary>Gets or sets the global data.</summary>
    public SceneInfo sceneDataCable { get; set; } = new();

    /// <summary>Gets or sets the global data.</summary>
    public SceneInfo sceneDataLoad { get; set; } = new();

    /// <summary>Gets or sets the global data.</summary>
    public SceneInfo sceneDataSegment { get; set; } = new();

    /// <summary>Gets or sets the global data.</summary>
    public List<DBMaster>? DBMasters { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<PlotPlan>? PlotPlans { get; set; } = [];

    public List<Vector3>? RefPoints { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public string XYEN { get; set; } = "";

    /// <summary>Gets or sets the global data.</summary>
    public List<SegmentResult>? SegmentResults { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Segment>? RawSegments { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Segment>? Segments { get; set; } = [];
    
    public List<Trench>? Trenches { get; set; } = [];
    public List<Structure>? Structures { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Segment>? Trays { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Bend>? Bends { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Tee>? Tees { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Cross>? Crosses { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Sleeve>? Sleeves { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Node>? Nodes { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Spacing>? Spacings { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>

    public List<Load>? Loads { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Cable>? Cables { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Equipment>? Equipments { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Board>? Boards { get; set; } = [];

    // system study page
    /// <summary>Gets or sets the global data.</summary>
    public float Sb { get; set; } = 100; // in MVA

    /// <summary>Gets or sets the global data.</summary>
    public List<Bus>? Buses { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Transformer>? Transformers { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<Switch>? Switches { get; set; } = [];

    public List<BusBarLink>? BusBarLinks { get; set; } = [];
    public List<Fuse>? Fuses { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<CableBranch>? CableBranches { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<BusDuct>? BusDucts { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<SLDXY>? SLDXYs { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<BusStudyResult>? BusStudyResults { get; set; } = [];

    /// <summary>Gets or sets the global data.</summary>
    public List<SLDComponent>? SLDComponents { get; set; } = [];


    //public static List<LoadMaster> ImportedLoads { get; set; } = [];

    //Catalogue Data
    /// <summary>Gets or sets the global data.</summary>
    public List<MotorData> MotorData { get; set; } = [];

    /// <summary>Gets or sets the a global data.</summary>
    public List<CableData> CableData { get; set; } = [];

    // cable schedule page
    /// <summary>Gets or sets the global data.</summary>
    public string DefaultPhaseIdentifier { get; set; }
        = ",L1,L2,L3"; // For Cable Tag no creation : 1st entry for 3ph

    /// <summary>Gets or sets the global data.</summary>
    public string DefaultRunSequenceIdentifier { get; set; }
        = ",A,B,C,D,E,F,G,H,I"; // For Cable Tag no creation: : 1st entry for single run

    /// <summary>Gets or sets the global data.</summary>
    public List<float> DefaultAvailableCableSizes1C { get; set; }
        = [1.5f, 2.5f, 4, 6, 10, 16, 25, 35, 50, 70, 95, 120, 150, 185, 240, 300, 400, 500, 630, 800, 1000];

    /// <summary>Gets or sets the global data.</summary>
    public List<float> DefaultAvailableCableSizesMC { get; set; }
        = [1.5f, 2.5f, 4, 6, 10, 16, 25, 35, 50, 70, 95, 120, 150, 185, 240, 300];


    // segment page

    /// <summary>Gets or sets the global data.</summary>
    public List<string> displayItemsLayoutComponentRawSegment { get; set; } = ["Plot", "RawSegment"];

    /// <summary>Gets or sets the global data.</summary>
    public List<string> displayItemsLayoutComponentGeneratedSegment { get; set; }
        = ["Plot", "IsolatedSegment", "Segment", "Bend", "Tee", "Cross", "Node", "Load", "Cable"];

    /// <summary>Gets or sets the global data.</summary>
    public string SegmentPage_locationString { get; set; } = "";


    // layout page
    /// <summary>Gets or sets the global data.</summary>
    public List<string> displayItemsLayoutComponent { get; set; }
        = ["Plot", "IsolatedSegment", "Segment", "Bend", "Tee", "Cross", "Node", "Sleeve", "Load", "Cable", "Board"];

    //public static List<string> displayItemsLayoutComponent = new() { "Plot", "Segment", "Tee", "Cross", "Node", "Load", "Cable" };
    //["Plot", "IsolatedSegment", "Segment", "Bend", "Tee", "Cross", "Node", "Sleeve", "Load", "Cable", "Board"];
    /// <summary>Gets or sets the aglobal data.</summary>
    public double segmentOpacity { get; set; } = 1;

    /// <summary>Gets or sets the aglobal data.</summary>
    public int[] isolatedSegmentColor { get; set; } = [150, 150, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] segmentColorNeutral { get; set; } = [150, 150, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] segmentColorLV { get; set; } = [150, 150, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] segmentColorHV { get; set; } = [150, 150, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] bendColor { get; set; } = [150, 150, 200];
    /// <summary>Gets or sets the global data.</summary>
    public int[] equipmentColor { get; set; } = [100, 100, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] teeColor { get; set; } = [150, 150, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] crossColor { get; set; } = [150, 150, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] nodeColor { get; set; } = [100, 50, 50];

    /// <summary>Gets or sets the global data.</summary>
    public int[] segmentColor { get; set; } = [0, 0, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] sleeveColor { get; set; } = [200, 200, 200];

    /// <summary>Gets or sets the global data.</summary>
    public int[] cableColor { get; set; } = [200, 50, 50];

    /// <summary>Gets or sets the aglobal data.</summary>
    public int CurveStep { get; set; } = 5;

    /// <summary>Gets or sets the aglobal data.</summary>
    public int BendStep { get; set; } = 5; // considered same as curvestep


    /// <summary>Gets or sets the aglobal data.</summary>
    public List<Segment> IsolatedSegments { get; set; } = [];

    //public static List<SegmentResult> SegmentResults { get; set; } = [];
    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> LadderCategories { get; set; } = [];

    //public static List<Cable> Cables { get; set; } = [];
    //
    /// <summary>Gets or sets the aglobal data.</summary>
    public bool CablesReadInProgress { get; set; } = false;

    /// <summary>Gets or sets the aglobal data.</summary>
    public bool LoadMastersReadInProgress { get; set; } = false;

    //
    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> RawSegmentJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> IsolatedSegmentJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> SegmentJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> BendJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> TeeJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> CrossJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> SleeveJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> NodeJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> LoadJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> CableJSON { get; set; } = [];

    /// <summary>Gets or sets the aglobal data.</summary>

    public int tubeTubularSegments { get; set; } = 10;

    /// <summary>Gets or sets the aglobal data.</summary>
    public int tubeRadialSegments { get; set; } = 10;

    /// <summary>Gets or sets the aglobal data.</summary>
    public double distanceMargin { get; set; } = 0.05; // mm

    /// <summary>Gets or sets the aglobal data.</summary>
    public double nodeGapLimit { get; set; } = 3000.0; // mm

    /// <summary>Gets or sets the aglobal data.</summary>
    public double MarginSide1 { get; set; } = 0.035; // to update later

    /// <summary>Gets or sets the aglobal data.</summary>
    public double MarginSide2 { get; set; } = 0.035; // to update later

    /// <summary>Gets or sets the aglobal data.</summary>
    public double MarginSpare { get; set; } = 0.1; // to update later

    //
    /// <summary>Gets or sets the aglobal data.</summary>
    public string LoadListComponent_locationString { get; set; } = "";

    /// <summary>Gets or sets the aglobal data.</summary>
    public List<string> CableCategoryList { get; set; } = [];


    // load list page

    /// <summary>Gets or sets the aglobal data.</summary>
    public string LoadListPage_displayMsgAction { get; set; } = "";

    /// <summary>Gets or sets the aglobal data.</summary>
    public bool LoadListPage_showInputFile { get; set; } = true;

    //public static List<LoadMaster> LoadMasters { get; set; } = [];
    //public static List<Board> Boards { get; set; } = [];
    //public static List<Equipment> Equipments { get; set; } = [];
    //public static List<LoadHeadS> LoadHeadSs { get; set; } = [];

    //public static List<BlockDiagram> BlockDiagrams { get; set; } = [];
    /// <summary>Gets or sets the aglobal data.</summary>
    public string exampleLoadJSON { get; set; }
        =
        "{'BlockDiagram':null,'ProjectId':'TestProject','OptionId':'base','FieldOrder':222,'RecordID':'262','Tag':'CF-1070','TagDesc':'Centrifuge','WBS':'977_A4: Solid','Discipline':'Electrical','LoadCategory':'Motor','LoadFilterCategory1':null,'LoadFilterCategory2':null,'LoadFilterCategory3':null,'PackageID':null,'PackageDescription':'Misc','SwitchboardIDList':null,'NamePlateRating':200.0,'NamePlateRatingUnit':'kW','Phase':3,'RatedVoltage':400.0,'RatedCurrent':332.38,'RatedPower':200.0,'Pole':0,'AbsorbedRating':160.0,'LoadingFactor':0.8,'SupplyClass':'Normal','FeederTypeBlockDiagram':'DOL MOTOR > 100kW','PowerFactorJSON':null,'PowerFactorTable':[{'X':100.0,'Y':0.9},{'X':75.0,'Y':0.828},{'X':50.0,'Y':0.702}],'PowerFactorRated':0.9,'PowerFactorOperating':0.847,'EfficiencyJSON':null,'EfficiencyTable':[{'X':100.0,'Y':96.5},{'X':75.0,'Y':96.8},{'X':50.0,'Y':96.7}],'EfficiencyRated':96.5,'EfficiencyOperating':96.772,'Duty':'Continuous','DiversityFactor':100.0,'StartingCurrentMultiples':9.5,'StartingPowerFactor':0.2,'ShorCircuitDuration':0.0,'SupplySource':'3JVS-1','SupplySourceBus':'A','CurrentOperating':281.75,'VoltageOperating':0.0,'CableDeratingFactor':0.0,'AmpicityMargin':0.0,'ActivePowerOperating':164.95,'ReactivePowerOperating':97.88,'RunningVoltageDropAuto':2.52,'RunningVoltageDropAutoC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'StartingVoltageDropAuto':14.4,'StartingVoltageDropAutoC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'RunningVoltageDropSelected':2.74,'RunningVoltageDropSelectedC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'StartingVoltageDropSelected':17.53,'StartingVoltageDropSelectedC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'RouteLengthCableSize':365.0,'VoltageOperatingC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'CurrentOperatingC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'PowerOperatingC':{'Real':0.0,'Imaginary':0.0,'Magnitude':0.0,'Phase':0.0},'PowerCableRunSelected':1,'PowerCableSizeSelected':0.0,'CableSizeSelected':'1Cx0','PowerCableRunAuto':3,'PowerCableSizeAuto':150.0,'CableSizeAuto':'3Rx3Cx150','SwitchboardID':null,'SwitchboardTag':null,'DataMaturityStatus':'EE','AutonomyTime':0.0,'RedundancyRequired':false,'BenchmarkRating':0.0,'ReAccelerationRequired':false,'ReStartRequired':false,'UpdatedBy':null,'UpdatedOn':'2023-03-07T13:38:43.3139262Z','DynamicLoadRatio':0.0,'ConnectedEquipmentId':null,'LoadScenario':null,'ConnectedCables':null,'LoadS':null,'MotorRatingS':null,'A':0.0,'LF':0.0,'AP':0.0,'SourceTag':null,'Ls':null,'CableSizes':null,'LoadHeadTag':null,'PhaseNoAuto':3,'PhaseNoSelected':1,'NeutralNo':1,'NeutralSize':0.0,'PENo':1,'PESize':0.0,'LocationAndSizeString':'','Location':{'X':1467.75,'Y':-988.07,'Z':0.0},'Shape':{'X':0.0,'Y':0.0,'Z':0.0},'LNS':null,'Remarks':'','Selected':false}";
        
    
    
    // SLD Page
    public int SLDPageTab { get; set; } = 1;
        
        
    }