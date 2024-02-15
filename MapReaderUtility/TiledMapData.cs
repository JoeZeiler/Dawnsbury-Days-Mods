using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml;
using System.Xml.Linq;

namespace MapReaderUtility
{
    /// <summary>
    /// loads a Tiled Map data and has some associated utility functions
    /// </summary>
    public class TiledMapData
    {
        public string MapName { get; set; } = "";
        private static readonly int[,] BlankLayer = {   { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } };

        /// <summary>
        /// the loaded xml of the map file
        /// </summary>
        public XDocument MapFileXML { get; set; }
        /// <summary>
        /// the main map XML
        /// </summary>
        public XElement MapXML { get; set; }

        /// <summary>
        /// creates the new tiled map by loading the data from the given file name
        /// </summary>
        /// <param name="mapName">the fully qualified path of the map to load</param>
        public TiledMapData(string mapName)
        {
            if(string.IsNullOrEmpty(mapName))
            {
                return;
            }
            MapName = mapName;
            try
            {
                MapFileXML = XDocument.Load(mapName);
                MapXML = MapFileXML.Element("map");
            }
            catch(Exception e)
            {
                if (e is XmlException || e is SecurityException || e is ArgumentException || e is InvalidOperationException || e is FileNotFoundException || e is UriFormatException)
                {
                    MapXML = null;
                }
                else
                    throw;
            }
        }

        /// <summary>
        /// translates the xml of the given layer name to its CSV representation
        /// </summary>
        /// <param name="layerName">the layer name to translate</param>
        /// <returns>the resulting CSV representation</returns>
        private string GetLayerCSV(string layerName)
        {
            string layerData = null;
            if (MapXML != null)
            {
                var layerXML = MapXML.Elements("layer").FirstOrDefault(x => x.Attribute("name").Value.Equals(layerName), null);
                if (layerXML != null)
                {
                    var dataXML = layerXML.Element("data");
                    if (dataXML != null)
                    {
                        layerData = dataXML.Value;
                    }
                }
            }
            return layerData;
        }

        /// <summary>
        /// translates the CSV of the given layer with the given layername to a 2D int array
        /// </summary>
        /// <param name="layerName">the name of the layer to translate</param>
        /// <returns>the 2D int array representation of the layer</returns>
        public int[,] GetLayer(string layerName)
        {
            var layerData = GetLayerCSV(layerName);

            if(layerData == null)
            {
                return BlankLayer.Clone() as int[,];
            }
            var lines = layerData.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int[,] layerArray = new int[19,23];
            for(int i = 0; i < 23;i++)
            {
                int[] line = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                if (lines.Length > i)
                {
                    line = lines[i].Split(",",StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(x=>int.TryParse(x,out int result)?result:0).ToArray();
                }
                for (int j = 0; j < 19; j++)
                {
                    if(line.Length <=j)
                    {
                        layerArray[j, i] = 0;
                        continue;
                    }
                    layerArray[j, i] = line[j];
                }
            }
            return layerArray;
        }

        /// <summary>
        /// checks the layer with the given layer name at the given location for the ID number of the tile at that location in that layer
        /// </summary>
        /// <param name="layerName">the name of the layer to check</param>
        /// <param name="x">the x location of the tile to check, 0 being far left and 18 being far right</param>
        /// <param name="y">th y location of the tile to check, 0 being the top, and 22 being the bottom</param>
        /// <returns>the ID number of the tile at the given location in the layer specified.</returns>
        public int GetValueAtLocationForLayer(string layerName, int x, int y)
        {
            var layerData = GetLayerCSV(layerName);
            if(layerData == null)
            {
                return 0;
            }
            var lines = layerData.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if(lines.Length <= y)
            {
                return 0;
            }
            var line = lines[y].Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out int result) ? result : 0).ToArray();
            if(line.Length <= x)
            {
                return 0;
            }
            return line[x];
        }
    }
}
