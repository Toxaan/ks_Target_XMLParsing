using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ks_Target_XMLParsing
{
    class Program
    {
        static void Main(string[] args)
        {
            string pathRules = @"..\..\..\data\q018.txt";
            string pathXml1 = @"..\..\..\data\XML\HM040016S04002_20025.xml";
            string pathXml2 = @"..\..\..\data\XML\LM040016S04002_20025.xml";

            var rules = getRules(pathRules);
            checkXML(rules, pathXml1);
            checkXML(rules, pathXml2);
        }

        public static Dictionary<string, string> getRules(string pathRules)
        {
            Dictionary<string, string> rules = new Dictionary<string, string>();

            try
            {
                using (StreamReader sr = new StreamReader(pathRules, System.Text.Encoding.Default))
                {
                    string line;
                    string[] str;

                    line = sr.ReadLine();
                    line = sr.ReadLine();

                    while ((line = sr.ReadLine()) != null)
                    {
                        line = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ");
                        str = line.Split(" ");
                        rules[str[1]] = str[2] + " " + str[str.Length - 1];
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return rules;
        }

        public static void checkXML(Dictionary<string, string> rules , string pathXml)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var enc1252 = Encoding.GetEncoding(1252);

                XDocument xDoc = XDocument.Load(pathXml);
                var xElems = xDoc.Elements();
                string str = "";
                int strNum = 2;

                XDocument xdoc = new XDocument();
                XElement errors = new XElement("ERRORS");

                void parse(IEnumerable<XElement> xElems)
                {
                    foreach(var t in xElems)
                    {
                        var u = t;
                        str += u.Name;
                        strNum++;
                        while (u.Parent != null)
                        {
                            str = str.Insert(0, (u.Parent.Name).ToString() + "/");
                            u = u.Parent;
                        }

                        if(rules.ContainsKey(str))
                        {
                            string[] limitations = rules[str].Split(" ");
                            switch (limitations[1])
                            {
                                case "T":

                                    if(t.Value.Length > Convert.ToInt16(limitations[0]))
                                    {
                                        xWrite(str, "Max string length " + limitations[0], strNum);
                                    }
                                    break;

                                case "D":

                                    var dateFormat = "yyyy-MM-dd";
                                    if (!DateTime.TryParseExact(t.Value, dateFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out DateTime date))
                                    {
                                        xWrite(str, "Date Format: YYYY-MM-DD", strNum);
                                    }
                                    break;

                                case "N":

                                    if(long.TryParse(t.Value.Trim(), out long valueLong))
                                    {
                                        if(byte.TryParse(limitations[0].Trim(), out byte limit) && t.Value.Trim().Length <= limit)
                                        {}
                                        else
                                        {
                                            xWrite(str, "Incorrect data or length exceeded or incorrect number format: " + limitations[0], strNum);
                                        }
                                    }
                                    else if(float.TryParse(t.Value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float valueFloat))
                                    {
                                        string[] strV = t.Value.Trim().Split(".");

                                        if (float.TryParse(limitations[0].Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float limit) && t.Value.Trim().Length <= Math.Floor(limit) && Convert.ToByte(limitations[0].Trim().Split(".")[1]) >= strV[1].Length)
                                        {}
                                        else
                                        {
                                            xWrite(str, "incorrect data or incorrect number format: " + limitations[0], strNum);
                                        }
                                    }
                                    break;
                            }
                        }

                        str = "";
                        if (t.Elements().Any() && t.Elements() != null)
                        {
                            parse(t.Elements());
                        }

                    }
                    strNum++;
                }

                void xWrite(string str, string limit, int strNum)
                {
                    XElement xEl = new XElement("ERROR");
                    XElement xTag = new XElement("TAG", str);
                    XElement xLimit = new XElement("LIMIT", limit);
                    XElement xStrNum = new XElement("STRNUM", strNum);
                    xEl.Add(xTag);
                    xEl.Add(xLimit);
                    xEl.Add(xStrNum);
                    errors.Add(xEl);
                }

                parse(xElems);

                if (!errors.Nodes().Any())
                {
                    errors.Value = "No errors";
                }

                xdoc.Add(errors);
                xdoc.Save(pathXml.Replace(".xml","") + "_ERRORS.xml");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
