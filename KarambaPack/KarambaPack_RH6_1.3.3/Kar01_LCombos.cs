using System;
using System.Collections.Generic;
using System.Linq;
using Karamba.Models;
using Karamba.GHopper.Models;
using Karamba.GHopper.Loads;
using Karamba.Elements;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Karamba.Loads;
using Karamba.Utilities;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace KarambaPack
{
    public class Kar01_LCombos : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Kar01_LCombos()
          : base("Load Combinations", "LCombos",
              "Create Load Combinations from the input Load Cases",
              "Karamba3D", "0.B+G")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddParameter(new Param_Model(), "inModel", "inModel", "Model to be manipulated", GH_ParamAccess.item);
            pManager.AddTextParameter("Load Combinations", "Comb", "Definition of Load Combinations", GH_ParamAccess.list);

            // If you want to change properties of certain parameters, 
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.RegisterParam(new Param_Model(), "outModel", "outModel", "Model replacing the Load Cases by Load Combinations");
            pManager.RegisterParam(new Param_Load(), "outLoads", "outLoads", "Output Combined Loads");

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.
            GH_Model in_gh_model = null;
            var combos = new List<string>();
            //int index0 = 0;

            //Output parameters:
            //var newModel = new Karamba.Models.Model();
            var myGHLoads = new List<GH_Load>();

            //Other variables:            
            var PLoads = new Dictionary<Tuple<int, int>, Karamba.Loads.PointLoad>();
            var ULoads = new Dictionary<Tuple<int, string>, Karamba.Loads.UniformlyDistLoad>();
            var GLoads = new List<Karamba.Loads.GravityLoad>();
            var TLoads = new Dictionary<Tuple<int, string>, Karamba.Loads.TemperatureLoad>();
            var SLoads = new Dictionary<Tuple<int, string>, Karamba.Loads.StrainLoad>();
            var myLoads = new List<Karamba.Loads.Load>();
            var oldPoints = new List<Karamba.Geometry.Point3>();
            var Grass = new List<Karamba.Elements.BuilderElement>();
            string info, msg;
            double mass = 0;
            var cdg = new Karamba.Geometry.Point3();
            //var GHmess = new GH_RuntimeMessageLevel();
            bool runtime_warning = false;

            // Then we need to access the input parameters individually. 
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData<GH_Model>(0, ref in_gh_model)) return;
            if (!DA.GetDataList(1, combos)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            //if (radius0 < 0.0)
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Inner radius must be bigger than or equal to zero");
            //    return;
            //}         

            // We're set to do stuff now.
            // clone model to avoid side effects
            Model model = in_gh_model.Value;
            model = (Karamba.Models.Model)model.Clone();
            model.clonePointLoads();
            //Get Load Factors from the definitions of the Load Combinations
            var LFactors = new Dictionary<Tuple<int, int>, double>(Others.dictLFactors(combos));
            // Define new loads!!
            // Loop through all point loads
            foreach (Karamba.Loads.PointLoad load in model.ploads)
            {
                for (int i = 0; i < combos.Count; i++)
                {
                    //myTuple1: LCombo, LCase
                    //myTuple2: LCombo, Node
                    var myTuple1 = new Tuple<int, int>(i, load.loadcase);
                    var myTuple2 = new Tuple<int, int>(i, load.node_ind);
                    if (LFactors.ContainsKey(myTuple1))
                    {
                        if (PLoads.ContainsKey(myTuple2))
                        {
                            PLoads[myTuple2].force += LFactors[myTuple1] * load.force;
                            PLoads[myTuple2].moment += LFactors[myTuple1] * load.moment;
                        }
                        else
                        {
                            var f = LFactors[myTuple1] * load.force;
                            var m = LFactors[myTuple1] * load.moment;
                            // var myLoad = new PointLoad(load.node_ind, f, m, load.LcName, load.local);
                            var myLoad = new PointLoad(load.node_ind, f, m, i, load.local);
                            //var myLoad = new Karamba.Loads.PointLoad();
                            ////myLoad.loadcase = i + index0;
                            //          myLoad.node_ind = load.node_ind;
                            //myLoad.moment = LFactors[myTuple1] * load.moment;
                            //myLoad.force = LFactors[myTuple1] * load.force;
                            PLoads.Add(myTuple2, myLoad);
                        }
                    }
                }
            }
            // Loop through all element loads
            foreach (Karamba.Loads.ElementLoad eload in model.eloads)
            {
                // Loop through all uniform loads
                if (eload is Karamba.Loads.UniformlyDistLoad)
                {
                    Karamba.Loads.UniformlyDistLoad load = (Karamba.Loads.UniformlyDistLoad)eload;
                    for (int i = 0; i < combos.Count; i++)
                    {
                        //myTuple1: LCombo, LCase
                        //myTuple2: LCombo, beamID
                        var myTuple1 = new Tuple<int, int>(i, load.loadcase);
                        if (LFactors.ContainsKey(myTuple1))
                        {
                            for (int j = 0; j < load.beamIds.Count; j++)
                            {
                                var myTuple2 = new Tuple<int, string>(i, load.beamIds[j]);
                                if (ULoads.ContainsKey(myTuple2))
                                {
                                    //ULoads[myTuple2] = new Karamba.Loads.UniformlyDistLoad(ULoads[myTuple2].beamIds,
                                    //                                                        ULoads[myTuple2].Load + LFactors[myTuple1] * load.Load,
                                    //                                                        ULoads[myTuple2].q_orient, ULoads[myTuple2].LcName);
                                    ULoads[myTuple2] = new Karamba.Loads.UniformlyDistLoad(ULoads[myTuple2].beamIds,
                                                                                            ULoads[myTuple2].Load + LFactors[myTuple1] * load.Load,
                                                                                            ULoads[myTuple2].q_orient, i);
                                }
                                else
                                {
                                    //var myLoad = new Karamba.Loads.UniformlyDistLoad(load.beamIds[j], LFactors[myTuple1] * load.Load,
                                    //                                                        load.q_orient, load.LcName);
                                    var myLoad = new Karamba.Loads.UniformlyDistLoad(load.beamIds[j], LFactors[myTuple1] * load.Load,
                                                                                            load.q_orient, i);
                                    ULoads.Add(myTuple2, myLoad);
                                }
                            }
                        }
                    }
                }
                else if (eload is Karamba.Loads.TemperatureLoad)
                {
                    // Loop through all temperature loads
                    Karamba.Loads.TemperatureLoad load = (Karamba.Loads.TemperatureLoad)eload;
                    for (int i = 0; i < combos.Count; i++)
                    {
                        //myTuple1: LCombo, LCase
                        //myTuple2: LCombo, beamID
                        var myTuple1 = new Tuple<int, int>(i, load.loadcase);
                        if (LFactors.ContainsKey(myTuple1))
                        {
                            for (int j = 0; j < load.beamIds.Count; j++)
                            {
                                var myTuple2 = new Tuple<int, string>(i, load.beamIds[j]);
                                if (TLoads.ContainsKey(myTuple2))
                                {
                                    //TLoads[myTuple2] = new Karamba.Loads.TemperatureLoad(TLoads[myTuple2].beamIds,
                                    //    TLoads[myTuple2].incT + LFactors[myTuple1] * load.incT, 
                                    //    TLoads[myTuple2].Kappa0 + LFactors[myTuple1] * load.Kappa0, load.LcName);
                                    TLoads[myTuple2] = new Karamba.Loads.TemperatureLoad(TLoads[myTuple2].beamIds,
                                        TLoads[myTuple2].incT + LFactors[myTuple1] * load.incT,
                                        TLoads[myTuple2].Kappa0 + LFactors[myTuple1] * load.Kappa0, i);
                                }
                                else
                                {
                                    //var myLoad = new TemperatureLoad(load.beamIds[j],
                                    //    LFactors[myTuple1] * load.incT, LFactors[myTuple1] * load.Kappa0,
                                    //    load.LcName);
                                    //TLoads.Add(myTuple2, myLoad);
                                    var myLoad = new TemperatureLoad(load.beamIds[j],
                                        LFactors[myTuple1] * load.incT, LFactors[myTuple1] * load.Kappa0,
                                        i);
                                    TLoads.Add(myTuple2, myLoad);
                                }

                            }
                        }
                    }
                }
                else if (eload is Karamba.Loads.StrainLoad)
                {
                    // Loop through all temperature loads
                    Karamba.Loads.StrainLoad load = (Karamba.Loads.StrainLoad)eload;
                    for (int i = 0; i < combos.Count; i++)
                    {
                        //myTuple1: LCombo, LCase
                        //myTuple2: LCombo, beamID
                        var myTuple1 = new Tuple<int, int>(i, load.loadcase);
                        if (LFactors.ContainsKey(myTuple1))
                        {
                            for (int j = 0; j < load.beamIds.Count; j++)
                            {
                                var myTuple2 = new Tuple<int, string>(i, load.beamIds[j]);
                                if (SLoads.ContainsKey(myTuple2))
                                {
                                    //SLoads[myTuple2] = new Karamba.Loads.StrainLoad(SLoads[myTuple2].beamIds,                                    
                                    SLoads[myTuple2] = new Karamba.Loads.StrainLoad(SLoads[myTuple2].beamIds,
                                        SLoads[myTuple2].Eps0 + LFactors[myTuple1] * load.Eps0,
                                        SLoads[myTuple2].Kappa0 + LFactors[myTuple1] * load.Kappa0, i);
                                }
                                else
                                {
                                    //var myLoad = new StrainLoad(load.beamIds[j],
                                    //    LFactors[myTuple1] * load.Eps0, LFactors[myTuple1] * load.Kappa0,
                                    //    load.LcName);
                                    var myLoad = new StrainLoad(load.beamIds[j],
                                        LFactors[myTuple1] * load.Eps0, LFactors[myTuple1] * load.Kappa0,
                                        i);
                                    SLoads.Add(myTuple2, myLoad);
                                }

                            }
                        }
                    }
                }
            }

            // Loop through all gravity loads
            foreach (var load in model.gravities)
            {
                for (int i = 0; i < combos.Count; i++)
                {
                    //myTuple1: LCombo, LCase
                    var myTuple1 = new Tuple<int, int>(i, load.Value.loadcase);
                    if (LFactors.ContainsKey(myTuple1))
                    {
                        //GLoads.Add(new Karamba.Loads.GravityLoad(LFactors[myTuple1] * load.Value.force, load.Value.LcName));
                        GLoads.Add(new Karamba.Loads.GravityLoad(LFactors[myTuple1] * load.Value.force, i));
                    }
                }
            }



            // Create list of loads from loads stored in dictionaries
            foreach (var item in PLoads)
            {
                myLoads.Add(item.Value);
                myGHLoads.Add(new GH_Load(item.Value));

            }
            foreach (var item in ULoads)
            {
                myLoads.Add(item.Value);
                myGHLoads.Add(new GH_Load(item.Value));
            }
            foreach (var item in TLoads)
            {
                myLoads.Add(item.Value);
                myGHLoads.Add(new GH_Load(item.Value));
            }
            foreach (var item in SLoads)
            {
                myLoads.Add(item.Value);
                myGHLoads.Add(new GH_Load(item.Value));
            }
            var gravities = new Dictionary<int, GravityLoad>();
            foreach (var item in GLoads)
            {
                myLoads.Add(item);
                myGHLoads.Add(new GH_Load(item));

                gravities.Add(item.loadcase, item);
            }
            //Assemble new Model!!
            foreach (Karamba.Nodes.Node node in model.nodes)
            {
                oldPoints.Add(node.pos);
            }
            //var elem_id2ind_ = new Karamba.Utilities.ID IdMapper();
            foreach (Karamba.Elements.ModelElement elem in model.elems)                
            {
                //elem.cloneBuilderElement();
                //var myGrass = elem.clonedGrassElement();
                elem.cloneBuilderElement();
                BuilderElement myGrass = elem.clonedBuilderElement();
                Grass.Add(myGrass);
            }

            ModelBuilder modelBuilder = new ModelBuilder(0.0001);

            MessageLogger messageLogger = new MessageLogger();
            model.cloneElements();


            GH_RuntimeMessageLevel gH_RuntimeMessageLevel = GH_RuntimeMessageLevel.Blank;
            AssembleModel.solve(oldPoints, Grass, model.supports, myLoads, model.in_crosecs, model.in_materials, model.beamsets, model.joints, 0.005, out var newModel, out info, out mass, out cdg, out msg, out runtime_warning);


            if (runtime_warning)
            {
                gH_RuntimeMessageLevel = GH_RuntimeMessageLevel.Warning;
            }
            if (msg != "")
            {
                AddRuntimeMessage(gH_RuntimeMessageLevel, msg);
                if (gH_RuntimeMessageLevel == GH_RuntimeMessageLevel.Error)
                {
                    return;
                }
            }

            // Finally assign output parameters.
            //DA.SetData(0, new GH_Model(newModel));
            DA.SetData(0, new GH_Model(newModel));
            DA.SetDataList(1, myGHLoads);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return KarambaPack_Common.Properties.Resources.molecule;
                //return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e11019d6-7b62-412a-873e-1b37b79a2d7a"); }
        }
    }
}
