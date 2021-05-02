using System;
using System.Collections.Generic;
using Karamba.Models;
using Karamba.GHopper.Models;
using Karamba.GHopper.Loads;
using Karamba.Elements;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Karamba.Loads;
using Karamba.Geometry;
using System.Drawing;
using Grasshopper.Kernel.Types;
using Karamba.GHopper.Utilities;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Karamba.GHopper.Geometry;
using Karamba.CrossSections;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace KarambaPack
{
    public class Kar03_LCA : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Kar03_LCA()
          : base("Input for LCA", "LCA", "Get Model masses groupes by material for Life Cycle Analysis",
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
            pManager.AddParameter(new Param_Model(), "Model", "Model", "Karamba Model to extract data from", GH_ParamAccess.item);

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
            pManager.AddTextParameter("Materials", "Mat", "Materials of the Karamba model", GH_ParamAccess.list);            
            pManager.AddNumberParameter("Mass", "Mass", "Mass of the Karamba elements in [kg]", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Volume", "Vol", "Volume of the Karamba elements in [m3]", GH_ParamAccess.tree);
            pManager.AddMeshParameter("Geometry", "Geo", "Geometry of the Karamba elements", GH_ParamAccess.tree);

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
            if (!DA.GetData<GH_Model>(0, ref in_gh_model)) return;
            var model = (Karamba.Models.Model)in_gh_model.Value;

            //Output parameters:            
            var materials = new List<string>();
            var outMeshes = new DataTree<GH_Mesh>();
            var outMasses = new DataTree<double>();
            var outVolumes = new DataTree<double>();

            //Other variables:
            var model_nodes = model.nodes;

            //Do stuff
            model.dp = (ModelDisp)model.dp.Clone();
            model.dp.beam = new BeamDisplay();
            model.dp.beam.displayCrosssection = true;
            model.dp.shell = new ShellDisplay();
            model.dp.shell.displayCrosssection = true;
            //model.dp.shell.displayCrosssectionThickness = true;

            List<IMesh> list = new List<IMesh>();
            List<IMesh> list2 = new List<IMesh>();
            List<string> legend_tags = new List<string>();
            List<Color> legend_colors = new List<Color>();

            // Get rendered elements
            List<GH_Mesh> list_beam_meshes = new List<GH_Mesh>();
            List<GH_Mesh> list_shell_meshes = new List<GH_Mesh>();
            model.dp.collectRenderedBeamMesh(model, model.lc_super_model_results, list2, ref legend_tags, ref legend_colors);
            foreach (IMesh item in list2)
            {
                if (item != null)
                {
                    Mesh3 mesh = item as Mesh3;
                    if (mesh != null)
                    {
                        list_beam_meshes.Add(new GH_Mesh(((IReadonlyMesh)mesh).Convert()));
                    }
                    else if (item is RhinoMesh)
                    {
                        list_beam_meshes.Add(new GH_Mesh(((IReadonlyMesh)mesh).Convert()));
                    }
                }
            }
            model.dp.collectRenderedShellMesh(model, model.lc_super_model_results, model.dp.shell.layerInd, list, ref legend_tags, ref legend_colors);            
            if (list.Count % 6 != 0) // Add caps to meshes
            {
                throw new ArgumentException("Number of meshes is uneven: capping of boundary not possible");
            }
            for (int i = 0; i < list.Count*2/3-1; i += 2)
            {
                Mesh3 mesh = Mesh3.CapMesh(list[i], list[i + 1]);
                mesh.ComputeNormals();

                var outMesh = new Mesh();
                outMesh.Append(((IReadonlyMesh)list[i]).Convert());
                outMesh.Append(((IReadonlyMesh)mesh).Convert());
                outMesh.Append(((IReadonlyMesh)list[i + 1]).Convert());
                GH_Mesh item = new GH_Mesh(outMesh);
                list_shell_meshes.Add(item);
            }

            // Get outputs materials and masses
            var countBeam = 0;
            var countShell = 0;
            foreach (var element in model.elems)
            {
                // Check if material exists in material list
                if(!materials.Contains(element.crosec.materialName))
                {
                    materials.Add(element.crosec.materialName);
                }
                var mat_index = materials.IndexOf(element.crosec.materialName);
                var mat_path = new GH_Path(mat_index);

                // Add mesh and mass to output
                var model_element = element as Karamba.Elements.ModelElement;
                var beam_element = model_element as Karamba.Elements.ModelBeam;
                outMeshes.EnsurePath(mat_path);
                outMasses.EnsurePath(mat_path);
                outVolumes.EnsurePath(mat_path);
                if (beam_element != null)
                {
                    //Add mesh to output
                    outMeshes.Add(list_beam_meshes[countBeam], mat_path);
                    // Add mass to output
                    var nodes = model_nodes.Where(x => model_element.node_inds.Contains(x.ind)).ToList();
                    var node_1 = nodes.Where(x => x.ind == model_element.node_inds[0]).ToList()[0];
                    var node_2 = nodes.Where(x => x.ind == model_element.node_inds[1]).ToList()[0];
                    double vol = node_1.pos.DistanceTo(node_2.pos) * ((CroSec_Beam)model_element.crosec).A;
                    double mass = vol * ((CroSec_Beam)model_element.crosec).material.gamma();
                    outVolumes.Add(vol, mat_path);
                    outMasses.Add(mass * 100, mat_path);
                    //increase counter
                    countBeam++;
                }else
                {
                    var shell_element = model_element as Karamba.Elements.ModelShell;
                    if(shell_element != null)
                    {
                        //Add mesh to output
                        outMeshes.Add(list_shell_meshes[countShell], mat_path);
                        // Add mass to output
                        double vol = 0.0;
                        for (int i = 0; i < shell_element.mesh.Faces.Count; i++)
                        {
                            ShellLayer shellLayer = (shell_element.crosec as CroSec_Shell).elem_crosecs[i].layers[0];
                            vol += shell_element.mesh.faceArea(i, out var _) * shellLayer.height;
                        }
                        outVolumes.Add(vol, mat_path);
                        outMasses.Add(vol * shell_element.crosec.material.gamma() * 100, mat_path);
                        countShell++;
                    }
                }
            }

            if (outMeshes.DataCount != outMasses.DataCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Output lists have different lenghts");
            }

            // Finally assign output parameters.
            DA.SetDataList(0, materials);            
            DA.SetDataTree(1, outMasses);
            DA.SetDataTree(2, outVolumes);
            DA.SetDataTree(3, outMeshes);
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
                return KarambaPack_Common.Properties.Resources.LCA;     
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
            get { return new Guid("27bbce4d-cfb9-4d14-9c1f-6f6bd319d6c6"); }
        }
    }
}
