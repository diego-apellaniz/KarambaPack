using System;
using System.Collections.Generic;
using Karamba.Models;
using Karamba.Elements;
using Karamba.GHopper.Models;
using Karamba.GHopper.Loads;
using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace KarambaPack
{
    public class Kar02_MMass : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Kar02_MMass()
          : base("Mass Source", "MsSrc",
              "Definition of the Mass Source for the Natural Vibrations Analysis based on the input Load Combination",
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
            pManager.AddTextParameter("Load Combinations", "Comb", "Definition of the Load Combination", GH_ParamAccess.item);

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
            pManager.RegisterParam(new Param_Model(), "outModel", "outModel", "Model with Point Masses assigned");
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
            string combo = "";

            //Output parameters:
            var newModel = new Karamba.Models.Model();

            //Other variables:            
            var PMasses = new Dictionary<Tuple<int, int>, Karamba.Loads.PointMass>();
            var myLoads = new List<Karamba.Loads.Load>();
            var myMasses = new List<Karamba.Loads.PointMass>();
            var oldPoints = new List<Karamba.Geometry.Point3>();
            var Grass = new List<BuilderElement>();
            string info, msg;
            double mass = 0;
            var cdg = new Karamba.Geometry.Point3();
            //var GHmess = new GH_RuntimeMessageLevel();
            bool runtime_warning = false;

            // Then we need to access the input parameters individually. 
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData<GH_Model>(0, ref in_gh_model)) return;
            if (!DA.GetData(1, ref combo)) return;

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

            //Dictionary of Load Factors
            var combos = new List<string>();
            combos.Add(combo);
            var LFactors = new Dictionary<Tuple<int, int>, double>(Others.dictLFactors(combos));

            // Loop through all point loads
            foreach (Karamba.Loads.PointLoad load in model.ploads)
            {
                //myTuple1: LCombo, LCase
                //myTuple2: LCombo, Node
                var myTuple1 = new Tuple<int, int>(0, load.loadcase);
                var myTuple2 = new Tuple<int, int>(0, load.node_ind);
                if (LFactors.ContainsKey(myTuple1))
                {
                    if (PMasses.ContainsKey(myTuple2))
                    {
                        PMasses[myTuple2] = new Karamba.Loads.PointMass(load.node_ind, -0.1 * LFactors[myTuple1] * load.force.Z + PMasses[myTuple2].mass(), 1000);
                    }
                    else
                    {
                        var myMass = new Karamba.Loads.PointMass(load.node_ind, -0.1 * LFactors[myTuple1] * load.force.Z, 1000);
                        PMasses.Add(myTuple2, myMass);
                    }
                }
            }


            //Assemble new Model!!
            foreach (Karamba.Nodes.Node node in model.nodes)
            {
                oldPoints.Add(node.pos);
            }
            foreach (Karamba.Elements.ModelElement elem in model.elems)
            {
                //elem.cloneGrassElement();
                //GrassElement myGrass = elem.clonedGrassElement();
                BuilderElement myGrass = elem.clonedBuilderElement();
                Grass.Add(myGrass);
                Grass.Add(myGrass);
            }
            foreach (Karamba.Loads.Load load in model.gravities.Values)
            {
                myLoads.Add(load);
            }
            foreach (Karamba.Loads.Load load in model.ploads)
            {
                myLoads.Add(load);
            }
            foreach (Karamba.Loads.Load load in model.eloads)
            {
                myLoads.Add(load);
            }
            foreach (Karamba.Loads.Load load in PMasses.Values)
            {
                myLoads.Add(load);
            }

            //Karamba.Models.Component_AssembleModel.solve(oldPoints, Grass, model.supports, myLoads, model.crosecs, model.materials,
            //                             model.beamsets, 0.01, out newModel, out info, out mass, out cdg, out msg, out GHmess);

            GH_RuntimeMessageLevel gH_RuntimeMessageLevel = GH_RuntimeMessageLevel.Blank;
            AssembleModel.solve(oldPoints, Grass, model.supports, myLoads, model.crosecs, model.materials, model.beamsets, model.joints, 0.01, out newModel, out info, out mass, out cdg, out msg, out runtime_warning);

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
            DA.SetData(0, new GH_Model(newModel));

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
                return KarambaPack_Common.Properties.Resources.ModalMass;
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
            get { return new Guid("e11019d6-7b62-412a-873e-1b37b49a2d7a"); }
        }
    }
}
