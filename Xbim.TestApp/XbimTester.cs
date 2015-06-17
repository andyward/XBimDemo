using System;
using System.IO;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using XbimGeometry.Interfaces;

namespace Xbim.TestApp
{
    public class XbimTester
    {
        const string ModelFile = "OneWallTwoWindows.ifc";
 
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
            foreach(var product in model.Instances.OfType<IfcProduct>()
                .Where(p=>p.EntityLabel == 1234)
                )
            {

                System.Console.WriteLine("{0}: [{1}] {2}",
                    product.EntityLabel,
                    product.GetType().Name,
                    product.Name
                    //product.Representation.Description
                    );

                GetGeometryData(model, product);

            }
        }

        private void GetGeometryData(XbimModel model, IfcProduct product)
        {
            var context = new Xbim3DModelContext(model);
            var metre = model.ModelFactors.OneMetre;

            //var scale = XbimMatrix3D.CreateScale((float)(1 / metre));

            var styles = context.SurfaceStyles().ToList();

            var productShape =
                context.ShapeInstancesOf(product).Where(p=>p.RepresentationType!=XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded).ToList();

            
            if(productShape.Any())
            {
                foreach (var shapeInstance in productShape)
                {
                    var shapeGeometry = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                    XbimColour style= new XbimColour();
                    if(shapeInstance.HasStyle)
                        style = styles.First(s=>s.DefinedObjectId == shapeInstance.StyleLabel).ColourMap.FirstOrDefault();

                    Console.WriteLine("--Style: {0}", style);
                    Console.WriteLine("-- x:{0} \n-- y:{1} \n-- z:{2} \n", 
                        shapeGeometry.BoundingBox.Location.X,
                        shapeGeometry.BoundingBox.Location.Y,
                        shapeGeometry.BoundingBox.Location.Z);

                    Console.WriteLine("-- sx:{0:0.0000} \n-- sy:{1:0.0000} \n-- sz:{2:0.0000} \n",
                        shapeGeometry.BoundingBox.SizeX,
                        shapeGeometry.BoundingBox.SizeY,
                        shapeGeometry.BoundingBox.SizeZ);
                }
            }
        }

        // Generates the Geometry from the semantic model
        private static void BuildGeometryModel(string geometryFileName, XbimModel model)
        {
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
            model.CreateFrom(ModelFile, semanticFileName, null, true);
        }
    }
}
