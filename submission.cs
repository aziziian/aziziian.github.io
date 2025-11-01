using System;
using System.Xml.Schema;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Net; // <-- for TLS 1.2

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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);

            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);

            result = Xml2Json(xmlURL);   // Per template: pass URL
            Console.WriteLine(result);
        }

        // Q2.1
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            // return "No errors are found" if XML is valid. Otherwise, return the desired exception message.
            var messages = new List<string>();

            try
            {
                // Load XSD (from URL)
                var schemas = new XmlSchemaSet();
                using (var xsdReader = XmlReader.Create(xsdUrl))
                {
                    schemas.Add(null, xsdReader);
                }

                // Configure validating reader
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
                    // Collect all messages (errors + warnings)
                    messages.Add($"{e.Severity}: {e.Message}");
                };

                // Parse & validate
                using (var reader = XmlReader.Create(xmlUrl, settings))
                {
                    while (reader.Read()) { /* walk the doc */ }
                }
            }
            catch (Exception ex)
            {
                // Network / well-formedness / other runtime issues
                messages.Add($"Exception: {ex.Message}");
            }

            // Spec’s success string:
            return messages.Count == 0 ? "No errors are found" : string.Join(Environment.NewLine, messages);
        }

        public static string Xml2Json(string xmlUrl)
        {
            // Load XML from URL
            var doc = new XmlDocument();
            doc.Load(xmlUrl);

            var root = doc.DocumentElement;
            if (root == null || root.Name != "Hotels")
                throw new InvalidOperationException("Root element must be <Hotels>.");

            var hotelNodes = root.SelectNodes("./Hotel");
            var hotelList = new List<object>();

            foreach (XmlNode h in hotelNodes)
            {
                var name = h.SelectSingleNode("./Name")?.InnerText ?? "";

                var phones = new List<string>();
                foreach (XmlNode p in h.SelectNodes("./Phone"))
                    phones.Add(p.InnerText);

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
                    ["Phone"] = phones,
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

            // Required by spec: must be de-serializable
            JsonConvert.DeserializeXmlNode(jsonText);

            return jsonText;
        }

        // helper (allowed by template)
        private static string GetChildText(XmlNode parent, string childName)
        {
            var n = parent.SelectSingleNode("./" + childName);
            return n?.InnerText ?? "";
        }
    }
}
