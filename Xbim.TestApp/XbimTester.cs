using System;
using System.IO;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using XbimGeometry.Interfaces;

using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.XbimExtensions.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using Humanizer;

namespace Xbim.TestApp
{
    public class XbimTester
    {
        //public const string ModelFile = "OneWallTwoWindows.ifc";
        public const string DefaultModelFile = "Duplex_A_20110907.ifc";

        public XbimTester(string modelFile = null)
        {
            IfcModelFile = modelFile ?? DefaultModelFile;
        }

        public string IfcModelFile
        { 
            get; set; 
        }

        public string XbimModel
        {
            get { return Path.ChangeExtension(IfcModelFile, ".xbim"); }
        }

        /// <summary>
        /// Loads an IFC model to an optimised format for interrogation and extracts data from it
        /// </summary>
        public void ProcessModel()
        {
            
            string geometryFileName = Path.ChangeExtension(IfcModelFile, ".wexbim");

            using(var model = new XbimModel())
            {
                BuildSemanticModel(model, XbimModel);

                BuildGeometry(model, geometryFileName);

                ExtractData(model);

                

            }
              
        }

        /// <summary>
        /// Takes a previously processed IFC model, reloads it and extracts data from it
        /// </summary>
        public void ProcessExisting()
        {

            using (var model = new XbimModel())
            {
                ReloadModel(model, XbimModel);

                ExtractData(model);

            }
                        
        }


        /// <summary>
        /// Lists all tangible items in the model and attempts to get data about their geometry
        /// </summary>
        /// <param name="model"></param>
        private void ExtractData(XbimModel model)
        {
            DumpContents(model);

            ShowSemanticSummary(model);
            ShowGeometrySummary(model);
        }

        private void DumpContents(XbimModel model)
        {
            Console.WriteLine("Extracting data from semantic and geometric model");

            // Just extract Products - i.e everything with Geometry or spatial context

            //foreach(var product in model.Instances.OfType<IfcProduct>()

            Parallel.ForEach(model.Instances.OfType<IfcProduct>()
                .Where(p => !p.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction))) // Ignore Openings  
                //.Where(p=>p.EntityLabel == 152)
                //.Where(p => p.GetType().IsSubclassOf(typeof(IfcBuildingElement)))
                .OrderBy(o => o.GetType().Name),
                product =>
                {

                    System.Console.WriteLine("#{0}: [{1}] {2} [{3}]",
                        product.EntityLabel,
                        product.GetType().Name,
                        product.Name,
                        product.GlobalId
                        //product.Representation
                        );

                    // TODO: show how to get Property sets etc.

                    GetGeometryData(model, product);

                });
        }

        private void GetGeometryData(XbimModel model, IfcProduct product)
        {
            var context = new Xbim3DModelContext(model);
            //TODO: WCS
            var metre = model.ModelFactors.OneMetre;

            var units = model.IfcProject.UnitsInContext.Units
                .Where<IfcSIUnit>(u => u.UnitType == IfcUnitEnum.LENGTHUNIT)
                .ToList();

            string defaultLengthUnit = "";
            
            if(units.Count > 0)
            {
                defaultLengthUnit = units.First().GetSymbol();
            }
            

            var styles = context.SurfaceStyles().ToList();

            var productShape =
                context.ShapeInstancesOf(product)
                .Where(p=>p.RepresentationType!=XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
                .Distinct();

            
            if(productShape.Any())
            {
                foreach (var shapeInstance in productShape)
                {
                    
                    var shapeGeometry = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                    XbimColour style = XbimColour.Default;
                    if(shapeInstance.HasStyle)
                        style = styles.First(s=>s.DefinedObjectId == shapeInstance.StyleLabel).ColourMap.FirstOrDefault();

                    Console.WriteLine("--Style: {0}", style);
                    Console.WriteLine("-- x:{0:0.0000} \n-- y:{1:0.0000} \n-- z:{2:0.0000} \n", 
                        shapeGeometry.BoundingBox.Location.X,
                        shapeGeometry.BoundingBox.Location.Y,
                        shapeGeometry.BoundingBox.Location.Z);

                    Console.WriteLine("-- sx:{0:0.0000} {3} \n-- sy:{1:0.0000} {3} \n-- sz:{2:0.0000} {3} \n",
                        shapeGeometry.BoundingBox.SizeX,
                        shapeGeometry.BoundingBox.SizeY,
                        shapeGeometry.BoundingBox.SizeZ,
                        defaultLengthUnit);
                }
            }
        }


        private void ShowSemanticSummary(XbimModel model)
        {
            var summary = model.Instances.OfType<IPersistIfcEntity>()
                .GroupBy(i => i.GetType())
                .Select(ig =>
                    new
                    {
                        Type = ig.Key.Name,
                        Classification = ig.Key.GetDomain(),
                        Count = ig.Count()
                    }
                )
                .OrderBy(o=>o.Classification)
                .ThenBy(n=>n.Type);

            Console.WriteLine("{0,-34} {1,-35} {2,-6}",
                "Domain",
                "Type",
                "Qty");
            Console.WriteLine("{0,-34} {1,-35} {2,6}",
                new string('=', 34),
                new string('=', 35), 
                new string('=', 6));

            foreach(var row in summary)
            {
                Console.WriteLine("{0,-34} {1,-35} {2,6}", row.Classification, row.Type, row.Count);
            }
            
        }

        

        private void ShowGeometrySummary(XbimModel model)
        {
            var context = new Xbim3DModelContext(model);
            
            Console.WriteLine();
            Console.WriteLine("Styles");
            foreach(var styles in context.SurfaceStyles())
            {
                Console.WriteLine("{0,-6} {1,-30} {2,-6}",
                    styles.DefinedObjectId,
                    styles.ColourMap.First(),
                    styles.IsTransparent);
            }
            var largest = context.GetLargestRegion();
            
            Console.WriteLine();
            Console.WriteLine("Regions");
            foreach(var region in context.GetRegions())
            {
                Console.WriteLine("{0,-20} {1,6} {2}",
                    region.Name,
                    region.Population,
                    (region == largest));

            }



            Console.WriteLine("{0,-8}, {1,-8}, {2,6} {3,8}",
                "Ifc Label",
                "Label",
                "Ref Count",
                "Bytes");

            var geometries = GetGeom(model)
                .OrderByDescending(o => o.Cost)
                .Distinct()
                .Take(10);

            var items = from inst in context.ShapeInstances()
                        join prod in model.Instances.OfType<IfcProduct>()
                            on inst.IfcProductLabel equals prod.EntityLabel
                      select new 
                      { 
                          Instance = inst, 
                          Geometry = context.ShapeGeometry(inst.InstanceLabel),  
                          Product = prod
                      };

            var expensiveProducts = items
                .OrderByDescending(o => (o.Geometry.IsValid ? o.Geometry.Cost : 0))
                .Take(20);

            foreach (var geometry in geometries)
            {
                Console.WriteLine("{0, -8}, {1,-8}, {2,6} {3,8}",
                    geometry.IfcShapeLabel,
                    geometry.ShapeLabel,
                    geometry.ReferenceCount,
                    geometry.Cost);
            }

            Console.WriteLine();
            Console.WriteLine("Top 20 expensive items");
            foreach (var item in expensiveProducts)
            {
                Console.WriteLine("{0,-6} {1,-40} {2,-20} {3,6}",
                    item.Product.EntityLabel,
                    item.Product.Name.ToString().Truncate(40),
                    item.Product.GetType().Name,
                    item.Geometry.Cost
                    );
            }
        }

        // Private fix for bug in Geom.Shape
        private IEnumerable<XbimShapeGeometry> GetGeom(XbimModel model)
        {
            using (var shapeGeometryTable = model.GetShapeGeometryTable())
            {

                using (shapeGeometryTable.BeginReadOnlyTransaction())
                {
                    IXbimShapeGeometryData shapeGeometry = new XbimShapeGeometry();
                    if (shapeGeometryTable.TryMoveFirstShapeGeometry(ref shapeGeometry))
                    {
                        do
                        {
                            yield return (XbimShapeGeometry)shapeGeometry;
                            shapeGeometry = new XbimShapeGeometry();
                        } while (shapeGeometryTable.TryMoveNextShapeGeometry(ref shapeGeometry));
                    }

                }

            }

        }

        /// <summary>
        /// Generates the Geometry from the semantic model
        /// </summary>
        /// <remarks>Geometry is stored in the xbim file and optionally exported to a wexbim file
        /// for use in optimised viewers etc</remarks>
        /// <param name="model"></param>
        /// <param name="geometryFileName"></param>
        private static void BuildGeometry(XbimModel model, string geometryFileName = "")
        {
            Console.WriteLine("Generating Geometry...");
            var m3DModelContext = new Xbim3DModelContext(model);
            m3DModelContext.CreateContext(XbimGeometryType.PolyhedronBinary);

            if(!String.IsNullOrEmpty(geometryFileName))
            {
                // This is optional. The generated geometry is already saved in the xbim file, but a wexbim 
                // format is much for transmission over a network (and is optimised for display)
                ExportGeometryData(geometryFileName, m3DModelContext);
            }
        }

        private static void ExportGeometryData(string geometryFileName, Xbim3DModelContext m3DModelContext)
        {
            using (var geometryStream = new FileStream(geometryFileName, FileMode.Create))
            {
                using (var bw = new BinaryWriter(geometryStream))
                {
                    m3DModelContext.Write(bw);

                    bw.Close();
                    geometryStream.Close();
                }
            }
        }

        /// <summary>
        /// Creates the xbim file from the source IFC model. 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="semanticFileName"></param>
        private void BuildSemanticModel(XbimModel model,string semanticFileName )
        {
            Console.WriteLine("Loading / Parsing Model file...");
            model.CreateFrom(IfcModelFile, semanticFileName, null, true);
        }

        /// <summary>
        /// Loads a model (semantic and geometric) from a previously created .xbim file
        /// </summary>
        /// <param name="model"></param>
        /// <param name="semanticFileName"></param>
        private void ReloadModel(XbimModel model, string semanticFileName)
        {
            model.Open(semanticFileName, XbimExtensions.XbimDBAccess.Read, null);
        }
    }
}
