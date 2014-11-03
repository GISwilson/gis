﻿using SInnovations.Gis.TileGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace SInnovations.Gis.OgrHelpers
{
    public class OgrHelper
    {
        public IEnvironmentVariablesProvider EnvironmentVariables { get; set; }
        public OgrHelper(IEnvironmentVariablesProvider EnvironmentVariables = null)
        {
            this.EnvironmentVariables = EnvironmentVariables;
        }
        public Task<double[]> GetOgrExtentAsync(string source, string layer=null)
        {
            var process = new AsyncProcess<double[]>(@"%GDAL%\ogrInfo", parseExtent) {  EnvironmentVariables = EnvironmentVariables};
            return process.RunAsync(string.Format(@"-so -al ""{0}"" {1}", source, layer));        
        }
        public Task<double[]> GetGdalExtentAsync(string source)
        {
            var process = new AsyncProcess<double[]>(@"%GDAL%\gdalinfo", parseRasterExtent) { EnvironmentVariables = EnvironmentVariables };
            return process.RunAsync(string.Format(@"""{0}""", source));
        }

        public Task<string> GetProj4TextAsync(string source, string layer = null)
        {
            var path = Path.ChangeExtension(source, "prj");
            var process = new AsyncProcess<string>(@"%GDAL%\gdalsrsinfo", parseProj4) { EnvironmentVariables = EnvironmentVariables };
            return process.RunAsync(string.Format(@"""{0}""", path));  
        }
        public Task<int> BuildVrtFileAsync(string filelist, string outputfile)
        {
            var process = new AsyncProcess(@"%GDAL%\gdalbuildvrt") { EnvironmentVariables = EnvironmentVariables };
            return process.RunAsync(string.Format(@" -input_file_list ""{0}"" ""{1}""", filelist, outputfile));  
        }

        public Task<int> GdalExtractWithTranslate(string source,string target, string projwin, string outtype, int? outputsize = null)
        {
            var process = new AsyncProcess(@"%GDAL%\gdal_translate") { EnvironmentVariables = EnvironmentVariables };
            return process.RunAsync(string.Format(@" -of {2} {5} {4} -projwin {1} ""{0}"" ""{3}"" ", 
                source, projwin, outtype,target, outputsize.HasValue ? 
                string.Format("-outsize {0} {0}",outputsize.Value):"", 
                (outtype=="gtiff") ? "-co COMPRESS=LZW -co PREDICTOR=2" : (outtype=="png"?"-co WORLDFILE=YES":"")));  
        }

        public Task<int> Ogr2OgrClipAsync(string source,string target, string t_srs, double[] extent)
        {
            var process = new AsyncProcess(@"%GDAL%\ogr2ogr") { EnvironmentVariables = EnvironmentVariables };

            return process.RunAsync(string.Format(@"{0} {1} -t_srs {2} -spat {3}",
                target, source, t_srs, string.Join(" ", extent)));  
        }

        public Task<int> AddSpatialIndexToMSQL(string connectionstring,string tablename)
        {
            var process = new AsyncProcess(@"%GDAL%\ogrinfo") { EnvironmentVariables = EnvironmentVariables };
            return process.RunAsync(string.Format(@" ""{1}"" -sql ""create spatial index on {0}""", tablename, connectionstring));
        }


        private static double[] parseExtent(Process p, string str, string err)
        {
            var extent = Regex.Match(str, @"Extent: \((.*),(.*)\) - \((.*),(.*)\)");

            return new double[] { double.Parse(extent.Groups[1].Value), double.Parse(extent.Groups[2].Value), double.Parse(extent.Groups[3].Value), double.Parse(extent.Groups[4].Value) };
        }
        public static double[] parseRasterExtent(Process p, string str, string err)
        {
            var tl = Regex.Match(str, @"Upper Left  \((.*?),(.*?)\)");
            var tr = Regex.Match(str, @"Upper Right \((.*?),(.*?)\)");
            var bl = Regex.Match(str, @"Lower Left  \((.*?),(.*?)\)");
            var br = Regex.Match(str, @"Lower Right \((.*?),(.*?)\)");

            return new double[] { double.Parse(tl.Groups[1].Value), double.Parse(br.Groups[2].Value), double.Parse(br.Groups[1].Value), double.Parse(tl.Groups[2].Value) };
            //options.Tl = new double[] { double.Parse(tl.Groups[1].Value), double.Parse(tl.Groups[2].Value) };
            //options.Tr = new double[] { double.Parse(tr.Groups[1].Value), double.Parse(tr.Groups[2].Value) };
            //options.Bl = new double[] { double.Parse(bl.Groups[1].Value), double.Parse(bl.Groups[2].Value) };
            //options.Br = new double[] { double.Parse(br.Groups[1].Value), double.Parse(br.Groups[2].Value) };
        }
        private static string parseProj4(Process p, string str, string err)
        {
            var extent = Regex.Match(str, @"PROJ.4 : '(.*)'");

            return extent.Groups[1].Value;

        }
    }
}
