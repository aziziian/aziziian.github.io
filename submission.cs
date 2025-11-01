using System;
using System.Xml.Schema;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
// extra namespaces are allowed by the template
using System.Collections.Generic;

/**
* This template file is created for ASU CSE445 Distributed SW Dev Assignment 4.
* Please do not modify or delete any existing class/variable/method names.
* However, you can add more variables and functions.
* Uploading this file directly will not pass the autograder's compilation check,
* resulting in a grade of 0.
**/
namespace ConsoleApp1
{
    public class Program
    {
        public static string xmlURL      = "https://aziziian.github.io/Hotels.xml";
        public static string xmlErrorURL = "https://aziziian.github.io/HotelsErrors.xml";
        public static string xsdURL      = "https://aziziian.github.io/Hotels.xsd";

        public static void Main(string[] args)
        {
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);

            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);

            result = Xml2Json(xmlURL);
            Console.WriteLine(result);
        }

        public static string Verification(string xmlUrl, string xsdUrl)
        {
            var messages = new List<string>();

            try
            {
                // load XSD
                var schemas = new XmlSchemaSet();
                using (var xsdReader = XmlReader.Create(xsdUrl))
                {
                    schemas.Add(null, xsdReader);
                }

                // validating reader
                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas,
                    DtdProcessing = DtdProcessing.Ignore,
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                };
                settings.ValidationEventHandler += (s, e) =>
                {
                    messages.Add($"{e.Severity}: {e.Message}");
                };

                // validate by walking the XML
                using (var reader = XmlReader.Create(xmlUrl, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Exception: {ex.Message}");
            }

            return messages.Count == 0 ? "No Error" : string.Join(Environment.NewLine, messages);
        }

        public static string Xml2Json(string xmlUrl)
        {
            // load from URL
            var doc = new XmlDocument();
            doc.Load(xmlUrl);

            var hotelsRoot = doc.DocumentElement;
            if (hotelsRoot == null || hotelsRoot.Name != "Hotels")
                throw new InvalidOperationException("Root element must be <Hotels>.");

            var hotelNodes = hotelsRoot.SelectNodes("./Hotel");
            var hotelList = new List<object>();

            foreach (XmlNode h in hotelNodes)
            {
                string name = h.SelectSingleNode("./Name")?.InnerText ?? "";

                var phoneVals = new List<string>();
                foreach (XmlNode p in h.SelectNodes("./Phone"))
                    phoneVals.Add(p.InnerText);

                var addr = h.SelectSingleNode("./Address");
                if (addr == null)
                    throw new InvalidOperationException("Each Hotel must contain an Address element.");

                var addressObj = new Dictionary<string, object>
                {
                    ["Number"] = GetChildText(addr, "Number"),
                    ["Street"] = GetChildText(addr, "Street"),
                    ["City"]   = GetChildText(addr, "City"),
                    ["State"]  = GetChildText(addr, "State"),
                    ["Zip"]    = GetChildText(addr, "Zip"),
                    ["NearestAirport"] = addr.Attributes?["NearestAirport"]?.Value ?? ""
                };

                var hotelObj = new Dictionary<string, object>
                {
                    ["Name"] = name,
                    ["Phone"] = phoneVals,
                    ["Address"] = addressObj
                };

                var rating = h.Attributes?["Rating"]?.Value;
                if (!string.IsNullOrWhiteSpace(rating))
                    hotelObj["_Rating"] = rating;

                hotelList.Add(hotelObj);
            }

            var jsonRoot = new Dictionary<string, object>
            {
                ["Hotels"] = new Dictionary<string, object>
                {
                    ["Hotel"] = hotelList
                }
            };

            var jsonText = JsonConvert.SerializeObject(jsonRoot);

            JsonConvert.DeserializeXmlNode(jsonText);

            return jsonText;
        }

        private static string GetChildText(XmlNode parent, string child)
        {
            return parent.SelectSingleNode("./" + child)?.InnerText ?? "";
        }
    }
}
