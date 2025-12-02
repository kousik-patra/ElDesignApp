using System.Collections.Generic;

namespace ElDesignApp.Models;

using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using ElDesignApp.Models;
using Switch = ElDesignApp.Models.Switch;

public class SystemStudyClass
{
    
}


public class LFNode
{
    public LFNode(string name, string swbdTag)
    {
        Name = name;
        Edges = new List<LFEdge>();
        Vb = 0;
        VbAssigned = false;
        X = null;
        Y = null;
        L = null;
        SwbdTag = swbdTag;
        Network = 0;
        SwingSources = new List<string>();
    }

    public string Name { get; set; }
    public List<LFEdge> Edges { get; set; }
    public float Vb { get; set; }
    public bool VbAssigned { get; set; }
    public List<string> SwingSources { get; set; }
    public string? SwbdTag { get; set; } // corresponding Switchboard Tag
    public float? X { get; set; } // X position index in SLD
    public float? Y { get; set; } // Y position index in SLD
    public float? L { get; set; } // Length of bus for SLD
    public int Network { get; set; } // 1,2,3...

    public void AddEdge(string name, LFNode target, double weight, float vratio)
    {
        Edges.Add(new LFEdge(name, this, target, weight, vratio));
    }
}


public class LFEdge // branch
{
    public LFEdge(string name, LFNode source, LFNode target, double weight, float vratio)
    {
        Name = name;
        Source = source;
        Target = target;
        Weight = weight;
        VRatio = vratio;
        X = null;
        Y = null;
        Ypu = null;
    }

    public string Name { get; set; }
    public LFNode Source { get; set; }
    public LFNode Target { get; set; }
    public double Weight { get; set; }
    public float VRatio { get; set; } //  voltage Ratio Source to Target
    public Complex? Ypu { get; set; } // Branch Admittance in PU
    public float? X { get; set; } // X coordinate in SLD
    public float? Y { get; set; } // Y coordinate in SLD
}





public class BusVisit
{
    public BusVisit(Bus b, bool v)
    {
        B = b;
        V = v;
    }

    public Bus B { get; set; } // Bus
    public bool V { get; set; } // Bus Visited
}


public class BusParent
{
    public BusParent()
    {
    }

    public BusParent(string b, string p, string s)
    {
        B = b;
        P = p;
        S = s;
    }

    public string B { get; set; } // This Bus
    public string P { get; set; } // Parent Bus towards Source
    public string S { get; set; } // corresponding source bus
}


public class LFResult
{
    public LFResult(List<Bus> busResult, float ms, int it)
    {
        BusResult = busResult;
        MS = ms;
        IT = it;
    }

    public List<Bus> BusResult { get; set; }
    public float MS { get; set; } // time in millisec
    public int IT { get; set; } // iteration?
}