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

namespace Xbim.TestApp
{
    public class XbimTester
    {
        //const string ModelFile = "OneWallTwoWindows.ifc";
        const string ModelFile = "Duplex_A_20110907.ifc";
 
        public void ProcessModel()
        {
            string semanticFileName = Path.ChangeExtension(ModelFile, ".xbim");
            string geometryFileName = Path.ChangeExtension(ModelFile, ".wexbim");

            using(var model = new XbimModel())
            {
                BuildSemanticModel(semanticFileName, model);

                BuildGeometryModel(geometryFileName, model);

                ExtractData(model);

            }
            Console.ReadLine();
            
        }

        private void ExtractData(XbimModel model)
        {
            Console.WriteLine("Extracting data from semantic and geometric model");

            // Just extract Products - i.e everything with Geometry or spatial context
            foreach(var product in model.Instances.OfType<IfcProduct>()
                .Where(p => !p.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction))) // Ignore Openings  
                //.Where(p=>p.EntityLabel == 152)
                //.Where(p => p.GetType().IsSubclassOf(typeof(IfcBuildingElement)))
                .OrderBy(o=>o.GetType().Name)
                )
            {

                System.Console.WriteLine("#{0}: [{1}] {2} [{3}]",
                    product.EntityLabel,
                    product.GetType().Name,
                    product.Name,
                    product.GlobalId
                    //product.Representation
                    );

                GetGeometryData(model, product);

            }
        }

        private void GetGeometryData(XbimModel model, IfcProduct product)
        {
            var context = new Xbim3DModelContext(model);
            //TODO: WCS
            var metre = model.ModelFactors.OneMetre;

            var defaultLengthUnit = model.IfcProject.UnitsInContext.Units
                .Where<IfcSIUnit>(u => u.UnitType == IfcUnitEnum.LENGTHUNIT)
                .First();


            var styles = context.SurfaceStyles().ToList();

            var productShape =
                context.ShapeInstancesOf(product)
                .Where(p=>p.RepresentationType!=XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
                .Distinct()
                .ToList();

            
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
                        defaultLengthUnit.GetSymbol());
                }
            }
        }

        // Generates the Geometry from the semantic model
        private static void BuildGeometryModel(string geometryFileName, XbimModel model)
        {
            Console.WriteLine("Generating Geometry...");
            var m3DModelContext = new Xbim3DModelContext(model);

            using (var geometryStream = new FileStream(geometryFileName, FileMode.Create))
            {
                using (var bw = new BinaryWriter(geometryStream))
                {
                    m3DModelContext.CreateContext(XbimGeometryType.PolyhedronBinary);
                    m3DModelContext.Write(bw);

                    bw.Close();
                    geometryStream.Close();
                }
            }
        }

        // Creates the xbim file from the source IFC model. 
        private static void BuildSemanticModel(string semanticFileName, XbimModel model)
        {
            Console.WriteLine("Loading / Parsing Model file...");
            model.CreateFrom(ModelFile, semanticFileName, null, true);
        }
    }
}
